using System.Collections.Generic;
using UnityEngine;
namespace Brainrot
{
    /// <summary>Short, original white crescent that follows the bat sector. Eight reusable renderers bound crowded effects.</summary>
    public sealed class BatSwingArc:MonoBehaviour
    {
        static readonly List<BatSwingArc> instances=new List<BatSwingArc>(8);
        static Material material;
        LineRenderer line,afterimage;MaterialPropertyBlock block,afterimageBlock;
        float started,lifetime,reach,width,arc;bool live,heavy;
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
                effect.afterimage=new GameObject("Бирюзовый след сильного взмаха").AddComponent<LineRenderer>();effect.afterimage.transform.SetParent(go.transform,false);effect.afterimageBlock=new MaterialPropertyBlock();
                effect.afterimage.useWorldSpace=false;effect.afterimage.positionCount=32;effect.afterimage.numCapVertices=2;
                effect.afterimage.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;effect.afterimage.receiveShadows=false;
                effect.afterimage.widthCurve=effect.line.widthCurve;effect.afterimage.sharedMaterial=material;
            }
            forward.y=0;if(forward.sqrMagnitude<.001f)forward=Vector3.forward;
            effect.transform.SetPositionAndRotation(origin,Quaternion.LookRotation(forward.normalized));effect.reach=Mathf.Clamp(range*.76f,.5f,10);effect.width=heavy?.16f:.055f;effect.heavy=heavy;
            effect.arc=Mathf.Clamp(GameBalance.Current.Combat.ArcDegrees,20,180);effect.lifetime=heavy?.21f:.13f;effect.started=Time.time;effect.live=true;effect.gameObject.SetActive(true);effect.Tick();
        }
        void Tick()
        {
            float t=Mathf.Clamp01((Time.time-started)/lifetime),fade=(1-t)*(1-t);
            float ease=1-(1-t)*(1-t),radius=reach*Mathf.Lerp(.87f,1.03f,ease);
            for(int i=0;i<32;i++){
                float u=i/31f,angle=Mathf.Lerp(-arc*.5f,arc*.5f,u)*Mathf.Deg2Rad;
                var point=new Vector3(Mathf.Sin(angle)*radius,(u-.5f)*.12f,Mathf.Cos(angle)*radius);line.SetPosition(i,point);
                afterimage.SetPosition(i,point*.92f+Vector3.down*.045f);
            }
            line.widthMultiplier=width*(.45f+fade*.55f);var color=new Color(1,1,.97f,Mathf.Sqrt(fade)*.90f);block.SetColor(BaseColor,color);block.SetColor(ColorId,color);line.SetPropertyBlock(block);
            afterimage.enabled=heavy&&SaveSystem.Data.Quality>0;
            if(afterimage.enabled){
                afterimage.widthMultiplier=width*.30f;var tint=new Color(.52f,.94f,1,fade*.58f);afterimageBlock.SetColor(BaseColor,tint);afterimageBlock.SetColor(ColorId,tint);afterimage.SetPropertyBlock(afterimageBlock);
            }
            if(t>=1){live=false;gameObject.SetActive(false);}
        }
        void Update(){if(live)Tick();}
        void OnDestroy(){instances.Remove(this);if(instances.Count==0&&material!=null){Destroy(material);material=null;}}
        public static void ClearAll(){for(int i=instances.Count-1;i>=0;i--){var item=instances[i];if(item==null){instances.RemoveAt(i);continue;}item.live=false;item.gameObject.SetActive(false);}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetPool(){instances.Clear();material=null;}
    }
}
