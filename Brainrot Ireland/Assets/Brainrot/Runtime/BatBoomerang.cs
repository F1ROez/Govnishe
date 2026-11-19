using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Brainrot
{
    /// <summary>One scene-owned projectile. Each leg has independent receipts and continuous segment contacts.</summary>
    public sealed class BatBoomerang:MonoBehaviour
    {
        PlayerController player;CharacterVisual visual;Transform projectile;TrailRenderer trail;Mesh mesh;Material timber,ribbon;
        readonly HashSet<EnemyController> outwardHits=new HashSet<EnemyController>();
        readonly HashSet<EnemyController> returnHits=new HashSet<EnemyController>();
        Vector3 origin,end,previous,turn;float age,outSeconds,returnSeconds;bool returning;
        public bool Flying{get;private set;}
        public float Remaining=>Flying?Mathf.Max(0,outSeconds+returnSeconds-age):0;
        public void Initialize(PlayerController p,CharacterVisual v){player=p;visual=v;}
        Vector3 Hand=>visual!=null&&visual.Weapon!=null?visual.Weapon.position:player.transform.position+Vector3.up*1.3f;
        public bool Launch(){
            if(Flying||player==null||visual==null||GameManager.Instance==null||!GameManager.Instance.IsPlaying)return false;
            EnsureVisual();var tuning=GameBalance.Current.Boomerang;origin=Hand;
            var forward=player.AimForward;forward.y=0;if(forward.sqrMagnitude<.001f)forward=player.transform.forward;forward.Normalize();
            end=origin+forward*Mathf.Clamp(tuning.Distance,2,14);previous=origin;turn=end;age=0;returning=false;
            outSeconds=Mathf.Clamp(tuning.OutSeconds,.15f,1);returnSeconds=Mathf.Clamp(tuning.ReturnSeconds,.2f,1.5f);
            outwardHits.Clear();returnHits.Clear();Flying=true;projectile.position=origin;projectile.gameObject.SetActive(true);trail.Clear();trail.emitting=true;visual.SetWeaponThrown(true);return true;
        }
        void Update(){var game=GameManager.Instance;if(Flying&&(game==null||!game.IsPlaying||player==null||player.Health<=0||player.IsCaptured)){Cancel();return;}if(Flying)Tick(Time.deltaTime);}
        public void Tick(float delta){
            if(!Flying||delta<=0)return;
            float next=age+delta;
            // Split at the turn even when a long frame crosses both legs. Never skip the outgoing end.
            if(!returning&&next>=outSeconds){Sweep(previous,end,outwardHits);previous=turn=end;returning=true;}
            age=next;
            Vector3 target;
            if(!returning){float t=Mathf.Clamp01(age/outSeconds);target=Vector3.Lerp(origin,end,Mathf.Sin(t*Mathf.PI*.5f))+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.28f);Sweep(previous,target,outwardHits);}
            else{float t=Mathf.Clamp01((age-outSeconds)/returnSeconds);Vector3 hand=Hand;var flat=hand-turn;flat.y=0;var side=flat.sqrMagnitude>.001f?Vector3.Cross(Vector3.up,flat.normalized):Vector3.right;
                target=Vector3.Lerp(turn,hand,t)+side*(Mathf.Sin(t*Mathf.PI)*.70f)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.35f);Sweep(previous,target,returnHits);
            }
            if(!Flying)return;previous=target;projectile.position=target;projectile.rotation=Quaternion.Euler(15,age*1350,80);
            if(age>=outSeconds+returnSeconds){if(outwardHits.Count+returnHits.Count==0)GameManager.Instance?.ReportMiss();HitEffects.Burst(Hand,new Color(1,.87f,.35f),3,0);Cancel();}
        }
        public static bool SegmentHits(Vector3 a,Vector3 b,Vector3 center,float radius){
            Vector3 d=b-a;float t=d.sqrMagnitude>.000001f?Mathf.Clamp01(Vector3.Dot(center-a,d)/d.sqrMagnitude):0;
            return (center-a-d*t).sqrMagnitude<=radius*radius;
        }
        void Sweep(Vector3 a,Vector3 b,HashSet<EnemyController> receipts){
            var game=GameManager.Instance;if(game==null||!game.IsPlaying)return;var enemies=game.Spawner.Active;
            for(int i=enemies.Count-1;i>=0;i--){
                if(i>=enemies.Count)continue;var enemy=enemies[i];if(enemy==null||!enemy.Alive||receipts.Contains(enemy))continue;
                var body=enemy.BodyCollider;Vector3 center=body!=null?body.bounds.center:enemy.transform.position+Vector3.up;
                float radius=body!=null?Mathf.Max(body.bounds.extents.x,body.bounds.extents.z):.55f;
                // Horizontal swept body disc with a vertical capsule extent, not centre-only point tests.
                float low=body!=null?body.bounds.min.y:enemy.transform.position.y,high=body!=null?body.bounds.max.y:low+2;
                float minY=Mathf.Min(a.y,b.y),maxY=Mathf.Max(a.y,b.y),hitRadius=Mathf.Clamp(GameBalance.Current.Boomerang.HitRadius,.1f,.8f);
                if(maxY+hitRadius<low||minY-hitRadius>high)continue;
                Vector3 aa=a,bb=b;aa.y=bb.y=center.y;
                if(!SegmentHits(aa,bb,center,radius+hitRadius))continue;
                receipts.Add(enemy);bool critical=Random.value<player.Stats.CritChance;
                float damage=(player.Stats.Damage+player.Stats.FireDamage)*Mathf.Clamp(GameBalance.Current.Boomerang.DamageMultiplier,.1f,2)*(critical?2:1);
                float dealt=Mathf.Min(enemy.Health,damage);enemy.TakeDamage(damage,Vector3.zero);player.Heal(dealt*player.Stats.Lifesteal);
                HitEffects.Burst(center,new Color(1,.85f,.30f),5,.35f);game.ReportHit(critical,1);GameAudio.Impact(false,critical,1);
                if(!game.IsPlaying){Cancel();break;}
            }
        }
        public void Cancel(){Flying=false;if(trail!=null){trail.emitting=false;trail.Clear();}if(projectile!=null)projectile.gameObject.SetActive(false);if(visual!=null)visual.SetWeaponThrown(false);outwardHits.Clear();returnHits.Clear();}
        void EnsureVisual(){
            if(projectile!=null)return;
            projectile=new GameObject("Бита-бумеранг",typeof(MeshFilter),typeof(MeshRenderer)).transform;projectile.SetParent(transform,false);
            mesh=MakeBat();projectile.GetComponent<MeshFilter>().sharedMesh=mesh;timber=WorldBuilder.Material(new Color(.98f,.56f,.14f),"Бумеранг — карамельное дерево");projectile.GetComponent<MeshRenderer>().sharedMaterial=timber;projectile.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            var tip=new GameObject("Бирюзовый след").transform;tip.SetParent(projectile,false);tip.localPosition=Vector3.up*.62f;
            trail=tip.gameObject.AddComponent<TrailRenderer>();ribbon=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")??Shader.Find("Sprites/Default"));ribbon.color=new Color(.12f,.95f,1,.65f);ribbon.renderQueue=3000;
            ribbon.SetFloat("_Surface",1);ribbon.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);ribbon.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);ribbon.SetInt("_ZWrite",0);ribbon.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            trail.sharedMaterial=ribbon;trail.time=.14f;trail.startWidth=.16f;trail.endWidth=0;trail.minVertexDistance=.08f;trail.shadowCastingMode=ShadowCastingMode.Off;trail.receiveShadows=false;
        }
        static Mesh MakeBat(){
            const int n=16;float[] heights={-.55f,-.51f,-.43f,-.12f,.05f,.38f,.53f,.58f};float[] radii={.075f,.085f,.045f,.055f,.095f,.135f,.13f,.025f};
            var vertices=new Vector3[n*heights.Length+2];var triangles=new int[n*(heights.Length-1)*6+n*6];
            for(int ring=0;ring<heights.Length;ring++)for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;vertices[ring*n+i]=new Vector3(Mathf.Cos(a)*radii[ring],heights[ring],Mathf.Sin(a)*radii[ring]);}
            int at=0;for(int ring=0;ring<heights.Length-1;ring++)for(int i=0;i<n;i++){int a=ring*n+i,b=ring*n+(i+1)%n,c=a+n,d=b+n;triangles[at++]=a;triangles[at++]=c;triangles[at++]=b;triangles[at++]=b;triangles[at++]=c;triangles[at++]=d;}
            int bottom=vertices.Length-2,top=vertices.Length-1;vertices[bottom]=Vector3.up*heights[0];vertices[top]=Vector3.up*heights[heights.Length-1];
            for(int i=0;i<n;i++){triangles[at++]=bottom;triangles[at++]=i;triangles[at++]=(i+1)%n;triangles[at++]=top;triangles[at++]=(heights.Length-1)*n+(i+1)%n;triangles[at++]=(heights.Length-1)*n+i;}
            var result=new Mesh{name="Общая сетка летящей деревянной биты"};result.vertices=vertices;result.triangles=triangles;result.RecalculateNormals();result.RecalculateBounds();return result;
        }
        void OnDisable(){Cancel();}
        void OnDestroy(){Cancel();if(projectile!=null)Destroy(projectile.gameObject);if(mesh!=null)Destroy(mesh);if(timber!=null)Destroy(timber);if(ribbon!=null)Destroy(ribbon);}
    }
}
