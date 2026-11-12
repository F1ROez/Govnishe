using UnityEngine;
namespace Brainrot {
 public sealed class CombatSystem:MonoBehaviour {
  PlayerController player; CharacterVisual visual; AttackRangeVisual rangeVisual;
  float nextAttack,swingUntil,nextEcho;int echoes;
  public bool IsAttacking=>Time.time<swingUntil;
  public void Initialize(PlayerController p,CharacterVisual v){player=p;visual=v;}
  public void SetRangeVisual(AttackRangeVisual value){rangeVisual=value;}
  public void ResetRun(){nextAttack=swingUntil=nextEcho=0;echoes=0;}
  public void TryAttack(){if(!GameManager.Instance.IsPlaying||Time.time<nextAttack)return;nextAttack=Time.time+Mathf.Max(.12f,player.Stats.AttackInterval);echoes=player.Stats.ExtraStrikes;Strike(1);nextEcho=Time.time+.14f;}
  void Update(){if(!GameManager.Instance.IsPlaying)return;if(echoes>0&&Time.time>=nextEcho){echoes--;nextEcho=Time.time+.14f;Strike(.55f);}}
  public static bool InMeleeSector(Vector3 origin,Vector3 forward,Vector3 target,float range,float arcDegrees,float innerRadius){
   Vector3 delta=target-origin;delta.y=0;float distance=delta.magnitude;
   if(distance>Mathf.Max(.1f,range))return false;
   if(distance<=Mathf.Max(0,innerRadius))return true;
   forward.y=0;if(forward.sqrMagnitude<.001f)return false;
   return Vector3.Dot(forward.normalized,delta/distance)>=Mathf.Cos(Mathf.Clamp(arcDegrees,1,360)*.5f*Mathf.Deg2Rad);
  }
  void Strike(float power){
   swingUntil=Time.time+.22f;var stats=player.Stats;var balance=GameBalance.Current.Combat;var game=GameManager.Instance;var enemies=game.Spawner.Active;
   int hitCount=0;bool anyCritical=false;GameAudio.Play(270,.06f,.045f);
   for(int i=enemies.Count-1;i>=0;i--){
    if(i>=enemies.Count)continue;var e=enemies[i];if(!e.Alive)continue;
    Vector3 delta=e.transform.position-transform.position;delta.y=0;
    bool melee=InMeleeSector(transform.position,transform.forward,e.transform.position,stats.Range,balance.ArcDegrees,balance.InnerOmniRadius);
    bool wave=stats.Shockwave>0&&delta.magnitude<=stats.Range+2.5f;if(!melee&&!wave)continue;
    bool critical=melee&&Random.value<stats.CritChance;
    float damage=melee?(stats.Damage+stats.FireDamage)*(critical?2:1)*power:0;if(wave)damage+=stats.Shockwave*power;
    float dealt=Mathf.Min(e.Health,damage);
    Vector3 direction=delta.sqrMagnitude>.001f?delta.normalized:transform.forward;
    e.TakeDamage(damage,direction*Mathf.Max(0,balance.Knockback));player.Heal(dealt*stats.Lifesteal);
    hitCount++;anyCritical|=critical;
    HitEffects.Burst(e.transform.position+Vector3.up,stats.FireDamage>0?new Color(1,.35f,.04f):new Color(1,.85f,.4f),critical?10:5);
    if(!game.IsPlaying)break;
   }
   rangeVisual?.Pulse(hitCount>0);
   if(hitCount>0){game.ReportHit(anyCritical,hitCount);GameAudio.Play(anyCritical?400:190,.08f,.15f);}else game.ReportMiss();
   if(stats.Shockwave>0)HitEffects.Burst(transform.position+Vector3.up*.3f,Color.cyan,16);
  }
 }
}