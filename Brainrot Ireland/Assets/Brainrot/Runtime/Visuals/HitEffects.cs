using System.Collections.Generic;
using UnityEngine;
namespace Brainrot
{
    /// <summary>Pooled burst particles; no transient per-hit GameObjects or lights.</summary>
    public sealed class HitEffects : MonoBehaviour
    {
        static readonly Queue<HitEffects> pool=new Queue<HitEffects>();
        static readonly List<HitEffects> effects=new List<HitEffects>();
        public static int AllocatedCount=>effects.Count;
        bool showRing;
        static Material particleMaterial,ringMaterial;
        static Texture2D sparkle;
        LineRenderer ring;
        MaterialPropertyBlock ringBlock;
        Color ringColor;float started,ringRadius=.8f;
        static readonly int BaseColor=Shader.PropertyToID("_BaseColor");
        ParticleSystem particles;
        float recycleAt;
        bool live;
        public static void Burst(Vector3 position,Color color,int count=8,float groundRingRadius=.8f)
        {
            HitEffects effect=null;
            while(pool.Count>0 && effect==null)effect=pool.Dequeue();
            int budget=Mathf.Clamp(GameBalance.Current.Feedback.EffectsBudget,4,48);
            if(effect==null&&effects.Count>=budget){
                // Reuse the oldest active effect rather than allocating a burst for every enemy in a crowd.
                float oldest=float.PositiveInfinity;
                for(int i=effects.Count-1;i>=0;i--){var candidate=effects[i];if(candidate==null){effects.RemoveAt(i);continue;}if(candidate.started<oldest){oldest=candidate.started;effect=candidate;}}
                if(effect!=null)effect.particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if(effect==null)
            {
                var go=new GameObject("Pooled hit sparks");
                effect=go.AddComponent<HitEffects>();effects.Add(effect);
                effect.particles=go.AddComponent<ParticleSystem>();
                effect.particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=effect.particles.main;main.playOnAwake=false;main.loop=false;main.duration=.7f;
                main.startLifetime=new ParticleSystem.MinMaxCurve(.20f,.55f);
                main.startSpeed=new ParticleSystem.MinMaxCurve(1.4f,4f);
                main.startSize=new ParticleSystem.MinMaxCurve(.10f,.23f);
                main.startRotation=new ParticleSystem.MinMaxCurve(-Mathf.PI,Mathf.PI);
                main.gravityModifier=1.8f;main.maxParticles=80;main.simulationSpace=ParticleSystemSimulationSpace.World;
                var emission=effect.particles.emission;emission.enabled=false;
                var shape=effect.particles.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.12f;
                var size=effect.particles.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
                var renderer=effect.particles.GetComponent<ParticleSystemRenderer>();
                if(particleMaterial==null)
                {
                    var shader=Shader.Find("Universal Render Pipeline/Particles/Unlit")??Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Sprites/Default");
                    particleMaterial=new Material(shader);particleMaterial.color=Color.white;
                    particleMaterial.SetFloat("_Surface",1);particleMaterial.SetFloat("_Blend",0);particleMaterial.SetFloat("_ZWrite",0);
                    particleMaterial.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);particleMaterial.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    particleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");particleMaterial.SetOverrideTag("RenderType","Transparent");particleMaterial.renderQueue=3000;
                    sparkle=SparkleTexture();particleMaterial.SetTexture("_BaseMap",sparkle);if(particleMaterial.HasProperty("_MainTex"))particleMaterial.SetTexture("_MainTex",sparkle);
                }
                renderer.sharedMaterial=particleMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                effect.particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                effect.ring=new GameObject("Мягкое кольцо попадания").AddComponent<LineRenderer>();effect.ring.transform.SetParent(go.transform,false);
                effect.ring.useWorldSpace=false;effect.ring.loop=true;effect.ring.positionCount=36;effect.ring.widthMultiplier=.045f;
                for(int i=0;i<36;i++){float angle=i*Mathf.PI*2/36;effect.ring.SetPosition(i,new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)));}
                if(ringMaterial==null){ringMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));ringMaterial.SetFloat("_Surface",1);ringMaterial.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);ringMaterial.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);ringMaterial.SetFloat("_ZWrite",0);ringMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");ringMaterial.renderQueue=3000;}
                effect.ring.sharedMaterial=ringMaterial;effect.ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;effect.ring.receiveShadows=false;effect.ringBlock=new MaterialPropertyBlock();
            }
            effect.transform.position=position;effect.gameObject.SetActive(true);
            var options=new ParticleSystem.EmitParams{startColor=color};
            effect.particles.Emit(options,Mathf.Clamp(count,1,80));effect.recycleAt=Time.time+.8f;effect.started=Time.time;effect.ringColor=color;effect.ringRadius=Mathf.Clamp(groundRingRadius,.1f,12);effect.showRing=groundRingRadius>0;effect.ring.enabled=effect.showRing;effect.live=true;
            effect.ring.transform.position=new Vector3(position.x,.07f,position.z);effect.TickRing();
        }
        static Texture2D SparkleTexture(){
            const int n=64;var texture=new Texture2D(n,n,TextureFormat.RGBA32,false){name="Оригинальная мультяшная искра",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[n*n];for(int y=0;y<n;y++)for(int x=0;x<n;x++){
                float u=(x+.5f)/n*2-1,v=(y+.5f)/n*2-1;float diamond=Mathf.Sqrt(Mathf.Abs(u))+Mathf.Sqrt(Mathf.Abs(v));
                float alpha=Mathf.Clamp01((1.12f-diamond)*8);float halo=Mathf.Exp(-(u*u+v*v)*14)*.23f;pixels[y*n+x]=new Color(1,1,1,Mathf.Max(alpha,halo));
            }texture.SetPixels(pixels);texture.Apply(false,true);return texture;
        }
        void TickRing(){float t=(Time.time-started)/.3f;ring.enabled=showRing&&t<1;if(!showRing||t>=1)return;ring.transform.localScale=Vector3.one*Mathf.Lerp(.16f,ringRadius,Mathf.Clamp01(t));var color=ringColor;color.a=(1-t)*.48f;ringBlock.SetColor(BaseColor,color);ring.SetPropertyBlock(ringBlock);}
        void Update()
        {
            if(!live)return;TickRing();
            if(Time.time<recycleAt)return;
            live=false;particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);gameObject.SetActive(false);pool.Enqueue(this);
        }
        public static void HeavyImpact(Vector3 position,Vector3 forward)
        {
            // One modest shock ring for a confirmed heavy contact, independent of the number of victims.
            Burst(position+Vector3.up*.4f,new Color(1,.97f,.81f),18,1.9f);
        }
        public static void ClearAll()
        {
            pool.Clear();BatSwingArc.ClearAll();
            for(int i=effects.Count-1;i>=0;i--){var effect=effects[i];if(effect==null){effects.RemoveAt(i);continue;}
                effect.live=false;effect.particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);effect.ring.enabled=false;effect.gameObject.SetActive(false);pool.Enqueue(effect);
            }
        }
        void OnDestroy(){
            effects.Remove(this);
            if(effects.Count!=0)return;
            pool.Clear();
            if(particleMaterial!=null)Destroy(particleMaterial);if(ringMaterial!=null)Destroy(ringMaterial);if(sparkle!=null)Destroy(sparkle);
            particleMaterial=ringMaterial=null;sparkle=null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetPool(){pool.Clear();effects.Clear();particleMaterial=null;ringMaterial=null;sparkle=null;}
    }
}
