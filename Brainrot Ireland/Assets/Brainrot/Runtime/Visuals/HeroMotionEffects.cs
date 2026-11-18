using UnityEngine;
using UnityEngine.Rendering;
namespace Brainrot
{
    /// <summary>Small scene-owned sand footsteps and a brief dash ribbon. Never changes actor movement.</summary>
    public sealed class HeroMotionEffects:MonoBehaviour
    {
        PlayerController player;ParticleSystem dust;TrailRenderer ribbon;Material dustMaterial,trailMaterial;Texture2D soft;
        Vector3 previous;float stepClock;bool left,wasVisible;
        public void Initialize(PlayerController value){
            player=value;
            var go=new GameObject("Мягкий песок под ногами",typeof(ParticleSystem));go.transform.SetParent(transform,false);dust=go.GetComponent<ParticleSystem>();dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=dust.main;main.playOnAwake=false;main.loop=false;main.duration=.5f;main.startLifetime=.28f;main.startSize=.12f;main.startSpeed=0;main.maxParticles=64;main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=-.025f;
            var emission=dust.emission;emission.enabled=false;var shape=dust.shape;shape.enabled=false;
            var size=dust.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.4f),new Keyframe(.5f,1),new Keyframe(1,1.35f)));
            var color=dust.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.65f,.08f),new GradientAlphaKey(0,1)});color.color=gradient;
            soft=new Texture2D(32,32,TextureFormat.RGBA32,false){name="Мягкий песочный след",wrapMode=TextureWrapMode.Clamp};var pixels=new Color32[1024];
            for(int y=0;y<32;y++)for(int x=0;x<32;x++){float u=(x-15.5f)/15.5f,v=(y-15.5f)/15.5f;float a=Mathf.Pow(Mathf.Clamp01(1-u*u-v*v),2);pixels[y*32+x]=new Color(1,1,1,a);}
            soft.SetPixels32(pixels);soft.Apply(false,true);
            dustMaterial=Transparent("Песочная дымка");dustMaterial.SetTexture("_BaseMap",soft);
            var renderer=dust.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=dustMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            var trail=new GameObject("Короткий след рывка").transform;trail.SetParent(player.transform,false);trail.localPosition=Vector3.up*.75f;
            ribbon=trail.gameObject.AddComponent<TrailRenderer>();trailMaterial=Transparent("Морской след рывка");ribbon.sharedMaterial=trailMaterial;ribbon.time=.15f;ribbon.minVertexDistance=.07f;ribbon.numCapVertices=2;
            ribbon.widthCurve=new AnimationCurve(new Keyframe(0,.16f),new Keyframe(1,0));ribbon.startColor=new Color(.6f,.95f,1,.52f);ribbon.endColor=new Color(.6f,.95f,1,0);ribbon.shadowCastingMode=ShadowCastingMode.Off;ribbon.receiveShadows=false;ribbon.emitting=false;ResetEffects();
        }
        static Material Transparent(string title){
            var material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")){name=title,renderQueue=3000};material.SetColor("_BaseColor",Color.white);material.SetFloat("_Surface",1);material.SetFloat("_ZWrite",0);material.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.SetOverrideTag("RenderType","Transparent");return material;
        }
        public void ResetEffects(){
            if(player!=null)previous=player.transform.position;stepClock=0;left=false;wasVisible=false;
            if(dust!=null)dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);if(ribbon!=null){ribbon.emitting=false;ribbon.Clear();}
        }
        void LateUpdate(){
            if(player==null||dust==null||ribbon==null)return;
            var game=GameManager.Instance;var feedback=GameBalance.Current.Feedback;
            bool visible=game!=null&&game.IsPlaying&&player.Health>0&&feedback.MovementEffects&&SaveSystem.Data.Quality>0;
            Vector3 delta=player.transform.position-previous;previous=player.transform.position;delta.y=0;
            if(!visible){ribbon.emitting=false;if(wasVisible&&(game==null||!game.HasRun))ResetEffects();wasVisible=false;return;}wasVisible=true;
            ribbon.emitting=player.IsDashing;
            if(Time.deltaTime<=0||delta.sqrMagnitude<.00001f||delta.sqrMagnitude>4||player.IsDashing){stepClock=0;return;}
            stepClock-=Time.deltaTime;if(stepClock>0)return;stepClock=.16f;
            float strength=Mathf.Clamp(feedback.FootstepDustScale,0,1.5f);if(strength<=0)return;
            left=!left;float scale=Mathf.Max(.1f,GameBalance.Current.PlayerModelScale);
            Vector3 position=player.transform.position+player.transform.right*((left?1:-1)*.18f*scale);position.y=.035f;
            var emit=new ParticleSystem.EmitParams{position=position,velocity=-delta.normalized*.25f+Vector3.up*.15f,startColor=new Color(.94f,.78f,.48f,.48f),startSize=.16f*strength,startLifetime=.32f};dust.Emit(emit,2);
        }
        void OnDestroy(){if(ribbon!=null)Destroy(ribbon.gameObject);if(dustMaterial!=null)Destroy(dustMaterial);if(trailMaterial!=null)Destroy(trailMaterial);if(soft!=null)Destroy(soft);}
    }
}
