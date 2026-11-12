using UnityEngine;

namespace Brainrot
{
    public enum UpgradeKind { Damage, AttackSpeed, MaxHealth, MoveSpeed, Lifesteal, FireBat, Shockwave, Range, ExtraStrike, CriticalChance, Regeneration, DashCooldown }
    public enum UpgradeRarity { Common, Rare, Epic }

    [CreateAssetMenu(menuName = "Brainrot/Upgrade", fileName = "Upgrade")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        public UpgradeKind Kind;
        public string Title;
        [TextArea] public string Description;
        public UpgradeRarity Rarity;
        [Min(1)] public int MaxRank = 5;
        [Min(0)] public float Amount;
        public Color Color => Rarity == UpgradeRarity.Epic ? new Color(.88f,.4f,1f) : Rarity == UpgradeRarity.Rare ? new Color(.25f,.8f,1f) : new Color(.45f,1f,.65f);
    }
}
