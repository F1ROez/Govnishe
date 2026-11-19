using UnityEditor;
namespace Brainrot.Editor {
 [CustomEditor(typeof(PlayerCharacterDefinition))]public sealed class PlayerCharacterInspector:GameBalanceInspector { }
 [CustomEditor(typeof(WeaponDefinition))]public sealed class WeaponInspector:GameBalanceInspector { }
 [CustomEditor(typeof(UpgradeDefinition))]public sealed class UpgradeInspector:GameBalanceInspector { }
}
