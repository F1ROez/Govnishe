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
    /// <summary>Scene-owned bounded coin pool. Every visible coin is exactly one currency unit; no reward is paid without pickup.</summary>
    public sealed class CoinDrops:MonoBehaviour
    {
        public const int Capacity=512;
        sealed class Coin {public Transform Root,Face;public int Id,Amount;public Vector3 Start,Landing,FlightStart;public float Age,Fly,RetryAt;public bool Active,Collecting,Landed;}
        readonly List<Coin> pending=new List<Coin>(Capacity);
        readonly List<Coin> pool=new List<Coin>(Capacity);
        readonly int[] pickupIds=new int[Capacity],pickupAmounts=new int[Capacity];
        Mesh disc;Material gold,face;
        GameManager game;
        public int ActiveCount{get{int n=0;foreach(var item in pool)if(item.Active)n++;return n;}}
        public void Initialize(GameManager owner){game=owner;}
        /// <summary>Stable receipt for one of at most 100 physical coins belonging to a kill.</summary>
        public static int RewardId(int killOrdinal,int coinIndex)
        {
            long value=((long)killOrdinal-1)*128+coinIndex+1;
            return killOrdinal>0&&coinIndex>=0&&coinIndex<100&&value<=int.MaxValue?(int)value:0;
        }
        public void Drop(EnemyKind kind,Vector3 position,int killOrdinal)
        {
            if(game==null||!game.IsPlaying||killOrdinal<1)return;
            int count=GameBalance.Current.Coins.Value(kind);
            for(int index=0;index<count;index++){
                int id=RewardId(killOrdinal,index);
                if(id<=0||MetaProgression.IsClaimed(game.CoinRunId,id))continue;
                // A repeated death notification cannot duplicate uncollected physical coins either.
                bool alreadyPresent=false;foreach(var existing in pool)if(existing.Active&&existing.Id==id){alreadyPresent=true;break;}
                if(alreadyPresent)continue;
                Coin coin=null;foreach(var item in pool)if(!item.Active){coin=item;break;}
                if(coin==null&&pool.Count>=Capacity){
                    // Recycle only an uncollected visual. Never bank a coin the player did not pick up.
                    foreach(var item in pool)if(!item.Collecting&&(coin==null||item.Age>coin.Age))coin=item;
                    if(coin==null)continue;
                    Hide(coin);
                }
                if(coin==null){coin=Create();pool.Add(coin);}
                float a=killOrdinal*2.399963f+index*Mathf.PI*2/Mathf.Max(1,count);
                float radius=.65f+(index%3)*.28f;
                var landing=IslandMap.ClampToLand(position+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius);landing.y=0;
                Activate(coin,id,1,position+Vector3.up*(.75f+(index%3)*.10f),landing,-(index%8)*.018f);
            }
        }
        void Activate(Coin c,int id,int amount,Vector3 start,Vector3 landing,float age)
        {
            c.Id=id;c.Amount=1;c.Start=start;c.Landing=landing;c.Age=age;c.Fly=0;c.RetryAt=0;c.Collecting=false;c.Active=true;c.Landed=age>=.48f;
            c.Root.gameObject.SetActive(true);c.Root.position=start;c.Root.localScale=Vector3.one;
        }
        Coin Create()
        {
            if(disc==null)disc=CoinMesh();
            if(gold==null){gold=WorldBuilder.Material(new Color(1,.64f,.055f),"Золотой ободок монеты");gold.SetFloat("_Smoothness",.48f);face=WorldBuilder.Material(new Color(1,.83f,.22f),"Тёплое золото монеты");face.SetFloat("_Smoothness",.30f);}
            var root=new GameObject("Золотая монета").transform;root.SetParent(transform,false);
            var body=new GameObject("Чеканка",typeof(MeshFilter),typeof(MeshRenderer)).transform;body.SetParent(root,false);
            body.GetComponent<MeshFilter>().sharedMesh=disc;var renderer=body.GetComponent<MeshRenderer>();renderer.sharedMaterials=new[]{gold,face};renderer.shadowCastingMode=ShadowCastingMode.Off;
            return new Coin{Root=root,Face=body};
        }
        // Bake the rim, two faces and embossed mark once. Every token shares one mesh/renderer,
        // rather than four separate renderers and their transforms for a dense reward pile.
        static Mesh CoinMesh()
        {
            var unit=Disc();var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.SetActive(false);
            var rim=new Mesh();var faces=new Mesh();
            try{
                rim.CombineMeshes(new[]{
                    new CombineInstance{mesh=unit,transform=Matrix4x4.Scale(new Vector3(.38f,.38f,.11f))},
                    new CombineInstance{mesh=cube.GetComponent<MeshFilter>().sharedMesh,transform=Matrix4x4.Scale(new Vector3(.057f,.3496f,.264f))}
                },true,true);
                faces.CombineMeshes(new[]{
                    new CombineInstance{mesh=unit,transform=Matrix4x4.TRS(new Vector3(0,0,-.1122f),Quaternion.identity,new Vector3(.3002f,.3002f,.0154f))},
                    new CombineInstance{mesh=unit,transform=Matrix4x4.TRS(new Vector3(0,0,.1122f),Quaternion.identity,new Vector3(.3002f,.3002f,.0154f))}
                },true,true);
                var result=new Mesh{name="Общая чеканная монета — ободок и лицо"};
                result.CombineMeshes(new[]{new CombineInstance{mesh=rim},new CombineInstance{mesh=faces}},false,false);result.RecalculateBounds();return result;
            }finally{Destroy(cube);Destroy(unit);Destroy(rim);Destroy(faces);}
        }
        public void Tick(float dt,PlayerController player)
        {
            if(game==null||!game.IsPlaying||player==null||player.Health<=0||dt<=0)return;
            pending.Clear();var tuning=GameBalance.Current.Coins;float radius=Mathf.Max(tuning.PickupRadius,tuning.MagnetRadius);
            float radiusSquared=radius*radius;var playerPosition=player.transform.position;
            foreach(var c in pool){
                if(!c.Active)continue;
                if(MetaProgression.IsClaimed(game.CoinRunId,c.Id)){Hide(c);continue;}
                c.Age+=dt;
                if(c.Collecting){
                    c.Fly=Mathf.Min(1,c.Fly+dt/.20f);float eased=c.Fly*c.Fly;
                    var target=playerPosition+Vector3.up*1.1f;
                    c.Root.position=Vector3.Lerp(c.FlightStart,target,eased)+Vector3.up*(Mathf.Sin(c.Fly*Mathf.PI)*.28f);c.Root.localScale=Vector3.one*Mathf.Lerp(1,.15f,eased);
                    if(c.Fly>=1&&Time.time>=c.RetryAt)pending.Add(c);
                    continue;
                }
                float t=Mathf.Clamp01(c.Age/.48f);
                if(t<1)c.Root.position=Vector3.Lerp(c.Start,c.Landing+Vector3.up*.45f,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*1.1f);
                else{
                    if(!c.Landed){c.Root.position=c.Landing+Vector3.up*.45f;c.Landed=true;}
                    var p=c.Root.position;p.y=.45f+Mathf.Sin(c.Age*3.2f+c.Id)*.075f;c.Root.position=p;
                    var delta=playerPosition-p;delta.y=0;
                    if(delta.sqrMagnitude<=radiusSquared){
                        float d=delta.magnitude;
                        float speed=Mathf.Lerp(3,13,1-Mathf.Clamp01(d/radius));
                        if(d<=Mathf.Max(.2f,tuning.PickupRadius)+speed*dt){c.Collecting=true;c.FlightStart=p;c.Fly=0;}
                        else c.Root.position+=delta/d*(speed*dt);
                    }
                }
                c.Face.localRotation=Quaternion.Euler(8,c.Age*115+c.Id*17,0);
                c.Root.localScale=Vector3.one*(1+Mathf.Sin(c.Age*3+c.Id)*.035f);
            }
            // One atomic in-memory transaction per cluster; no arrays or disk writes on pickup.
            if(pending.Count>0){
                for(int i=0;i<pending.Count;i++){pickupIds[i]=pending[i].Id;pickupAmounts[i]=pending[i].Amount;}
                bool saved=game.TryCollectCoins(pickupIds,pickupAmounts,pending.Count);foreach(var c in pending){if(saved)Hide(c);else c.RetryAt=Time.time+1;}
                if(saved)HitEffects.Burst(player.transform.position+Vector3.up*.8f,new Color(1,.83f,.30f),3,0);
            }
        }
        static void Hide(Coin c){c.Active=false;c.Root.gameObject.SetActive(false);}
        public void Clear(){foreach(var c in pool)Hide(c);}
        public CoinDropSnapshot[] Capture(){var result=new List<CoinDropSnapshot>();foreach(var c in pool)if(c.Active)result.Add(new CoinDropSnapshot{Id=c.Id,Amount=c.Amount,Position=c.Age<.48f?c.Landing:c.Root.position});return result.ToArray();}
        public void Restore(CoinDropSnapshot[] saved)
        {
            Clear();if(saved==null)return;
            foreach(var state in saved){
                if(state==null||state.Amount!=1||MetaProgression.IsClaimed(game.CoinRunId,state.Id))continue;
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
