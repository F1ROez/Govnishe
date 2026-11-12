using UnityEngine;
namespace Brainrot {
 public sealed class CombatSystem:MonoBehaviour {
  PlayerController player;CharacterVisual visual;float nextAttack,swingUntil,nextEcho;int echoes;
  public bool IsAttacking=>Time.time<swingUntil;
  public void Initialize(PlayerController p,CharacterVisual v){player=p;visual=v;} public void ResetRun(){nextAttack=swingUntil=nextEcho=0;echoes=0;}
  public void TryAttack(){if(Time.time<nextAttack)return;nextAttack=Time.time+Mathf.Max(.12f,player.Stats.AttackInterval);echoes=player.Stats.ExtraStrikes;Strike(1);nextEcho=Time.time+.14f;}
  void Update(){if(!GameManager.Instance.IsPlaying)return;if(echoes>0&&Time.time>=nextEcho){echoes--;nextEcho=Time.time+.14f;Strike(.55f);}}
  void Strike(float power){swingUntil=Time.time+.22f;var stats=player.Stats;var enemies=GameManager.Instance.Spawner.Active;GameAudio.Play(270,.06f,.08f);
   // Reverse traversal remains valid when a kill removes the current pooled enemy.
   for(int i=enemies.Count-1;i>=0;i--){if(i>=enemies.Count)continue;var e=enemies[i];if(!e.Alive)continue;Vector3 delta=e.transform.position-transform.position;delta.y=0;float distance=delta.magnitude;
    bool inArc=distance<stats.Range&& (distance<1.1f||Vector3.Dot(transform.forward,delta.normalized)>.05f);bool wave=stats.Shockwave>0&&distance<stats.Range+2.5f;
    if(!inArc&&!wave)continue;float damage=(stats.Damage+stats.FireDamage)*(Random.value<stats.CritChance?2:1)*power*(inArc?1:0);if(wave)damage+=stats.Shockwave*power;float dealt=Mathf.Min(e.Health,damage);e.TakeDamage(damage,delta.normalized*4);player.Heal(dealt*stats.Lifesteal);HitEffects.Burst(e.transform.position+Vector3.up,stats.FireDamage>0?new Color(1,.35f,.04f):Color.yellow,6);if(!GameManager.Instance.IsPlaying)break;
   }
   if(stats.Shockwave>0)HitEffects.Burst(transform.position+Vector3.up*.3f,Color.cyan,16);
  }
 }
}