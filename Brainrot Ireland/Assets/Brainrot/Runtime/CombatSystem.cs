using System.Collections.Generic;
using UnityEngine;
namespace Brainrot {
 public sealed class CombatSystem:MonoBehaviour {
  struct PendingContact {public float Time,Power;public bool Heavy;}
  readonly List<PendingContact> contacts=new List<PendingContact>(16);
  PlayerController player;CharacterVisual visual;AttackRangeVisual rangeVisual;
  float nextAttack,nextHeavyAttack,chargeStarted,bufferUntil;bool charging,inputSuppressed,chargeReadyEffect,bufferedAttack,bufferedHeavy,holdBlocked;
  float cooldownNoticeUntil,nextBoomerang,throwReleaseAt;BatBoomerang boomerang;bool boomerangSuppressed=true,throwPending;
  const float ThrowDuration=.42f,ThrowReleaseDelay=.14f;
  public bool BoomerangActive=>throwPending||boomerang!=null&&boomerang.Flying;
  public float BoomerangCooldownRemaining=>Mathf.Max(0,nextBoomerang-Time.time);
  public float BoomerangCooldownDuration=>Mathf.Clamp(GameBalance.Current.Boomerang.CooldownSeconds,.3f,30);
  public float BoomerangRemaining=>throwPending?Mathf.Max(0,throwReleaseAt-Time.time)+Mathf.Clamp(GameBalance.Current.Boomerang.OutSeconds,.15f,1)+Mathf.Clamp(GameBalance.Current.Boomerang.ReturnSeconds,.2f,1.5f):boomerang!=null?boomerang.Remaining:0;
  public bool HeavyCooldownNotice=>Time.time<cooldownNoticeUntil;
  public bool IsCharging=>charging&&!holdBlocked&&HeavyReady&&Time.time-chargeStarted>=HoldDelay;
  float HoldDelay=>Mathf.Clamp(GameBalance.Current.Combat.HeavyHoldDelay,0,.35f);
  public float HeavyCooldownRemaining=>Mathf.Max(0,nextHeavyAttack-Time.time);
  public float HeavyCooldownDuration=>Mathf.Max(.1f,GameBalance.Current.Combat.StrongCooldownSeconds);
  public bool HeavyReady=>HeavyCooldownRemaining<=0&&!BoomerangActive;
  public float ChargeProgress=>IsCharging?Mathf.Clamp01((Time.time-chargeStarted-HoldDelay)/Mathf.Max(.2f,GameBalance.Current.Combat.HeavyChargeSeconds)):0;
  public bool IsAttacking=>visual!=null&&visual.AttackActive;
  public bool HeavyVisualActive{get;private set;}
  public void Initialize(PlayerController p,CharacterVisual v){player=p;visual=v;boomerang=gameObject.AddComponent<BatBoomerang>();boomerang.Initialize(p,v);}
  public void SetRangeVisual(AttackRangeVisual value){rangeVisual=value;}
  public void CancelPendingSwings(){throwPending=false;boomerangSuppressed=true;HeavyVisualActive=false;boomerang?.Cancel();contacts.Clear();bufferedAttack=false;bufferUntil=0;charging=false;holdBlocked=false;cooldownNoticeUntil=0;chargeReadyEffect=false;inputSuppressed=true;GameAudio.EndCharge();player?.ResetCombatCamera();if(visual!=null)visual.ResetAttack();}
  public void ResetRun(){nextAttack=nextHeavyAttack=nextBoomerang=0;CancelPendingSwings();}
  public void RestoreHeavyCooldown(float remaining){CancelPendingSwings();nextAttack=Time.time+.2f;nextHeavyAttack=Time.time+Mathf.Clamp(float.IsNaN(remaining)||float.IsInfinity(remaining)?0:remaining,0,HeavyCooldownDuration);}
  public void UpdateAttackInput(bool held,bool pressed=false,bool released=false){
   var game=GameManager.Instance;if(!isActiveAndEnabled||game==null||!game.IsPlaying||player==null||visual==null||player.IsCaptured||player.Health<=0)return;
   if(inputSuppressed){if(!held)inputSuppressed=false;return;}
   if(bufferedAttack){
    if(Time.time>bufferUntil)bufferedAttack=false;
    else if(Time.time>=nextAttack){bool requestedHeavy=bufferedHeavy;bufferedAttack=false;if(!requestedHeavy||HeavyReady)ExecuteAttack(requestedHeavy);}
   }
   float window=Mathf.Clamp(GameBalance.Current.Combat.InputBufferSeconds,0,.25f);
   if((held||pressed)&&!charging&&Time.time>=nextAttack-window){
    charging=true;chargeStarted=Time.time;chargeReadyEffect=false;holdBlocked=!HeavyReady;
   }
   if(charging&&Time.time-chargeStarted>=HoldDelay&&(holdBlocked||!HeavyReady)){holdBlocked=true;if(HeavyCooldownRemaining>0)cooldownNoticeUntil=Time.time+.8f;}
   // Input is sampled immediately; poses wait until the previous swing has recovered.
   if(held&&IsCharging&&Time.time>=nextAttack){if(HeavyReady&&(GameAudio.Instance==null||!GameAudio.Instance.Charging))GameAudio.BeginCharge();visual.SetChargePose(ChargeProgress,HeavyReady);GameAudio.SetCharge(ChargeProgress,HeavyReady);if(HeavyReady&&ChargeProgress>=1&&!chargeReadyEffect){chargeReadyEffect=true;visual.ChargeReady();}}
   if(charging&&(!held||released)){
    bool blocked=Time.time-chargeStarted>=HoldDelay&&(holdBlocked||!HeavyReady);
    bool heavy=ChargeProgress>=1&&HeavyReady;charging=false;GameAudio.EndCharge();
    if(blocked){if(HeavyCooldownRemaining>0)cooldownNoticeUntil=Time.time+.8f;if(!visual.AttackActive)visual.SetChargePose(0,false);return;}
    if(!ExecuteAttack(heavy)){
     if(Time.time<nextAttack&&nextAttack-Time.time<=window){bufferedAttack=true;bufferedHeavy=heavy;bufferUntil=Time.time+window;}
     if(!visual.AttackActive)visual.SetChargePose(0,false);
    }
   }
  }
  public void TryAttack(){ExecuteAttack(false);}
  public bool ExecuteAttack(bool heavy){
   var game=GameManager.Instance;if(!isActiveAndEnabled||game==null||!game.IsPlaying||player==null||visual==null||player.IsCaptured||player.Health<=0||Time.time<nextAttack)return false;
   if(BoomerangActive||heavy&&!HeavyReady)return false;
   // Direct ability/test calls use the same lifecycle as releasing LMB. Keep the visual charge until PlayAttack consumes it.
   charging=false;bufferedAttack=false;GameAudio.EndCharge();
   nextAttack=Time.time+Mathf.Max(.12f,player.Stats.AttackInterval);
   if(heavy)nextHeavyAttack=Time.time+HeavyCooldownDuration;
   HeavyVisualActive=heavy;
   BeginSwing(heavy?Mathf.Max(1,GameBalance.Current.Combat.StrongDamageMultiplier):1,heavy);return true;
  }
  public void UpdateBoomerangInput(bool held,bool pressed){
   if(boomerangSuppressed){if(!held)boomerangSuppressed=false;return;}
   if(pressed&&!charging)ExecuteBoomerang();
  }
  public bool ExecuteBoomerang(){
   var game=GameManager.Instance;
   if(!isActiveAndEnabled||game==null||!game.IsPlaying||player==null||visual==null||player.IsCaptured||player.Health<=0||charging||IsAttacking||Time.time<nextAttack||BoomerangActive||BoomerangCooldownRemaining>0)return false;
   throwPending=true;throwReleaseAt=Time.time+ThrowReleaseDelay;nextBoomerang=Time.time+BoomerangCooldownDuration;
   nextAttack=Time.time+ThrowDuration;HeavyVisualActive=false;visual.PlayThrow(ThrowDuration,ThrowReleaseDelay);player.NotifyAttack();return true;
  }

  void BeginSwing(float power,bool heavy=false){
   var tuning=GameBalance.Current.Combat;var weapon=player.Character!=null?player.Character.Weapon:null;
   float duration=Mathf.Min(weapon!=null&&weapon.UseCustomTiming?weapon.SwingDuration:tuning.SwingDuration,Mathf.Max(.15f,player.Stats.AttackInterval*.95f));
   float delay=Mathf.Clamp(weapon!=null&&weapon.UseCustomTiming?weapon.ImpactDelay:tuning.ImpactDelay,0,duration*.65f);
   visual.SetAttackReach(player.Stats.Range);visual.PlayAttack(duration,delay,heavy);player.NotifyAttack();GameAudio.Swing(heavy);
   if(contacts.Count<32)contacts.Add(new PendingContact {Time=Time.time+delay,Power=power,Heavy=heavy});
  }
  void Update(){
   var game=GameManager.Instance;if(game==null||!game.IsPlaying)return;
   if(throwPending&&Time.time>=throwReleaseAt){throwPending=false;if(boomerang.Launch())GameAudio.Swing(false);}
   for(int i=0;i<contacts.Count&&game.IsPlaying;){if(Time.time>=contacts[i].Time){float power=contacts[i].Power;bool heavy=contacts[i].Heavy;contacts.RemoveAt(i);Strike(power,heavy);}else i++;}
  }
  void OnDisable(){CancelPendingSwings();}
  /// <summary>Intersection of the displayed cone/near circle and the enemy's horizontal body disc.</summary>
  public static bool InMeleeSector(Vector3 origin,Vector3 forward,Vector3 target,float range,float arcDegrees,float innerRadius,float targetRadius=0){
   Vector3 delta=target-origin;delta.y=0;float distance=delta.magnitude;
   range=Mathf.Max(.1f,range);targetRadius=Mathf.Max(0,targetRadius);
   if(distance>range+targetRadius)return false;
   if(distance<=Mathf.Clamp(innerRadius,0,range)+targetRadius)return true;
   forward.y=0;if(forward.sqrMagnitude<.001f)return false;
   forward.Normalize();float degrees=Mathf.Clamp(arcDegrees,1,360);if(degrees>=360)return true;float halfAngle=degrees*.5f*Mathf.Deg2Rad;
   if(Vector3.Dot(forward,delta/distance)>=Mathf.Cos(halfAngle))return true;
   if(targetRadius<=0)return false;
   // A large target may cross either straight edge while its centre is outside the cone.
   Vector3 side=Vector3.Cross(Vector3.up,forward)*Mathf.Sin(halfAngle),front=forward*Mathf.Cos(halfAngle);
   float squaredRadius=targetRadius*targetRadius;
   return SegmentDistanceSquared(delta,(front+side)*range)<=squaredRadius||SegmentDistanceSquared(delta,(front-side)*range)<=squaredRadius;
  }
  static float SegmentDistanceSquared(Vector3 point,Vector3 end){float t=end.sqrMagnitude>.000001f?Mathf.Clamp01(Vector3.Dot(point,end)/end.sqrMagnitude):0;return (point-end*t).sqrMagnitude;}
  void Strike(float power,bool heavy){
   var stats=player.Stats;var tuning=GameBalance.Current.Combat;var game=GameManager.Instance;var enemies=game.Spawner.Active;
   var weapon=player.Character!=null?player.Character.Weapon:null;
   float push=tuning.Knockback*Mathf.Max(0,tuning.KnockbackMultiplier)*(weapon!=null?Mathf.Max(0,weapon.KnockbackMultiplier):1);
   float visualScale=Mathf.Max(.1f,player.Character!=null?player.Character.VisualScale:1)*Mathf.Max(.1f,GameBalance.Current.PlayerModelScale);
   BatSwingArc.Play(transform.position+Vector3.up*(1.15f*visualScale),transform.forward,stats.Range,heavy);
   int hits=0;bool anyCritical=false;Vector3 impactCenter=Vector3.zero;
   for(int i=enemies.Count-1;i>=0;i--){
    if(i>=enemies.Count)continue;var e=enemies[i];if(e==null||!e.Alive)continue;if(Mathf.Abs(e.transform.position.y-transform.position.y)>3)continue;
    Vector3 delta=e.transform.position-transform.position;delta.y=0;
    var capsule=e.GetComponent<CapsuleCollider>();Vector3 target=e.transform.position;float bodyRadius=0;
    if(capsule!=null){var scale=capsule.transform.lossyScale;bodyRadius=Mathf.Max(0,capsule.radius)*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));target=capsule.transform.TransformPoint(capsule.center);}
    bool melee=InMeleeSector(transform.position,transform.forward,target,stats.Range,tuning.ArcDegrees,tuning.InnerOmniRadius,bodyRadius);
    bool radial=stats.Shockwave>0&&delta.magnitude<=stats.Range+2.5f;if(!melee&&!radial)continue;
    bool critical=melee&&Random.value<stats.CritChance;
    float damage=melee?(stats.Damage+stats.FireDamage)*(critical?2:1)*power:0;if(radial)damage+=stats.Shockwave*power;
    float dealt=Mathf.Min(e.Health,damage);Vector3 direction=delta.sqrMagnitude>.001f?delta.normalized:transform.forward;
    e.TakeDamage(damage,heavy?direction*Mathf.Max(GameBalance.Current.Bodies.LaunchSpeed,push):Vector3.zero);player.Heal(dealt*stats.Lifesteal);hits++;anyCritical|=critical;impactCenter+=e.transform.position;
    HitEffects.Burst(e.transform.position+Vector3.up,stats.FireDamage>0?new Color(1,.35f,.04f):new Color(1,.9f,.75f),heavy?10:critical?8:4,heavy?1.1f:.45f);
    if(!game.IsPlaying)break;
   }
   rangeVisual?.Pulse(hits>0);
   if(hits>0){
    game.ReportHit(anyCritical,hits);player.ConfirmBatHit(heavy,hits);GameAudio.Impact(heavy,anyCritical,hits);
    if(heavy){visual.HoldImpact(GameBalance.Current.Feedback.HitPosePauseSeconds);HitEffects.HeavyImpact(impactCenter/hits,transform.forward);}
   }else game.ReportMiss();
   if(stats.Shockwave>0)HitEffects.Burst(transform.position+Vector3.up*.3f,Color.white,16);
  }
 }
}
