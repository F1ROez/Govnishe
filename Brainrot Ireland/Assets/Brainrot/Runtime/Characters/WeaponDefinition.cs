using UnityEngine;
namespace Brainrot {
 [CreateAssetMenu(menuName="TungTung/Оружие",fileName="Weapon")]
 public sealed class WeaponDefinition:ScriptableObject {
  public string DisplayName="Деревянная бита";
  [Min(.1f)]public float DamageMultiplier=1,RangeMultiplier=1,AttackIntervalMultiplier=1,KnockbackMultiplier=1;
  public bool UseCustomTiming;
  [Range(.15f,.6f)]public float SwingDuration=.32f;
  [Range(0,.2f)]public float ImpactDelay=.09f;
 }
}
