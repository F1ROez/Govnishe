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
        bool showRing,heavy;
        static Material particleMaterial,ringMaterial;
        static Texture2D sparkle;
        LineRenderer ring,echoRing;
        MaterialPropertyBlock ringBlock,echoBlock;
        Color ringColor;float started,ringRadius=.8f;
        static readonly int BaseColor=Shader.PropertyToID("_BaseColor");
        ParticleSystem particles;
        float recycleAt;
        bool live;
        public static void Burst(Vector3 position,Color color,int count=8,float groundRingRadius=.8f)
        {
            Spawn(position,color,count,groundRingRadius,false,Vector3.forward);
        }
        static void Spawn(Vector3 position,Color color,int count,float groundRingRadius,bool heavy,Vector3 direction)
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
                main.startLifetime=new ParticleSystem.MinMaxCurve(.18f,.42f);
                main.startSpeed=new ParticleSystem.MinMaxCurve(1.4f,4f);
                main.startSize=new ParticleSystem.MinMaxCurve(.10f,.23f);
                main.startRotation=new ParticleSystem.MinMaxCurve(-Mathf.PI,Mathf.PI);
                main.gravityModifier=.85f;main.maxParticles=80;main.simulationSpace=ParticleSystemSimulationSpace.World;
                var emission=effect.particles.emission;emission.enabled=false;
                var shape=effect.particles.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.12f;
                var size=effect.particles.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
                var fading=effect.particles.colorOverLifetime;fading.enabled=true;
                var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.06f),new GradientAlphaKey(.75f,.38f),new GradientAlphaKey(0,1)});fading.color=gradient;
                var renderer=effect.particles.GetComponent<ParticleSystemRenderer>();
                if(particleMaterial==null)
                {
                    var shader=Shader.Find("Universal Render Pipeline/Particles/Unlit")??Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Sprites/Default");
                    particleMaterial=new Material(shader);particleMaterial.color=Color.white;
                    particleMaterial.SetFloat("_Surface",1);particleMaterial.SetFloat("_Blend",0);particleMaterial.SetFloat("_ZWrite",0);
                    particleMaterial.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);particleMaterial.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    particleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");particleMaterial.SetOverrideTag("RenderType","Transparent");particleMaterial.renderQueue=3000;
                    sparkle=SparkleTexture();if(particleMaterial.HasProperty("_BaseMap"))particleMaterial.SetTexture("_BaseMap",sparkle);if(particleMaterial.HasProperty("_MainTex"))particleMaterial.SetTexture("_MainTex",sparkle);
                }
                renderer.sharedMaterial=particleMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                effect.particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                effect.ring=new GameObject("Мягкое кольцо попадания").AddComponent<LineRenderer>();effect.ring.transform.SetParent(go.transform,false);
                effect.ring.useWorldSpace=false;effect.ring.loop=true;effect.ring.positionCount=36;effect.ring.widthMultiplier=.045f;
                for(int i=0;i<36;i++){float angle=i*Mathf.PI*2/36;effect.ring.SetPosition(i,new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)));}
                if(ringMaterial==null){ringMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));ringMaterial.SetFloat("_Surface",1);ringMaterial.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);ringMaterial.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);ringMaterial.SetFloat("_ZWrite",0);ringMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");ringMaterial.SetOverrideTag("RenderType","Transparent");ringMaterial.renderQueue=3000;}
                effect.ring.sharedMaterial=ringMaterial;effect.ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;effect.ring.receiveShadows=false;effect.ringBlock=new MaterialPropertyBlock();
                effect.echoRing=new GameObject("Эхо сильного удара").AddComponent<LineRenderer>();effect.echoRing.transform.SetParent(go.transform,false);
                effect.echoRing.useWorldSpace=false;effect.echoRing.loop=true;effect.echoRing.positionCount=36;
                effect.echoRing.sharedMaterial=ringMaterial;effect.echoRing.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;effect.echoRing.receiveShadows=false;effect.echoRing.enabled=false;effect.echoBlock=new MaterialPropertyBlock();
                for(int i=0;i<36;i++){float angle=i*Mathf.PI*2/36;effect.echoRing.SetPosition(i,new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)));}
            }
            effect.transform.position=position;effect.gameObject.SetActive(true);
            effect.heavy=heavy;
            direction.y=0;if(direction.sqrMagnitude<.001f)direction=Vector3.forward;direction.Normalize();
            var side=Vector3.Cross(Vector3.up,direction);
            int amount=Mathf.Clamp(count,1,heavy?24:40);if(SaveSystem.Data.Quality==0)amount=Mathf.Max(2,amount/2);
            // Local deterministic fan: effects do not consume the gameplay random sequence.
            for(int i=0;i<amount;i++){
                float phase=(i+.5f)*2.399963f,spread=heavy?2.7f:1.4f;
                var velocity=side*(Mathf.Sin(phase)*spread)+direction*(Mathf.Cos(phase)*spread+(heavy?1.7f:0))+Vector3.up*(.8f+(i%5)*.36f);
                var tint=i%4==0?Color.Lerp(color,Color.white,.85f):color;
                var options=new ParticleSystem.EmitParams{startColor=tint,velocity=velocity,startSize=heavy?.10f+(i%3)*.035f:.075f+(i%3)*.025f,startLifetime=heavy?.28f+(i%4)*.055f:.17f+(i%4)*.04f};
                effect.particles.Emit(options,1);
            }
            effect.recycleAt=Time.time+.65f;effect.started=Time.time;effect.ringColor=color;effect.ringRadius=Mathf.Clamp(groundRingRadius,.1f,12);effect.showRing=groundRingRadius>0;effect.ring.enabled=effect.showRing;effect.live=true;
            effect.ring.transform.position=new Vector3(position.x,.07f,position.z);effect.echoRing.transform.position=new Vector3(position.x,.085f,position.z);effect.TickRing();
        }
        static Texture2D SparkleTexture(){
            const int n=64;var texture=new Texture2D(n,n,TextureFormat.RGBA32,false){name="Оригинальная мультяшная искра",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[n*n];for(int y=0;y<n;y++)for(int x=0;x<n;x++){
                float u=(x+.5f)/n*2-1,v=(y+.5f)/n*2-1;float diamond=Mathf.Sqrt(Mathf.Abs(u))+Mathf.Sqrt(Mathf.Abs(v));
                float alpha=Mathf.Clamp01((1.12f-diamond)*8);float halo=Mathf.Exp(-(u*u+v*v)*14)*.23f;pixels[y*n+x]=new Color(1,1,1,Mathf.Max(alpha,halo));
            }texture.SetPixels(pixels);texture.Apply(false,true);return texture;
        }
        void TickRing(){
            float age=Time.time-started,duration=heavy?.42f:.24f,t=Mathf.Clamp01(age/duration);
            ring.enabled=showRing&&t<1;
            if(ring.enabled){
                float ease=1-(1-t)*(1-t)*(1-t);
                ring.transform.localScale=Vector3.one*Mathf.Lerp(.12f,ringRadius,ease);
                ring.widthMultiplier=Mathf.Lerp(heavy?.105f:.04f,.008f,t);
                var color=ringColor;color.a=(1-t)*(1-t)*(heavy?.65f:.35f);ringBlock.SetColor(BaseColor,color);ring.SetPropertyBlock(ringBlock);
            }
            float echo=Mathf.Clamp01((age-.065f)/.37f);echoRing.enabled=showRing&&heavy&&SaveSystem.Data.Quality>0&&age>.065f&&echo<1;
            if(echoRing.enabled){
                echoRing.transform.localScale=Vector3.one*Mathf.Lerp(.25f,ringRadius*.86f,1-(1-echo)*(1-echo));echoRing.widthMultiplier=Mathf.Lerp(.045f,.006f,echo);
                echoBlock.SetColor(BaseColor,new Color(.55f,.95f,1,(1-echo)*(1-echo)*.35f));echoRing.SetPropertyBlock(echoBlock);
            }
        }
        void Update()
        {
            if(!live)return;TickRing();
            if(Time.time<recycleAt)return;
            live=false;particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);gameObject.SetActive(false);pool.Enqueue(this);
        }
        public static void HeavyImpact(Vector3 position,Vector3 forward)
        {
            // One directional spark fan and two fine rings for the whole confirmed swing, not each victim.
            Spawn(position+Vector3.up*.4f,new Color(1,.90f,.63f),20,2.1f,true,forward);
        }
        public static void ClearAll()
        {
            pool.Clear();BatSwingArc.ClearAll();
            for(int i=effects.Count-1;i>=0;i--){var effect=effects[i];if(effect==null){effects.RemoveAt(i);continue;}
                effect.live=false;effect.particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);effect.ring.enabled=false;effect.echoRing.enabled=false;effect.gameObject.SetActive(false);pool.Enqueue(effect);
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
