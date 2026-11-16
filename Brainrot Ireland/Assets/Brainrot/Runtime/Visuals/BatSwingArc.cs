using System.Collections.Generic;
using UnityEngine;
namespace Brainrot
{
    /// <summary>Short, original white crescent that follows the bat sector. Eight reusable renderers bound crowded effects.</summary>
    public sealed class BatSwingArc:MonoBehaviour
    {
        static readonly List<BatSwingArc> instances=new List<BatSwingArc>(8);
        static Material material;
        LineRenderer line;MaterialPropertyBlock block;
        float started,lifetime,reach,width,arc;bool live;
        static readonly int BaseColor=Shader.PropertyToID("_BaseColor"),ColorId=Shader.PropertyToID("_Color");
        public static void Play(Vector3 origin,Vector3 forward,float range,bool heavy)
        {
            BatSwingArc effect=null;float oldest=float.PositiveInfinity;
            for(int i=instances.Count-1;i>=0;i--){var item=instances[i];if(item==null){instances.RemoveAt(i);continue;}if(!item.live){effect=item;break;}if(item.started<oldest){oldest=item.started;effect=item;}}
            if(effect==null||effect.live&&instances.Count<8){
                var go=new GameObject("Белый полумесяц биты");effect=go.AddComponent<BatSwingArc>();instances.Add(effect);effect.line=go.AddComponent<LineRenderer>();effect.block=new MaterialPropertyBlock();
                effect.line.useWorldSpace=false;effect.line.positionCount=32;effect.line.numCapVertices=3;effect.line.numCornerVertices=3;
                effect.line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;effect.line.receiveShadows=false;
                effect.line.widthCurve=new AnimationCurve(new Keyframe(0,0),new Keyframe(.18f,1),new Keyframe(.7f,.85f),new Keyframe(1,0));
                if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Sprites/Default")){name="Белый взмах",renderQueue=3000};
                    if(material.HasProperty("_Surface"))material.SetFloat("_Surface",1);material.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);material.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);material.SetInt("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.SetOverrideTag("RenderType","Transparent");}
                effect.line.sharedMaterial=material;
            }
            forward.y=0;if(forward.sqrMagnitude<.001f)forward=Vector3.forward;
            effect.transform.SetPositionAndRotation(origin,Quaternion.LookRotation(forward.normalized));effect.reach=Mathf.Clamp(range*.76f,.5f,10);effect.width=heavy?.12f:.055f;
            effect.arc=Mathf.Clamp(GameBalance.Current.Combat.ArcDegrees,20,180);effect.lifetime=heavy?.16f:.11f;effect.started=Time.time;effect.live=true;effect.gameObject.SetActive(true);effect.Tick();
        }
        void Tick()
        {
            float t=Mathf.Clamp01((Time.time-started)/lifetime),fade=1-t;
            float radius=reach*Mathf.Lerp(.9f,1.08f,t);
            for(int i=0;i<32;i++){float u=i/31f,angle=Mathf.Lerp(-arc*.5f,arc*.5f,u)*Mathf.Deg2Rad;line.SetPosition(i,new Vector3(Mathf.Sin(angle)*radius,(u-.5f)*.12f,Mathf.Cos(angle)*radius));}
            line.widthMultiplier=width*(.6f+fade*.4f);var color=new Color(1,1,1,fade*.82f);block.SetColor(BaseColor,color);block.SetColor(ColorId,color);line.SetPropertyBlock(block);
            if(t>=1){live=false;gameObject.SetActive(false);}
        }
        void Update(){if(live)Tick();}
        void OnDestroy(){instances.Remove(this);if(instances.Count==0&&material!=null){Destroy(material);material=null;}}
        public static void ClearAll(){for(int i=instances.Count-1;i>=0;i--){var item=instances[i];if(item==null){instances.RemoveAt(i);continue;}item.live=false;item.gameObject.SetActive(false);}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetPool(){instances.Clear();material=null;}
    }
}
