using UnityEngine;

namespace Brainrot
{
    public enum UpgradeKind { [InspectorName("Урон")] Damage, [InspectorName("Скорость атаки")] AttackSpeed, [InspectorName("Максимальное здоровье")] MaxHealth, [InspectorName("Передвижение")] MoveSpeed, [InspectorName("Вампиризм")] Lifesteal, [InspectorName("Огненная бита")] FireBat, [InspectorName("Круговой удар")] Shockwave, [InspectorName("Дальность")] Range, [InspectorName("Дополнительный удар")] ExtraStrike, [InspectorName("Критический удар")] CriticalChance, [InspectorName("Регенерация")] Regeneration, [InspectorName("Перезарядка рывка")] DashCooldown }
    public enum UpgradeRarity { [InspectorName("Обычное")] Common, [InspectorName("Редкое")] Rare, [InspectorName("Эпическое")] Epic }

    [CreateAssetMenu(menuName = "Брейнрот/Улучшение", fileName = "Upgrade")]
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
