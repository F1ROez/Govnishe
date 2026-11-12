using System.Collections.Generic;
using UnityEngine;
namespace Brainrot
{
    /// <summary>Pooled burst particles; no transient per-hit GameObjects or lights.</summary>
    public sealed class HitEffects : MonoBehaviour
    {
        static readonly Queue<HitEffects> pool=new Queue<HitEffects>();
        static Material particleMaterial;
        ParticleSystem particles;
        float recycleAt;
        bool live;
        public static void Burst(Vector3 position,Color color,int count=8)
        {
            HitEffects effect=null;
            while(pool.Count>0 && effect==null)effect=pool.Dequeue();
            if(effect==null)
            {
                var go=new GameObject("Pooled hit sparks");
                effect=go.AddComponent<HitEffects>();
                effect.particles=go.AddComponent<ParticleSystem>();
                effect.particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=effect.particles.main;main.playOnAwake=false;main.loop=false;main.duration=.7f;
                main.startLifetime=new ParticleSystem.MinMaxCurve(.20f,.55f);
                main.startSpeed=new ParticleSystem.MinMaxCurve(1.4f,4f);
                main.startSize=new ParticleSystem.MinMaxCurve(.06f,.15f);
                main.gravityModifier=1.8f;main.maxParticles=80;main.simulationSpace=ParticleSystemSimulationSpace.World;
                var emission=effect.particles.emission;emission.enabled=false;
                var shape=effect.particles.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.12f;
                var size=effect.particles.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
                var renderer=effect.particles.GetComponent<ParticleSystemRenderer>();
                if(particleMaterial==null)
                {
                    var shader=Shader.Find("Universal Render Pipeline/Particles/Unlit")??Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Sprites/Default");
                    particleMaterial=new Material(shader);particleMaterial.color=Color.white;
                }
                renderer.sharedMaterial=particleMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                effect.particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            effect.transform.position=position;effect.gameObject.SetActive(true);
            var options=new ParticleSystem.EmitParams{startColor=color};
            effect.particles.Emit(options,Mathf.Clamp(count,1,80));effect.recycleAt=Time.time+.8f;effect.live=true;
        }
        void Update()
        {
            if(!live || Time.time<recycleAt)return;
            live=false;particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);gameObject.SetActive(false);pool.Enqueue(this);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetPool(){pool.Clear();particleMaterial=null;}
    }
}
