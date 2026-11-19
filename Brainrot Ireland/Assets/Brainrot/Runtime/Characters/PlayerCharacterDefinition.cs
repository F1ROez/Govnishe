using UnityEngine;
namespace Brainrot {
 public enum PlayerVisualStyle { [InspectorName("Тун-Тун Сахур")] TungTung }
 [CreateAssetMenu(menuName="TungTung/Персонаж игрока",fileName="PlayerCharacter")]
 public sealed class PlayerCharacterDefinition:ScriptableObject {
  public string Id="tung-tung";
  public string DisplayName="Тун-Тун Сахур";
  public PlayerVisualStyle VisualStyle=PlayerVisualStyle.TungTung;
  public Color BodyColor=new Color(.52f,.25f,.09f);
  [Min(.1f)]public float VisualScale=1;
  [Tooltip("Выключено: используются характеристики общего профиля баланса.")]
  public bool UseCustomStats;
  public PlayerTuning StartingStats=new PlayerTuning();
  public WeaponDefinition Weapon;
  public PlayerAbilityDefinition[] Abilities=new PlayerAbilityDefinition[0];
  static PlayerCharacterDefinition fallback;
  public static PlayerCharacterDefinition Default {
   get {if(fallback==null){fallback=Resources.Load<PlayerCharacterDefinition>("DefaultCharacter");if(fallback==null){fallback=CreateInstance<PlayerCharacterDefinition>();fallback.hideFlags=HideFlags.DontSave;}}return fallback;}
  }
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetDefault(){fallback=null;}
 }
}
