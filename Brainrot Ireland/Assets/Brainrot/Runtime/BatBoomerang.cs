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
        Vector3 origin,end,previous,turn,returnControlA,returnControlB;float age,outSeconds,returnSeconds,legAge,returnHeight,lifetime,hitRadius;bool returning,piercing;
        public bool Flying{get;private set;}
        public float Remaining=>Flying?Mathf.Max(0,(returning?returnSeconds:outSeconds+returnSeconds)-legAge):0;
        public bool Piercing=>piercing;
        public bool Returning=>returning;
        public Vector3 ProjectilePosition=>projectile!=null?projectile.position:Vector3.zero;
        public void Initialize(PlayerController p,CharacterVisual v){player=p;visual=v;}
        Vector3 Hand=>visual!=null&&visual.Weapon!=null?visual.Weapon.position:player.transform.position+Vector3.up*1.3f;
        public bool Launch(){
            if(Flying||player==null||visual==null||GameManager.Instance==null||!GameManager.Instance.IsPlaying)return false;
            EnsureVisual();var tuning=GameBalance.Current.Boomerang;origin=Hand;
            var forward=player.AimForward;forward.y=0;if(forward.sqrMagnitude<.001f)forward=player.transform.forward;forward.Normalize();
            end=origin+forward*Mathf.Clamp(tuning.Distance,2,14);previous=origin;turn=end;age=legAge=lifetime=0;returning=false;piercing=MetaProgression.Rank(4)>0;
            outSeconds=Mathf.Clamp(tuning.OutSeconds,.15f,1);returnSeconds=Mathf.Clamp(tuning.ReturnSeconds,.2f,1.5f);
            hitRadius=Mathf.Clamp(tuning.HitRadius,.1f,.8f);outwardHits.Clear();returnHits.Clear();Flying=true;projectile.position=origin;projectile.gameObject.SetActive(true);trail.Clear();trail.emitting=true;visual.SetWeaponThrown(true);return true;
        }
        void Update(){var game=GameManager.Instance;if(Flying&&(game==null||!game.IsPlaying||player==null||player.Health<=0||player.IsCaptured)){Cancel();return;}if(Flying)Tick(Time.deltaTime);}
        public void Tick(float delta){
            if(!Flying||delta<=0)return;
            lifetime+=delta;if(lifetime>=4){Catch();return;}
            float remaining=delta;int turns=0;
            // Each step is split at the turnaround, including the early impact turnaround.
            while(Flying&&remaining>.000001f&&turns++<32){
                if(age>=4){Catch();return;}
                float duration=returning?returnSeconds:outSeconds;
                float step=Mathf.Min(.04f,Mathf.Min(remaining,Mathf.Max(.000001f,duration-legAge)));
                float nextAge=Mathf.Min(duration,legAge+step);float t=Mathf.Clamp01(nextAge/duration);
                Vector3 target;
                if(!returning)target=Vector3.Lerp(origin,end,Mathf.Sin(t*Mathf.PI*.5f))+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.28f);
                else if(piercing){
                    var flat=Hand-turn;flat.y=0;var side=flat.sqrMagnitude>.001f?Vector3.Cross(Vector3.up,flat.normalized):Vector3.right;
                    target=Vector3.Lerp(turn,Hand,t)+side*(Mathf.Sin(t*Mathf.PI)*.70f)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.35f);
                }else{
                    // After a bounce, lift above the crowd before descending to the moving hand.
                    float u=1-t;target=turn*(u*u*u)+returnControlA*(3*u*u*t)+returnControlB*(3*u*t*t)+Hand*(t*t*t);
                }
                age+=step;remaining-=step;
                if(piercing)SweepPiercing(previous,target,returning?returnHits:outwardHits);
                else if(TryFirstContact(previous,target,out var enemy,out var hit,out var normal)){
                    var segment=target-previous;
                    float fraction=segment.sqrMagnitude>.000001f?Mathf.Clamp01(Vector3.Dot(hit-previous,segment)/segment.sqrMagnitude):0;
                    float unused=Mathf.Max(0,step-Mathf.Max(.0001f,step*fraction));age-=unused;remaining+=unused;
                    previous=hit+normal*.025f;projectile.position=previous;
                    if(!returning){Damage(enemy,outwardHits);if(!Flying)return;BeginReturn(previous,normal);}
                    else BeginReturn(previous,normal); // Re-route around a body, never through it or into another damaging pass.
                    continue;
                }
                if(!Flying)return;
                legAge=nextAge;previous=target;projectile.position=target;projectile.rotation=Quaternion.Euler(15,age*1350,80);
                if(legAge>=duration-.000001f){
                    if(!returning)BeginReturn(target,(origin-end).normalized);
                    else{if(outwardHits.Count+returnHits.Count==0)GameManager.Instance?.ReportMiss();Catch();}
                }
                // Failsafe for a player standing inside a crowd: catch without a phantom damage sweep.
                if(Flying&&age>4){HitEffects.Burst(Hand,new Color(1,.87f,.35f),3,0);Cancel();}
            }
        }
        void BeginReturn(Vector3 point,Vector3 away){
            returning=true;legAge=0;turn=point;previous=point;
            var game=GameManager.Instance;returnHeight=Mathf.Max(point.y,Hand.y)+1.4f;
            if(game!=null&&game.Spawner!=null)foreach(var enemy in game.Spawner.Active){
                if(enemy==null||!enemy.Alive)continue;var body=enemy.BodyCollider;
                returnHeight=Mathf.Max(returnHeight,(body!=null?body.bounds.max.y:enemy.transform.position.y+2)+.9f);
            }
            away.y=0;if(away.sqrMagnitude<.001f){away=Hand-point;away.y=0;}
            away=away.sqrMagnitude>.001f?away.normalized:-player.AimForward;
            returnControlA=point+away*1.1f;returnControlA.y=returnHeight;
            returnControlB=Hand;returnControlB.y=returnHeight;
        }
        /// <summary>First entry of a swept horizontal disc, not nearest centre or active-list order.</summary>
        public static bool SegmentEntry(Vector3 a,Vector3 b,Vector3 center,float radius,out float entry){
            a.y=b.y=center.y=0;var delta=b-a;var offset=a-center;float c=Vector3.Dot(offset,offset)-radius*radius;
            entry=0;if(c<=0)return true;float aa=Vector3.Dot(delta,delta);if(aa<.000001f)return false;
            float bb=Vector3.Dot(offset,delta),disc=bb*bb-aa*c;if(disc<0)return false;
            entry=(-bb-Mathf.Sqrt(disc))/aa;return entry>=0&&entry<=1;
        }
        public static bool SegmentHits(Vector3 a,Vector3 b,Vector3 center,float radius){
            Vector3 d=b-a;float t=d.sqrMagnitude>.000001f?Mathf.Clamp01(Vector3.Dot(center-a,d)/d.sqrMagnitude):0;
            return (center-a-d*t).sqrMagnitude<=radius*radius;
        }
        bool Contact(EnemyController enemy,Vector3 a,Vector3 b,out float entry,out Vector3 center){
            var body=enemy.BodyCollider;center=body!=null?body.bounds.center:enemy.transform.position+Vector3.up;
            float radius=body!=null?Mathf.Max(body.bounds.extents.x,body.bounds.extents.z):.55f;
            float low=body!=null?body.bounds.min.y:enemy.transform.position.y,high=body!=null?body.bounds.max.y:low+2;
            entry=0;
            if(Mathf.Max(a.y,b.y)+hitRadius<low||Mathf.Min(a.y,b.y)-hitRadius>high)return false;
            if(!SegmentEntry(a,b,center,radius+hitRadius,out entry))return false;
            var flat=b-a;flat.y=0;var offset=a-center;offset.y=0;
            float aa=flat.sqrMagnitude,exit=1;
            if(aa>.000001f){float bb=Vector3.Dot(offset,flat),cc=offset.sqrMagnitude-(radius+hitRadius)*(radius+hitRadius);float disc=bb*bb-aa*cc;if(disc<0)return false;exit=Mathf.Min(1,(-bb+Mathf.Sqrt(disc))/aa);}
            float vertical=b.y-a.y;
            if(Mathf.Abs(vertical)<.000001f)return a.y+hitRadius>=low&&a.y-hitRadius<=high;
            float y0=(low-hitRadius-a.y)/vertical,y1=(high+hitRadius-a.y)/vertical;
            entry=Mathf.Max(entry,Mathf.Min(y0,y1));exit=Mathf.Min(exit,Mathf.Max(y0,y1));return entry<=exit&&entry>=0&&entry<=1;
        }
        bool TryFirstContact(Vector3 a,Vector3 b,out EnemyController found,out Vector3 point,out Vector3 normal){
            found=null;point=b;normal=Vector3.zero;var game=GameManager.Instance;if(game==null||!game.IsPlaying)return false;
            float first=float.PositiveInfinity;
            foreach(var enemy in game.Spawner.Active){
                if(enemy==null||!enemy.Alive||!Contact(enemy,a,b,out float entry,out var center)||entry>=first)continue;
                if(returning&&entry<=.000001f&&LeavingInitialOverlap(a,b,center))continue;
                first=entry;found=enemy;point=Vector3.Lerp(a,b,entry);normal=point-center;normal.y=0;
            }
            if(found==null)return false;
            if(normal.sqrMagnitude<.000001f){normal=a-b;normal.y=0;}normal=normal.sqrMagnitude>.000001f?normal.normalized:-player.AimForward;return true;
        }
        bool LeavingInitialOverlap(Vector3 a,Vector3 b,Vector3 center){
            // The harmless return may escape an overlap at its starting point. Re-bouncing at
            // t=0 pins the bat in a crowd and repeatedly rebuilds the path until the failsafe.
            var flat=a-center;flat.y=0;var movement=b-a;var horizontal=movement;horizontal.y=0;
            if(Vector3.Dot(flat,horizontal)>0)return true;
            return (a.y>=center.y&&movement.y>0)||(a.y<center.y&&movement.y<0);
        }
        void SweepPiercing(Vector3 a,Vector3 b,HashSet<EnemyController> receipts){
            var game=GameManager.Instance;if(game==null||!game.IsPlaying)return;var enemies=game.Spawner.Active;
            for(int i=enemies.Count-1;i>=0;i--){
                if(i>=enemies.Count)continue;var enemy=enemies[i];
                if(enemy==null||!enemy.Alive||receipts.Contains(enemy)||!Contact(enemy,a,b,out _,out _))continue;
                Damage(enemy,receipts);if(!Flying)break;
            }
        }
        void Damage(EnemyController enemy,HashSet<EnemyController> receipts){
            if(enemy==null||!enemy.Alive||!receipts.Add(enemy))return;
            var game=GameManager.Instance;var body=enemy.BodyCollider;Vector3 center=body!=null?body.bounds.center:enemy.transform.position+Vector3.up;
            bool critical=Random.value<player.Stats.CritChance;
            float damage=(player.Stats.Damage+player.Stats.FireDamage)*Mathf.Clamp(GameBalance.Current.Boomerang.DamageMultiplier,.1f,2)*(critical?2:1);
            float dealt=Mathf.Min(enemy.Health,damage);enemy.TakeDamage(damage,Vector3.zero);player.Heal(dealt*player.Stats.Lifesteal);
            HitEffects.Burst(center,new Color(1,.85f,.30f),5,.35f);game.ReportHit(critical,1);GameAudio.Impact(false,critical,1);
            if(!game.IsPlaying)Cancel();
        }
        void Catch(){if(Flying&&player!=null){HitEffects.Burst(Hand,new Color(1,.87f,.35f),3,0);GameAudio.PlayCatch();}Cancel();}
        public void Cancel(){Flying=false;if(trail!=null){trail.emitting=false;trail.Clear();}if(projectile!=null)projectile.gameObject.SetActive(false);if(visual!=null)visual.SetWeaponThrown(false);outwardHits.Clear();returnHits.Clear();}
        void EnsureVisual(){
            if(projectile!=null)return;
            projectile=new GameObject("Бита-бумеранг",typeof(MeshFilter),typeof(MeshRenderer)).transform;projectile.SetParent(GameManager.Instance!=null?GameManager.Instance.transform:null,false);
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
