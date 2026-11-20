using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Brainrot
{
    [Serializable] public sealed class HeartDropTuning
    {
        [Min(1),Tooltip("Здоровье от одного сердечка осьминога.")]public float Healing=10;
        [Range(5,120),Tooltip("Время ожидания подбора; пауза останавливает таймер.")]public float Lifetime=45;
        [Range(.5f,3),Tooltip("Расстояние подбора сердечка, м.")]public float PickupRadius=1.1f;
        [Range(1,5),Tooltip("Расстояние мягкого притяжения к раненому игроку, м.")]public float MagnetRadius=2.2f;
    }
    [Serializable] public sealed class HeartDropSnapshot
    {
        public Vector3 Position;public float Remaining;
    }
    /// <summary>Bounded scene-owned hearts. Death rewards only; no colliders, gameplay RNG or per-frame allocations.</summary>
    public sealed class HeartDrops:MonoBehaviour
    {
        public const int Capacity=32;
        sealed class Item {public Transform Root,Visual;public TextMesh Label;public Vector3 Start,Landing;public float Age,Remaining,CollectAge;public bool Active,Collected;}
        readonly List<Item> pool=new List<Item>(Capacity);
        Mesh mesh;Material heartMaterial;
        public int ActiveCount {get{int n=0;foreach(var item in pool)if(item.Active&&!item.Collected)n++;return n;}}
        public static bool Eligible(EnemyKind kind)=>kind==EnemyKind.BlueberrinniOctopusini;
        public void Drop(EnemyKind kind,Vector3 position)
        {
            if(!Eligible(kind))return;
            Item chosen=null;
            foreach(var item in pool)if(!item.Active){chosen=item;break;}
            if(chosen==null&&pool.Count>=Capacity){chosen=pool[0];foreach(var item in pool)if(item.Remaining<chosen.Remaining)chosen=item;}
            if(chosen==null){chosen=Create();pool.Add(chosen);}
            Vector3 landing=IslandMap.ClampToLand(position);landing.y=0;
            // Golden-angle scatter is cosmetic and never consumes the enemy spawning random stream.
            float angle=pool.IndexOf(chosen)*2.399963f+position.x*.31f;
            landing=IslandMap.ClampToLand(landing+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*.7f);landing.y=0;
            Activate(chosen,position+Vector3.up*.7f,landing,Mathf.Clamp(GameBalance.Current.Hearts.Lifetime,5,120),0);
        }
        void Activate(Item item,Vector3 start,Vector3 landing,float remaining,float age)
        {
            item.Start=start;item.Landing=landing;item.Age=age;item.Remaining=remaining;item.CollectAge=0;item.Collected=false;item.Active=true;
            item.Root.gameObject.SetActive(true);item.Visual.gameObject.SetActive(true);item.Label.gameObject.SetActive(false);item.Visual.localScale=Vector3.one*.48f;
            item.Root.position=start;item.Visual.localRotation=Quaternion.identity;
        }
        Item Create()
        {
            if(mesh==null)mesh=HeartMesh();
            if(heartMaterial==null){heartMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Коралловое сердечко"};heartMaterial.SetColor("_BaseColor",new Color(1,.12f,.25f));heartMaterial.SetFloat("_Smoothness",.42f);}
            var root=new GameObject("Сердечко осьминога").transform;root.SetParent(transform,false);
            var visual=new GameObject("Объёмное сердце",typeof(MeshFilter),typeof(MeshRenderer)).transform;visual.SetParent(root,false);
            visual.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=visual.GetComponent<MeshRenderer>();renderer.sharedMaterial=heartMaterial;renderer.shadowCastingMode=ShadowCastingMode.On;
            var label=new GameObject("Лечение +10",typeof(TextMesh)).GetComponent<TextMesh>();label.transform.SetParent(root,false);label.font=CartoonUI.Font;label.fontSize=48;label.characterSize=.045f;label.anchor=TextAnchor.MiddleCenter;label.color=new Color(.55f,1,.65f);
            // TextMesh requires the font atlas material to display Cyrillic/digits reliably.
            label.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;
            return new Item{Root=root,Visual=visual,Label=label};
        }
        public void Tick(float dt,PlayerController player)
        {
            if(dt<=0||player==null||player.Health<=0)return;
            var settings=GameBalance.Current.Hearts;
            float magnetRadius=Mathf.Max(settings.PickupRadius,settings.MagnetRadius),magnetSquare=magnetRadius*magnetRadius;
            var playerPosition=player.transform.position;
            var camera=player.ViewCamera;
            foreach(var item in pool){
                if(!item.Active)continue;
                if(item.Collected){
                    item.CollectAge+=dt;item.Root.position+=Vector3.up*(dt*1.2f);
                    item.Label.color=new Color(.55f,1,.65f,Mathf.Clamp01(1-item.CollectAge/.65f));
                    if(camera!=null)item.Label.transform.rotation=camera.transform.rotation;
                    else item.Label.transform.rotation=Quaternion.LookRotation(item.Root.position-playerPosition);
                    if(item.CollectAge>=.65f)Hide(item);continue;
                }
                item.Age+=dt;item.Remaining-=dt;if(item.Remaining<=0){Hide(item);continue;}
                float t=Mathf.Clamp01(item.Age/.65f);
                if(t<1)item.Root.position=Vector3.Lerp(item.Start,item.Landing+Vector3.up*.55f,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*1.35f);
                else{
                    var p=item.Root.position;p.y=.55f+Mathf.Sin(item.Age*3)*.07f;item.Root.position=p;
                    Vector3 delta=playerPosition-p;delta.y=0;
                    // An earlier heart in this same tick may already have filled the player's health.
                    if(player.Health<player.Stats.MaxHealth-.001f&&delta.sqrMagnitude<=magnetSquare){
                        float distance=delta.magnitude;
                        if(distance>settings.PickupRadius)item.Root.position+=delta/distance*Mathf.Min(distance,dt*5);
                        else{
                            float before=player.Health;player.Heal(Mathf.Max(1,settings.Healing));float healed=player.Health-before;
                            if(healed<=0)continue;
                            item.Collected=true;item.CollectAge=0;item.Visual.gameObject.SetActive(false);item.Label.text="+"+Mathf.CeilToInt(healed);item.Label.gameObject.SetActive(true);
                            item.Root.position=playerPosition+Vector3.up*2.2f;GameAudio.PlayHealing();HitEffects.Burst(playerPosition+Vector3.up*.7f,new Color(.5f,1,.65f),5,0);continue;
                        }
                    }
                }
                item.Visual.localRotation=Quaternion.Euler(8, item.Age*55,0);
                float fade=Mathf.Clamp01(item.Remaining/2);float pop=t<1?Mathf.Lerp(.3f,1,Mathf.Sin(t*Mathf.PI*.5f)):1+Mathf.Sin(item.Age*3)*.035f;
                item.Visual.localScale=Vector3.one*(.48f*pop*fade);
            }
        }
        static void Hide(Item item){item.Active=false;item.Root.gameObject.SetActive(false);}
        public void Clear(){foreach(var item in pool)Hide(item);}
        public HeartDropSnapshot[] Capture()
        {
            var result=new List<HeartDropSnapshot>();foreach(var item in pool)if(item.Active&&!item.Collected)result.Add(new HeartDropSnapshot{Position=item.Age<.65f?item.Landing:item.Root.position,Remaining=item.Remaining});return result.ToArray();
        }
        public void Restore(HeartDropSnapshot[] saved)
        {
            Clear();if(saved==null)return;
            foreach(var state in saved){if(state==null||pool.Count>=Capacity&&ActiveCount>=Capacity)break;
                Item item=null;foreach(var free in pool)if(!free.Active){item=free;break;}if(item==null){item=Create();pool.Add(item);}
                var p=IslandMap.ClampToLand(state.Position);p.y=0;Activate(item,p+Vector3.up*.55f,p,Mathf.Clamp(state.Remaining,.01f,120),.65f);
            }
        }
        static Mesh HeartMesh()
        {
            const int n=48;var vertices=new Vector3[2+n*2];var triangles=new int[n*12];vertices[0]=new Vector3(0,.12f,-.18f);vertices[1]=new Vector3(0,.12f,.18f);
            for(int i=0;i<n;i++){
                float t=i*Mathf.PI*2/n;float x=16*Mathf.Pow(Mathf.Sin(t),3)/18;float y=(13*Mathf.Cos(t)-5*Mathf.Cos(2*t)-2*Mathf.Cos(3*t)-Mathf.Cos(4*t))/18;
                vertices[2+i]=new Vector3(x,y,-.07f);vertices[2+n+i]=new Vector3(x,y,.07f);
                int a=2+i,b=2+(i+1)%n,c=a+n,d=b+n,k=i*12;
                triangles[k]=0;triangles[k+1]=b;triangles[k+2]=a;triangles[k+3]=1;triangles[k+4]=c;triangles[k+5]=d;
                triangles[k+6]=a;triangles[k+7]=b;triangles[k+8]=d;triangles[k+9]=a;triangles[k+10]=d;triangles[k+11]=c;
            }
            for(int i=0;i<triangles.Length;i+=3){int swap=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=swap;}
            var result=new Mesh{name="Оригинальное объёмное сердце",vertices=vertices,triangles=triangles};result.RecalculateNormals();result.RecalculateBounds();return result;
        }
        void OnDestroy(){if(mesh!=null)Destroy(mesh);if(heartMaterial!=null)Destroy(heartMaterial);}
    }
}
