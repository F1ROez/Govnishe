using UnityEngine;
using UnityEngine.Rendering;
namespace Brainrot
{
    /// <summary>Bounded encounter-owned ground marks. The visible geometry is also the damage geometry.</summary>
    public sealed class BossArenaEffects:MonoBehaviour
    {
        const int Slots=16;
        static readonly Vector2[] unitCircle=BuildCircle();
        static Vector2[] BuildCircle(){var points=new Vector2[49];for(int i=0;i<points.Length;i++){float a=i*Mathf.PI*2/(points.Length-1);points[i]=new Vector2(Mathf.Cos(a),Mathf.Sin(a));}return points;}
        sealed class Mark {
            public LineRenderer Edge,Border;public MeshRenderer Fill;public Transform Droplet;public Transform[] Limbs;
            public readonly Vector3[] CirclePoints=new Vector3[49],LanePoints=new Vector3[6],ConePoints=new Vector3[27];
            public readonly MaterialPropertyBlock Block=new MaterialPropertyBlock();
            public void Hide(){if(Edge!=null)Edge.enabled=Border.enabled=false;if(Fill!=null)Fill.enabled=false;if(Droplet!=null)Droplet.gameObject.SetActive(false);if(Limbs!=null)foreach(var limb in Limbs)limb.gameObject.SetActive(false);}
        }
        enum HazardType {None,Wave,Ink,Tentacle}
        sealed class Hazard {public HazardType Type;public Mark Mark;public Vector3 Center,Origin;public float Age,Delay,Radius,Speed,Life,Damage,Slow;public bool Hit,Shown;}
        readonly Mark[] warnings=new Mark[4];readonly Hazard[] hazards=new Hazard[Slots];
        BossEncounter boss;Material material,limbMaterial,inkMaterial;Mesh disc;Vector3 previousPlayer;bool tracked;
        public int ActiveHazards{get{int count=0;foreach(var item in hazards)if(item!=null&&item.Type!=HazardType.None)count++;return count;}}
        static readonly int BaseColor=Shader.PropertyToID("_BaseColor"),ColorId=Shader.PropertyToID("_Color");
        public void Initialize(BossEncounter value){boss=value;Clear();}
        Material SharedMaterial(){
            if(material!=null)return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Sprites/Default")){name="Предупреждения островных боссов",renderQueue=3020};
            material.SetFloat("_Surface",1);material.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.SetOverrideTag("RenderType","Transparent");return material;
        }
        Mark CreateMark(string title){
            var root=new GameObject(title).transform;root.SetParent(transform,false);var mark=new Mark();
            mark.Border=Line(root,"Тёмная подложка");mark.Edge=Line(root,"Контур опасности");mark.Hide();return mark;
        }
        LineRenderer Line(Transform parent,string title){
            var renderer=new GameObject(title).AddComponent<LineRenderer>();renderer.transform.SetParent(parent,false);
            renderer.sharedMaterial=SharedMaterial();renderer.useWorldSpace=true;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.numCapVertices=2;renderer.numCornerVertices=2;return renderer;
        }
        static Color ColorFor(BossWarning type)=>type==BossWarning.Charge?new Color(1,.15f,.12f):type==BossWarning.Special?new Color(.08f,.91f,1):new Color(1,.49f,.07f);
        void Draw(Mark mark,Vector3[] points,Color color,float width=.13f){
            mark.Edge.enabled=mark.Border.enabled=true;mark.Edge.positionCount=mark.Border.positionCount=points.Length;
            mark.Edge.widthMultiplier=width;mark.Border.widthMultiplier=width+.09f;
            mark.Edge.SetPositions(points);mark.Border.SetPositions(points);
            mark.Block.SetColor(BaseColor,new Color(.035f,.07f,.08f,.85f));mark.Block.SetColor(ColorId,new Color(.035f,.07f,.08f,.85f));mark.Border.SetPropertyBlock(mark.Block);
            mark.Block.SetColor(BaseColor,color);mark.Block.SetColor(ColorId,color);mark.Edge.SetPropertyBlock(mark.Block);
        }
        void CircleMark(Mark mark,Vector3 center,float radius,Color color,float width=.13f){
            center.y=.11f;
            for(int i=0;i<mark.CirclePoints.Length;i++)mark.CirclePoints[i]=center+new Vector3(unitCircle[i].x,0,unitCircle[i].y)*radius;
            Draw(mark,mark.CirclePoints,color,width);
        }
        Mark Warning(int slot){slot=Mathf.Clamp(slot,0,warnings.Length-1);return warnings[slot]??(warnings[slot]=CreateMark("Предупреждение босса "+slot));}
        public void Circle(int slot,Vector3 center,float radius,float progress,BossWarning type){
            var color=Color.Lerp(ColorFor(type),Color.white,Mathf.Clamp01(progress)*.24f);CircleMark(Warning(slot),center,radius,color);
        }
        public void Lane(int slot,Vector3 origin,Vector3 direction,float length,float halfWidth,float progress){
            var mark=Warning(slot);origin.y=.11f;direction.y=0;direction=direction.sqrMagnitude>.0001f?direction.normalized:Vector3.forward;
            var side=Vector3.Cross(Vector3.up,direction)*Mathf.Max(.1f,halfWidth);var end=origin+direction*Mathf.Max(0,length);var p=mark.LanePoints;
            p[0]=origin-side;p[1]=end-side;p[2]=end;p[3]=end+side;p[4]=origin+side;p[5]=p[0];Draw(mark,p,Color.Lerp(ColorFor(BossWarning.Charge),Color.white,Mathf.Clamp01(progress)*.2f));
        }
        public void Cone(int slot,Vector3 origin,Vector3 direction,float range,float degrees,float progress){
            var mark=Warning(slot);origin.y=.11f;direction.y=0;direction=direction.sqrMagnitude>.0001f?direction.normalized:Vector3.forward;var side=Vector3.Cross(Vector3.up,direction);var p=mark.ConePoints;p[0]=p[26]=origin;
            for(int i=0;i<25;i++){float angle=Mathf.Lerp(-degrees*.5f,degrees*.5f,i/24f)*Mathf.Deg2Rad;p[i+1]=origin+(direction*Mathf.Cos(angle)+side*Mathf.Sin(angle))*range;}
            Draw(mark,p,Color.Lerp(ColorFor(BossWarning.Area),Color.white,Mathf.Clamp01(progress)*.2f));
        }
        public void HideWarnings(){foreach(var mark in warnings)mark?.Hide();}
        Hazard Reserve(HazardType type,Vector3 center,float radius){
            for(int i=0;i<hazards.Length;i++){
                var item=hazards[i];if(item==null){item=new Hazard{Mark=CreateMark("Зона босса "+i)};hazards[i]=item;}
                if(item.Type!=HazardType.None)continue;
                item.Mark.Hide();center.y=0;item.Type=type;item.Center=center;item.Radius=Mathf.Clamp(radius,.2f,18);item.Age=0;item.Hit=item.Shown=false;item.Delay=0;item.Damage=1;return item;
            }
            return null;
        }
        public void Shockwave(Vector3 center,float radius,float speed,float multiplier,float delay){
            var h=Reserve(HazardType.Wave,center,radius);if(h==null)return;h.Speed=Mathf.Clamp(speed,2,18);h.Delay=Mathf.Max(0,delay);h.Life=h.Delay+h.Radius/h.Speed+.12f;h.Damage=Mathf.Max(0,multiplier);
            CircleMark(h.Mark,center,.2f,ColorFor(BossWarning.Area),.22f);
        }
        public void Ink(Vector3 center,float radius,float duration,float slow){
            var h=Reserve(HazardType.Ink,center,radius);if(h==null)return;h.Life=.32f+Mathf.Clamp(duration,1,12);h.Origin=boss.Position+Vector3.up*Mathf.Max(1.5f,boss.Enemy.BodyCollider.height*.7f);h.Slow=Mathf.Clamp(slow,.35f,1);EnsureFill(h.Mark);EnsureDroplet(h.Mark);
            CircleMark(h.Mark,center,h.Radius,new Color(.23f,.24f,.68f));
        }
        public void Tentacle(Vector3 center,float radius,float delay,float multiplier){
            var h=Reserve(HazardType.Tentacle,center,radius);if(h==null)return;h.Delay=Mathf.Max(.45f,delay);h.Life=h.Delay+.55f;h.Damage=Mathf.Max(0,multiplier);CircleMark(h.Mark,center,h.Radius,ColorFor(BossWarning.Area));
        }
        void EnsureFill(Mark mark){
            if(mark.Fill!=null)return;
            if(disc==null){
                const int n=48;var vertices=new Vector3[n+1];var triangles=new int[n*3];for(int i=0;i<n;i++){float angle=i*Mathf.PI*2/n;vertices[i+1]=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));triangles[i*3]=0;triangles[i*3+1]=1+(i+1)%n;triangles[i*3+2]=i+1;}
                disc=new Mesh{name="Общая чернильная лужа",vertices=vertices,triangles=triangles};disc.RecalculateNormals();disc.RecalculateBounds();
            }
            var go=new GameObject("Чернильная поверхность",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(mark.Edge.transform.parent,false);go.GetComponent<MeshFilter>().sharedMesh=disc;mark.Fill=go.GetComponent<MeshRenderer>();mark.Fill.sharedMaterial=SharedMaterial();mark.Fill.shadowCastingMode=ShadowCastingMode.Off;mark.Fill.receiveShadows=false;
        }
        void EnsureDroplet(Mark mark){
            if(mark.Droplet!=null)return;
            if(inkMaterial==null)inkMaterial=WorldBuilder.Material(new Color(.12f,.10f,.32f),"Большие капли чернил");
            mark.Droplet=WorldBuilder.Primitive("Капля чернил",PrimitiveType.Sphere,mark.Edge.transform.parent,Vector3.zero,Vector3.one*.65f,inkMaterial).transform;
            mark.Droplet.gameObject.SetActive(false);
        }
        void TentacleVisual(Hazard h,float progress){
            var mark=h.Mark;if(mark.Limbs==null){
                mark.Limbs=new Transform[5];if(limbMaterial==null)limbMaterial=WorldBuilder.Material(new Color(.17f,.48f,.86f),"Босс — щупальце");
                for(int i=0;i<5;i++){var go=WorldBuilder.Primitive("Сегмент щупальца",PrimitiveType.Capsule,mark.Edge.transform.parent,Vector3.zero,Vector3.one,limbMaterial);mark.Limbs[i]=go.transform;}
            }
            float lift=Mathf.Sin(Mathf.Clamp01(progress)*Mathf.PI)*3.8f;
            for(int i=0;i<mark.Limbs.Length;i++){var limb=mark.Limbs[i];limb.gameObject.SetActive(true);float t=(i+.5f)/5;limb.position=h.Center+Vector3.up*(lift*t)+Vector3.right*(t*t*lift*.2f);limb.localScale=new Vector3(.65f*(1-t*.7f),Mathf.Max(.02f,lift*.15f),.65f*(1-t*.7f));limb.rotation=Quaternion.Euler(0,0,-t*22);}
        }
        public void Tick(float dt){
            if(boss==null||boss.Player==null||dt<=0)return;var player=boss.Player;player.ResetBossSlow();var position=player.transform.position;
            foreach(var h in hazards){
                if(h==null||h.Type==HazardType.None)continue;
                float oldAge=h.Age;h.Age+=dt;
                if(h.Type==HazardType.Ink&&h.Age>=h.Life){h.Type=HazardType.None;h.Mark.Hide();continue;}
                if(h.Type==HazardType.Wave){
                    float before=Mathf.Clamp((oldAge-h.Delay)*h.Speed,0,h.Radius),radius=Mathf.Clamp((h.Age-h.Delay)*h.Speed,0,h.Radius);
                    if(h.Age<h.Delay){CircleMark(h.Mark,h.Center,.2f,ColorFor(BossWarning.Area),.22f);continue;}
                    CircleMark(h.Mark,h.Center,Mathf.Max(.2f,radius),ColorFor(BossWarning.Area),.43f);
                    if(!h.Hit){
                        if(BossWaveGeometry.SweptRingHit(tracked?previousPlayer:position,position,h.Center,before,radius,.26f)){h.Hit=true;boss.Hit(h.Center,h.Damage,6);}
                    }
                }else if(h.Type==HazardType.Ink){
                    if(h.Age<.32f){
                        float t=h.Age/.32f;h.Mark.Droplet.gameObject.SetActive(true);
                        h.Mark.Droplet.position=Vector3.Lerp(h.Origin,h.Center+Vector3.up*.2f,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*1.2f;
                        CircleMark(h.Mark,h.Center,h.Radius,ColorFor(BossWarning.Special));continue;
                    }
                    h.Mark.Droplet.gameObject.SetActive(false);
                    float appear=Mathf.Clamp01((h.Age-.32f)/.25f),fade=Mathf.Clamp01((h.Life-h.Age)/.6f);h.Mark.Fill.enabled=true;
                    h.Mark.Fill.transform.position=h.Center+Vector3.up*.075f;h.Mark.Fill.transform.localScale=Vector3.one*h.Radius*Mathf.Lerp(.85f,1,appear);
                    var color=new Color(.09f,.10f,.30f,.76f*fade);h.Mark.Block.SetColor(BaseColor,color);h.Mark.Block.SetColor(ColorId,color);h.Mark.Fill.SetPropertyBlock(h.Mark.Block);
                    CircleMark(h.Mark,h.Center,h.Radius,new Color(.23f,.35f,.84f,fade));
                    if(BossEncounter.InCircle(position,h.Center,h.Radius))player.SetBossSlow(h.Slow);
                    if(!h.Shown){h.Shown=true;HitEffects.Burst(h.Center+Vector3.up*.2f,new Color(.23f,.28f,.7f),5,0);}
                }else{
                    CircleMark(h.Mark,h.Center,h.Radius,Color.Lerp(ColorFor(BossWarning.Area),Color.white,Mathf.Clamp01(h.Age/h.Delay)*.3f));
                    if(h.Age>=h.Delay){
                        // The warning is already visible from creation. Keep its contour during the actual hit.
                        TentacleVisual(h,(h.Age-h.Delay)/.55f);
                        if(!h.Hit){h.Hit=true;boss.CircleHit(h.Center,h.Radius,h.Damage,5);HitEffects.Burst(h.Center,Color.cyan,8,0);}
                    }
                }
                if(h.Age>=h.Life){h.Type=HazardType.None;h.Mark.Hide();}
                if(!boss.IsActive||GameManager.Instance==null||!GameManager.Instance.IsPlaying)break;
            }
            previousPlayer=position;tracked=true;
        }
        public void Clear(){HideWarnings();foreach(var h in hazards)if(h!=null){h.Type=HazardType.None;h.Mark.Hide();}boss?.Player?.ResetBossSlow();tracked=false;}
        void OnDisable(){Clear();}
        void OnDestroy(){Clear();if(material!=null)Destroy(material);if(limbMaterial!=null)Destroy(limbMaterial);if(inkMaterial!=null)Destroy(inkMaterial);if(disc!=null)Destroy(disc);}
    }
}
