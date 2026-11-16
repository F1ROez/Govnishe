using System;
using UnityEngine;
namespace Brainrot
{
    [Serializable] public sealed class CombatFeelTuning
    {
        [Range(0,10),Tooltip("Плавное сужение угла обзора при полном замахе, градусы.")]public float ChargeFovReduction=5;
        [Range(.1f,1),Tooltip("Время плавного приближения при заряде, с.")]public float ChargeInSeconds=.45f;
        [Range(.06f,.4f),Tooltip("Быстрый мягкий возврат камеры после отпускания, с.")]public float ReleaseSeconds=.14f;
        [Range(0,5),Tooltip("Короткое расширение FOV от подтверждённого сильного попадания.")]public float ImpactFovKick=1.8f;
        [Range(0,.3f),Tooltip("Сила направленной встряски при сильном попадании, м.")]public float HitShake=.12f;
        [Range(.05f,.35f),Tooltip("Длительность затухающей отдачи камеры, с.")]public float HitShakeSeconds=.16f;
        [Range(0,.08f),Tooltip("Короткая задержка только визуальной позы биты при сильном попадании; игровое время не останавливается.")]public float HitPosePauseSeconds=.045f;
        [Range(4,48),Tooltip("Максимум одновременно существующих эффектов попадания; старые переиспользуются.")]public int EffectsBudget=24;
    }

    /// <summary>Deterministic, frame-rate independent charge/release/impact envelope. Never changes global game time.</summary>
    public sealed class CombatCameraFeedback
    {
        public float Charge {get;private set;}
        public float Impact {get;private set;}
        Vector3 direction;
        float impactAge,impactDuration=.16f,impactStrength;
        public void Reset(){Charge=Impact=impactStrength=impactAge=0;direction=Vector3.zero;}
        public void ConfirmHit(Vector3 forward,bool heavy,CombatFeelTuning tuning)
        {
            forward.y=0;direction=forward.sqrMagnitude>.0001f?forward.normalized:Vector3.forward;
            impactAge=0;impactDuration=Mathf.Max(.05f,tuning.HitShakeSeconds);
            // One envelope per swing. Multiple victims never add amplitude or extend global time.
            impactStrength=heavy?1:.25f;Impact=impactStrength;
        }
        public void Tick(float chargeTarget,float deltaTime,CombatFeelTuning tuning)
        {
            float dt=Mathf.Max(0,deltaTime),target=Mathf.Clamp01(chargeTarget);
            float seconds=target>Charge?tuning.ChargeInSeconds:tuning.ReleaseSeconds;
            Charge=Mathf.Lerp(Charge,target,1-Mathf.Exp(-4*dt/Mathf.Max(.04f,seconds)));
            if(target<=0&&Charge<.0001f)Charge=0;
            impactAge+=dt;float t=Mathf.Clamp01(impactAge/impactDuration);
            Impact=impactStrength*(1-t)*(1-t);
        }
        public float FieldOfView(float baseline,CombatFeelTuning tuning,float strength)
            =>Mathf.Clamp(baseline-Charge*Mathf.Clamp(tuning.ChargeFovReduction,0,10)+Impact*Mathf.Max(0,tuning.ImpactFovKick)*Mathf.Clamp01(strength),35,90);
        public Vector3 Offset(CombatFeelTuning tuning,float strength)
        {
            float amplitude=Impact*Mathf.Clamp(tuning.HitShake,0,.3f)*Mathf.Clamp01(strength);
            float t=impactAge/Mathf.Max(.05f,impactDuration);
            Vector3 side=Vector3.Cross(Vector3.up,direction);
            return (-direction*Mathf.Cos(t*Mathf.PI*3)+Vector3.up*Mathf.Sin(t*Mathf.PI*4)*.35f+side*Mathf.Sin(t*Mathf.PI*5)*.22f)*amplitude;
        }
    }
}
