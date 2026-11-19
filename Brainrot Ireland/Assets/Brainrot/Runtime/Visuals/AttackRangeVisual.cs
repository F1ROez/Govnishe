using UnityEngine;
using UnityEngine.Rendering;

namespace Brainrot
{
    /// <summary>Ground outline matches the melee cone. Rebuilds only when its dimensions change.</summary>
    public sealed class AttackRangeVisual : MonoBehaviour
    {
        const int Segments=40;
        static Material sharedMaterial;
        static readonly int BaseColorId=Shader.PropertyToID("_BaseColor");
        static readonly int ColorId=Shader.PropertyToID("_Color");
        PlayerController owner;
        LineRenderer arc, nearCircle;
        MaterialPropertyBlock block;
        float cachedRange=-1,cachedArc=-1,cachedInner=-1,pulseRemaining;
        bool pulseHit;
        static readonly Color Idle=new Color(.75f,.93f,.92f,.30f);
        static readonly Color Swing=new Color(1,.83f,.38f,.75f);
        static readonly Color Hit=new Color(.46f,1,.70f,.94f);

        public void Initialize(PlayerController player)
        {
            owner=player;
            if(arc!=null)return;
            transform.SetParent(player.transform,false);
            block=new MaterialPropertyBlock();
            arc=CreateLine("Bat reach",false);
            nearCircle=CreateLine("Close reach",true);
            Rebuild();
            ApplyColor(Idle);
        }
        LineRenderer CreateLine(string name,bool loop)
        {
            if(sharedMaterial==null)
            {
                var shader=Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Sprites/Default");
                sharedMaterial=new Material(shader){name="Shared attack outline",renderQueue=3000};
                sharedMaterial.SetFloat("_Surface",1);sharedMaterial.SetFloat("_Blend",0);
                sharedMaterial.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);
                sharedMaterial.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
                sharedMaterial.SetInt("_ZWrite",0);
                sharedMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                sharedMaterial.SetOverrideTag("RenderType","Transparent");
                sharedMaterial.color=Color.white;
            }
            var go=new GameObject(name);go.transform.SetParent(transform,false);
            var line=go.AddComponent<LineRenderer>();line.sharedMaterial=sharedMaterial;
            line.useWorldSpace=false;line.loop=loop;line.widthMultiplier=.04f;
            line.startColor=line.endColor=Color.white;line.numCornerVertices=2;line.numCapVertices=2;
            line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
            line.alignment=LineAlignment.View;
            return line;
        }
        public void Pulse(bool hit){pulseHit=hit;pulseRemaining=.20f;}
        void LateUpdate()
        {
            if(owner==null||arc==null)return;
            var game=GameManager.Instance;
            bool visible=game!=null&&game.IsPlaying&&GameBalance.Current.Combat.ShowAttackRange&&SaveSystem.Data.ShowAttackRange;
            arc.enabled=visible;nearCircle.enabled=visible&&GameBalance.Current.Combat.InnerOmniRadius>0;
            if(!visible)return;
            // The arena is a flat surface at y approximately zero; avoid z-fighting and leg height.
            transform.localPosition=Vector3.up*(.035f-owner.transform.position.y);
            Rebuild();
            pulseRemaining=Mathf.Max(0,pulseRemaining-Time.deltaTime);
            float t=pulseRemaining/.20f;
            ApplyColor(Color.Lerp(Idle,pulseHit?Hit:Swing,t));
            arc.widthMultiplier=Mathf.Lerp(.04f,.075f,t);
        }
        void Rebuild()
        {
            if(owner==null)return;
            float radius=Mathf.Max(.1f,owner.Stats.Range);
            float degrees=Mathf.Clamp(GameBalance.Current.Combat.ArcDegrees,1,360);
            float inner=Mathf.Clamp(GameBalance.Current.Combat.InnerOmniRadius,0,radius);
            if(Mathf.Approximately(radius,cachedRange)&&Mathf.Approximately(degrees,cachedArc)&&Mathf.Approximately(inner,cachedInner))return;
            cachedRange=radius;cachedArc=degrees;cachedInner=inner;
            arc.positionCount=Segments+3;
            arc.SetPosition(0,Vector3.zero);
            for(int i=0;i<=Segments;i++)
            {
                float a=Mathf.Lerp(-degrees*.5f,degrees*.5f,i/(float)Segments)*Mathf.Deg2Rad;
                arc.SetPosition(i+1,new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*radius);
            }
            arc.SetPosition(Segments+2,Vector3.zero);
            nearCircle.positionCount=Segments;
            for(int i=0;i<Segments;i++)
            {
                float a=i*Mathf.PI*2/Segments;
                nearCircle.SetPosition(i,new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*inner);
            }
        }
        void ApplyColor(Color color)
        {
            if(block==null)return;
            block.Clear();block.SetColor(BaseColorId,color);block.SetColor(ColorId,color);
            arc.SetPropertyBlock(block);
            color.a*=.55f;block.SetColor(BaseColorId,color);block.SetColor(ColorId,color);
            nearCircle.SetPropertyBlock(block);
        }
        void OnDisable(){pulseRemaining=0;}
    }
}
