using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Brainrot
{
    [Serializable]public sealed class CoinTuning
    {
        [Min(1),Tooltip("Монеты за обычную акулу.")]public int SharkValue=1;
        [Min(1),Tooltip("Монеты за осьминога.")]public int OctopusValue=3;
        [Min(1),Tooltip("Монеты за Оркаладона.")]public int OrcaladonValue=8;
        [Range(1,7),Tooltip("Радиус притяжения монет, м.")]public float MagnetRadius=3;
        [Range(.2f,1.5f),Tooltip("Расстояние начала анимации подбора.")]public float PickupRadius=.65f;
        public int Value(EnemyKind kind)=>Mathf.Clamp(kind==EnemyKind.Orcaladon?OrcaladonValue:kind==EnemyKind.BlueberrinniOctopusini?OctopusValue:SharkValue,1,100);
    }
    [Serializable]public sealed class CoinDropSnapshot {public int Id,Amount;public Vector3 Position;}
    /// <summary>Scene-owned bounded coin pool. One visible gold coin may represent several currency units.</summary>
    public sealed class CoinDrops:MonoBehaviour
    {
        public const int Capacity=128;
        sealed class Coin {public Transform Root,Face;public int Id,Amount;public Vector3 Start,Landing,FlightStart;public float Age,Fly,RetryAt;public bool Active,Collecting;}
        readonly List<Coin> pending=new List<Coin>(Capacity);
        readonly List<Coin> pool=new List<Coin>(Capacity);
        Mesh disc;Material gold,face;
        GameManager game;
        public int ActiveCount{get{int n=0;foreach(var item in pool)if(item.Active)n++;return n;}}
        public void Initialize(GameManager owner){game=owner;}
        public void Drop(EnemyKind kind,Vector3 position,int id)
        {
            if(game==null||!game.IsPlaying||id<1||MetaProgression.IsClaimed(game.CoinRunId,id))return;
            Coin coin=null;foreach(var item in pool)if(!item.Active){coin=item;break;}
            if(coin==null&&pool.Count>=Capacity){
                // Bank the oldest reward before recycling its visual; a busy arena cannot lose currency.
                coin=pool[0];foreach(var item in pool)if(item.Age>coin.Age)coin=item;
                if(!MetaProgression.IsClaimed(game.CoinRunId,coin.Id)&&!game.TryCollectCoin(coin.Id,coin.Amount))return;
            }
            if(coin==null){coin=Create();pool.Add(coin);}
            float a=id*2.399963f;var landing=IslandMap.ClampToLand(position+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.85f);landing.y=0;
            Activate(coin,id,GameBalance.Current.Coins.Value(kind),position+Vector3.up*.8f,landing,0);
        }
        void Activate(Coin c,int id,int amount,Vector3 start,Vector3 landing,float age)
        {
            c.Id=id;c.Amount=amount;c.Start=start;c.Landing=landing;c.Age=age;c.Fly=0;c.RetryAt=0;c.Collecting=false;c.Active=true;
            c.Root.gameObject.SetActive(true);c.Root.position=start;c.Root.localScale=Vector3.one;
        }
        Coin Create()
        {
            if(disc==null)disc=Disc();
            if(gold==null){gold=WorldBuilder.Material(new Color(1,.64f,.055f),"Золотой ободок монеты");gold.SetFloat("_Smoothness",.48f);face=WorldBuilder.Material(new Color(1,.83f,.22f),"Тёплое золото монеты");face.SetFloat("_Smoothness",.30f);}
            var root=new GameObject("Золотая монета").transform;root.SetParent(transform,false);
            var body=Part(root,"Чеканка",Vector3.zero,new Vector3(.38f,.38f,.11f),gold);
            Part(body,"Золотой центр",new Vector3(0,0,-1.02f),new Vector3(.79f,.79f,.14f),face);
            Part(body,"Золотой центр оборота",new Vector3(0,0,1.02f),new Vector3(.79f,.79f,.14f),face);
            // A bold embossed vertical mark reads from either side without text textures.
            var mark=GameObject.CreatePrimitive(PrimitiveType.Cube);mark.name="Рельефный знак монеты";mark.transform.SetParent(body,false);mark.transform.localScale=new Vector3(.15f,.92f,2.4f);Destroy(mark.GetComponent<Collider>());mark.GetComponent<Renderer>().sharedMaterial=gold;
            return new Coin{Root=root,Face=body};
        }
        Transform Part(Transform parent,string title,Vector3 pos,Vector3 size,Material material)
        {
            var t=new GameObject(title,typeof(MeshFilter),typeof(MeshRenderer)).transform;t.SetParent(parent,false);t.localPosition=pos;t.localScale=size;
            t.GetComponent<MeshFilter>().sharedMesh=disc;var renderer=t.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;return t;
        }
        public void Tick(float dt,PlayerController player)
        {
            if(game==null||!game.IsPlaying||player==null||player.Health<=0||dt<=0)return;
            pending.Clear();var tuning=GameBalance.Current.Coins;float radius=Mathf.Max(tuning.PickupRadius,tuning.MagnetRadius);
            foreach(var c in pool){
                if(!c.Active)continue;
                if(MetaProgression.IsClaimed(game.CoinRunId,c.Id)){Hide(c);continue;}
                c.Age+=dt;
                if(c.Collecting){
                    c.Fly=Mathf.Min(1,c.Fly+dt/.20f);float eased=c.Fly*c.Fly;
                    var target=player.transform.position+Vector3.up*1.1f;
                    c.Root.position=Vector3.Lerp(c.FlightStart,target,eased)+Vector3.up*(Mathf.Sin(c.Fly*Mathf.PI)*.28f);c.Root.localScale=Vector3.one*Mathf.Lerp(1,.15f,eased);
                    if(c.Fly>=1&&Time.time>=c.RetryAt)pending.Add(c);
                    continue;
                }
                float t=Mathf.Clamp01(c.Age/.48f);
                if(t<1)c.Root.position=Vector3.Lerp(c.Start,c.Landing+Vector3.up*.45f,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*1.1f);
                else{
                    var p=c.Root.position;p.y=.45f+Mathf.Sin(c.Age*3.2f+c.Id)*.075f;c.Root.position=p;
                    var delta=player.transform.position-p;delta.y=0;float d=delta.magnitude;
                    if(d<=radius){
                        float speed=Mathf.Lerp(3,13,1-Mathf.Clamp01(d/radius));
                        if(d<=Mathf.Max(.2f,tuning.PickupRadius)+speed*dt){c.Collecting=true;c.FlightStart=p;c.Fly=0;}
                        else c.Root.position+=delta/d*(speed*dt);
                    }
                }
                c.Face.localRotation=Quaternion.Euler(8,c.Age*115+c.Id*17,0);
                c.Root.localScale=Vector3.one*(1+Mathf.Sin(c.Age*3+c.Id)*.035f);
            }
            // A cluster reaching the player in one frame commits with one disk write.
            if(pending.Count>0){
                var ids=new int[pending.Count];var amounts=new int[pending.Count];for(int i=0;i<pending.Count;i++){ids[i]=pending[i].Id;amounts[i]=pending[i].Amount;}
                bool saved=game.TryCollectCoins(ids,amounts);foreach(var c in pending){if(saved)Hide(c);else c.RetryAt=Time.time+1;}
            }
        }
        static void Hide(Coin c){c.Active=false;c.Root.gameObject.SetActive(false);}
        public void Clear(){foreach(var c in pool)Hide(c);}
        public CoinDropSnapshot[] Capture(){var result=new List<CoinDropSnapshot>();foreach(var c in pool)if(c.Active)result.Add(new CoinDropSnapshot{Id=c.Id,Amount=c.Amount,Position=c.Age<.48f?c.Landing:c.Root.position});return result.ToArray();}
        public void Restore(CoinDropSnapshot[] saved)
        {
            Clear();if(saved==null)return;
            foreach(var state in saved){
                if(state==null||MetaProgression.IsClaimed(game.CoinRunId,state.Id))continue;
                Coin c=null;foreach(var free in pool)if(!free.Active){c=free;break;}if(c==null){if(pool.Count>=Capacity)break;c=Create();pool.Add(c);}
                var p=IslandMap.ClampToLand(state.Position);p.y=0;Activate(c,state.Id,state.Amount,p+Vector3.up*.45f,p,.48f);
            }
        }
        static Mesh Disc()
        {
            const int n=32;var vertices=new Vector3[2+n*2];var triangles=new int[n*12];vertices[0]=new Vector3(0,0,-1);vertices[1]=new Vector3(0,0,1);
            for(int i=0;i<n;i++){
                float a=i*Mathf.PI*2/n;vertices[2+i]=new Vector3(Mathf.Cos(a),Mathf.Sin(a),-1);vertices[2+n+i]=new Vector3(Mathf.Cos(a),Mathf.Sin(a),1);
                int u=2+i,v=2+(i+1)%n,w=u+n,x=v+n,k=i*12;
                triangles[k]=0;triangles[k+1]=v;triangles[k+2]=u;triangles[k+3]=1;triangles[k+4]=w;triangles[k+5]=x;
                triangles[k+6]=u;triangles[k+7]=v;triangles[k+8]=x;triangles[k+9]=u;triangles[k+10]=x;triangles[k+11]=w;
            }
            var mesh=new Mesh{name="Оригинальная золотая монета",vertices=vertices,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        void OnDestroy(){if(disc!=null)Destroy(disc);if(gold!=null)Destroy(gold);if(face!=null)Destroy(face);}
    }
}
