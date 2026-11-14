using System.Collections.Generic;
using UnityEngine;
namespace Brainrot {
 public sealed class CombatSystem:MonoBehaviour {
  struct PendingContact {public float Time,Power;public bool Heavy;}
  readonly List<PendingContact> contacts=new List<PendingContact>(16);
  PlayerController player;CharacterVisual visual;AttackRangeVisual rangeVisual;
  float nextAttack,chargeStarted;bool charging,inputSuppressed;
  public bool IsCharging=>charging;
  public float ChargeProgress=>charging?Mathf.Clamp01((Time.time-chargeStarted)/Mathf.Max(.2f,GameBalance.Current.Combat.HeavyChargeSeconds)):0;
  public bool IsAttacking=>visual!=null&&visual.AttackActive;
  public void Initialize(PlayerController p,CharacterVisual v){player=p;visual=v;}
  public void SetRangeVisual(AttackRangeVisual value){rangeVisual=value;}
  public void CancelPendingSwings(){contacts.Clear();charging=false;inputSuppressed=true;visual?.ResetAttack();}
  public void ResetRun(){nextAttack=0;CancelPendingSwings();}
  public void UpdateAttackInput(bool held){
   if(inputSuppressed){if(!held)inputSuppressed=false;return;}
   if(held&&!charging&&Time.time>=nextAttack){charging=true;chargeStarted=Time.time;}
   if(!held&&charging){bool heavy=ChargeProgress>=1;charging=false;ExecuteAttack(heavy);}
  }
  public void TryAttack(){ExecuteAttack(false);}
  public bool ExecuteAttack(bool heavy){
   var game=GameManager.Instance;if(game==null||!game.IsPlaying||Time.time<nextAttack)return false;
   float cooldown=Mathf.Max(.12f,player.Stats.AttackInterval)*(heavy?Mathf.Max(1,GameBalance.Current.Combat.HeavyCooldownMultiplier):1);
   nextAttack=Time.time+cooldown;BeginSwing(heavy?Mathf.Max(1,GameBalance.Current.Combat.HeavyDamageMultiplier):1,heavy);return true;
  }
  void BeginSwing(float power,bool heavy=false){
   var tuning=GameBalance.Current.Combat;var weapon=player.Character!=null?player.Character.Weapon:null;
   float duration=Mathf.Min(weapon!=null&&weapon.UseCustomTiming?weapon.SwingDuration:tuning.SwingDuration,Mathf.Max(.15f,player.Stats.AttackInterval*.95f));
   float delay=Mathf.Clamp(weapon!=null&&weapon.UseCustomTiming?weapon.ImpactDelay:tuning.ImpactDelay,0,duration*.65f);
   visual.SetAttackReach(player.Stats.Range);visual.PlayAttack(duration);player.NotifyAttack();GameAudio.Play(270,.06f,.045f);
   if(contacts.Count<32)contacts.Add(new PendingContact {Time=Time.time+delay,Power=power,Heavy=heavy});
  }
  void Update(){
   var game=GameManager.Instance;if(game==null||!game.IsPlaying)return;
   for(int i=0;i<contacts.Count&&game.IsPlaying;){if(Time.time>=contacts[i].Time){float power=contacts[i].Power;bool heavy=contacts[i].Heavy;contacts.RemoveAt(i);Strike(power,heavy);}else i++;}
  }
  public static bool InMeleeSector(Vector3 origin,Vector3 forward,Vector3 target,float range,float arcDegrees,float innerRadius){
   Vector3 delta=target-origin;delta.y=0;float distance=delta.magnitude;
   if(distance>Mathf.Max(.1f,range))return false;
   if(distance<=Mathf.Max(0,innerRadius))return true;
   forward.y=0;if(forward.sqrMagnitude<.001f)return false;
   return Vector3.Dot(forward.normalized,delta/distance)>=Mathf.Cos(Mathf.Clamp(arcDegrees,1,360)*.5f*Mathf.Deg2Rad);
  }
  void Strike(float power,bool heavy){
   var stats=player.Stats;var tuning=GameBalance.Current.Combat;var game=GameManager.Instance;var enemies=game.Spawner.Active;
   var weapon=player.Character!=null?player.Character.Weapon:null;
   float push=tuning.Knockback*Mathf.Max(0,tuning.KnockbackMultiplier)*(weapon!=null?Mathf.Max(0,weapon.KnockbackMultiplier):1);
   int hits=0;bool anyCritical=false;
   for(int i=enemies.Count-1;i>=0;i--){
    if(i>=enemies.Count)continue;var e=enemies[i];if(!e.Alive)continue;
    Vector3 delta=e.transform.position-transform.position;delta.y=0;
    bool melee=InMeleeSector(transform.position,transform.forward,e.transform.position,stats.Range,tuning.ArcDegrees,tuning.InnerOmniRadius);
    bool radial=stats.Shockwave>0&&delta.magnitude<=stats.Range+2.5f;if(!melee&&!radial)continue;
    bool critical=melee&&Random.value<stats.CritChance;
    float damage=melee?(stats.Damage+stats.FireDamage)*(critical?2:1)*power:0;if(radial)damage+=stats.Shockwave*power;
    float dealt=Mathf.Min(e.Health,damage);Vector3 direction=delta.sqrMagnitude>.001f?delta.normalized:transform.forward;
    e.TakeDamage(damage,heavy?direction*Mathf.Max(GameBalance.Current.Bodies.LaunchSpeed,push):Vector3.zero);player.Heal(dealt*stats.Lifesteal);hits++;anyCritical|=critical;
    HitEffects.Burst(e.transform.position+Vector3.up,stats.FireDamage>0?new Color(1,.35f,.04f):new Color(1,.9f,.75f),critical?10:5);
    if(!game.IsPlaying)break;
   }
   rangeVisual?.Pulse(hits>0);
   if(hits>0){game.ReportHit(anyCritical,hits);GameAudio.Play(anyCritical?400:190,.08f,.15f);}else game.ReportMiss();
   if(stats.Shockwave>0)HitEffects.Burst(transform.position+Vector3.up*.3f,Color.white,16);
  }
 }
}
