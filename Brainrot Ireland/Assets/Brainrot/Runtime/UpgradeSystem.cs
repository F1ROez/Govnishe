using System.Collections.Generic;
using UnityEngine;

namespace Brainrot
{
    public sealed class UpgradeSystem : MonoBehaviour
    {
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int RequiredExperience => 12 + (Level - 1) * 8;
        public List<UpgradeDefinition> Choices { get; } = new List<UpgradeDefinition>(3);
        [SerializeField] private UpgradeDefinition[] definitions;
        private readonly Dictionary<UpgradeKind, int> ranks = new Dictionary<UpgradeKind, int>();
        private int pending;

        private void Awake() { EnsureDefinitions(); }

        public void ResetRun()
        {
            EnsureDefinitions();
            Level = 1; Experience = 0; pending = 0;
            ranks.Clear(); Choices.Clear();
        }

        public int Rank(UpgradeDefinition definition)
        {
            return definition != null && ranks.TryGetValue(definition.Kind, out int rank) ? rank : 0;
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0) return;
            Experience += amount;
            while (Experience >= RequiredExperience)
            {
                Experience -= RequiredExperience;
                Level++; pending++;
            }
            if (pending > 0 && Choices.Count == 0) PresentChoices();
        }

        private void PresentChoices()
        {
            Choices.Clear();
            var available = new List<UpgradeDefinition>();
            foreach (var definition in definitions)
                if (definition != null && Rank(definition) < definition.MaxRank) available.Add(definition);
            // When all permanent bonuses are mastered, level rewards remain useful healing choices.
            if (available.Count == 0)
            {
                foreach (var definition in definitions)
                    if (definition != null && definition.Kind == UpgradeKind.MaxHealth) available.Add(definition);
            }
            while (Choices.Count < 3 && available.Count > 0)
            {
                int total = 0;
                foreach (var item in available) total += Weight(item.Rarity);
                int pick = Random.Range(0, total);
                int index = 0;
                while (index < available.Count - 1 && (pick -= Weight(available[index].Rarity)) >= 0) index++;
                Choices.Add(available[index]); available.RemoveAt(index);
            }
            GameManager.Instance.ShowUpgrades();
        }

        private static int Weight(UpgradeRarity rarity) => rarity == UpgradeRarity.Epic ? 2 : rarity == UpgradeRarity.Rare ? 4 : 7;

        public void Choose(int index)
        {
            if (pending <= 0 || index < 0 || index >= Choices.Count) return;
            UpgradeDefinition definition = Choices[index];
            var player = GameManager.Instance.Player;
            if (player == null) return;
            int rank = Rank(definition);
            if (rank >= definition.MaxRank) player.Heal(player.Stats.MaxHealth * .5f);
            else
            {
                ranks[definition.Kind] = rank + 1;
                float amount = definition.Amount;
                switch (definition.Kind)
                {
                    case UpgradeKind.Damage: player.Stats.Damage += amount; break;
                    case UpgradeKind.AttackSpeed: player.Stats.AttackInterval = Mathf.Max(.12f, player.Stats.AttackInterval * (1f - amount)); break;
                    case UpgradeKind.MaxHealth: player.Stats.MaxHealth += amount; player.Heal(amount); break;
                    case UpgradeKind.MoveSpeed: player.Stats.MoveSpeed += amount; break;
                    case UpgradeKind.Lifesteal: player.Stats.Lifesteal = Mathf.Min(.3f, player.Stats.Lifesteal + amount); break;
                    case UpgradeKind.FireBat: player.Stats.FireDamage += amount; break;
                    case UpgradeKind.Shockwave: player.Stats.Shockwave += amount; break;
                    case UpgradeKind.Range: player.Stats.Range += amount; break;
                    case UpgradeKind.ExtraStrike: player.Stats.ExtraStrikes += Mathf.RoundToInt(amount); break;
                    case UpgradeKind.CriticalChance: player.Stats.CritChance = Mathf.Min(.75f, player.Stats.CritChance + amount); break;
                    case UpgradeKind.Regeneration: player.Stats.Regen += amount; break;
                    case UpgradeKind.DashCooldown: player.Stats.DashCooldown = Mathf.Max(.7f, player.Stats.DashCooldown * (1f - amount)); break;
                }
            }
            pending--; Choices.Clear();
            if (pending > 0) PresentChoices();
            else GameManager.Instance.ResumeAfterUpgrade();
        }

        private void EnsureDefinitions()
        {
            if (definitions != null && definitions.Length > 0) return;
            definitions = Resources.LoadAll<UpgradeDefinition>("Upgrades");
            if (definitions.Length > 0) return;
            definitions = new[]
            {
                Make(UpgradeKind.Damage,"BONK HARDER","+6 damage with every bat strike.",UpgradeRarity.Common,8,6),
                Make(UpgradeKind.AttackSpeed,"TUNG TURBO","Attack 10% faster.",UpgradeRarity.Common,6,.1f),
                Make(UpgradeKind.MaxHealth,"BIG LOG ENERGY","+25 maximum HP and heal 25 HP. At mastery, restores 50% HP.",UpgradeRarity.Common,8,25),
                Make(UpgradeKind.MoveSpeed,"SAND SPRINTER","+0.5 movement speed.",UpgradeRarity.Common,5,.5f),
                Make(UpgradeKind.Lifesteal,"SIPHON SAHUR","Heal for 3% of damage dealt.",UpgradeRarity.Rare,6,.03f),
                Make(UpgradeKind.FireBat,"SPICY TIMBER","+5 burning damage on every hit.",UpgradeRarity.Rare,6,5),
                Make(UpgradeKind.Shockwave,"ISLAND SLAM","+8 radial shockwave damage when attacking.",UpgradeRarity.Epic,5,8),
                Make(UpgradeKind.Range,"LONGER BONK","+0.35 melee reach.",UpgradeRarity.Common,5,.35f),
                Make(UpgradeKind.ExtraStrike,"DOUBLE TUNG","One additional strike per attack.",UpgradeRarity.Epic,2,1),
                Make(UpgradeKind.CriticalChance,"LUCK OF IRELAND","+8% chance to deal double damage.",UpgradeRarity.Rare,6,.08f),
                Make(UpgradeKind.Regeneration,"COCONUT RECOVERY","Regenerate 0.7 HP per second.",UpgradeRarity.Rare,6,.7f),
                Make(UpgradeKind.DashCooldown,"SLIPPERY LOG","Dash cooldown reduced by 15%.",UpgradeRarity.Common,5,.15f)
            };
        }

        private static UpgradeDefinition Make(UpgradeKind kind,string title,string description,UpgradeRarity rarity,int maxRank,float amount)
        {
            var definition = ScriptableObject.CreateInstance<UpgradeDefinition>();
            definition.name = kind.ToString(); definition.Kind = kind; definition.Title = title;
            definition.Description = description; definition.Rarity = rarity; definition.MaxRank = maxRank; definition.Amount = amount;
            definition.hideFlags = HideFlags.DontSave;
            return definition;
        }
    }
}
