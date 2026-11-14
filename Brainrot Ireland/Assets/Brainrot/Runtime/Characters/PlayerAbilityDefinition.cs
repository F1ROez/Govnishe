using UnityEngine;
namespace Brainrot {
 /// <summary>A definition creates independent per-run state; never mutate shared assets during combat.</summary>
 public abstract class PlayerAbilityDefinition:ScriptableObject {
  public abstract IPlayerAbilityRuntime CreateRuntime(PlayerController owner);
 }
 public interface IPlayerAbilityRuntime {
  void Tick(float deltaTime);
  void OnAttack();
  void OnDamageTaken(float damage);
  void Dispose();
 }
}
