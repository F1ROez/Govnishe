using System.Collections.Generic;
using UnityEngine;

namespace Brainrot
{
    public sealed class UpgradeSystem : MonoBehaviour
    {
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public const int MaxLevel = 10000;
        public int RequiredExperience => Mathf.Max(1, GameBalance.Current.RequiredXp(Level));
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
            // Saturate before conversion; a malformed custom profile must never wrap XP or hang.
            Experience = (int)System.Math.Min(int.MaxValue, (long)Experience + amount);
            while (Level < MaxLevel && Experience >= RequiredExperience)
            {
                Experience -= RequiredExperience;
                Level++; pending++;
            }
            if (Level >= MaxLevel) Experience = 0;
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
                float amount = EffectiveAmount(definition);
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
                    case UpgradeKind.ExtraStrike: player.Stats.ExtraStrikes = Mathf.Min(4, player.Stats.ExtraStrikes + Mathf.RoundToInt(amount)); break;
                    case UpgradeKind.CriticalChance: player.Stats.CritChance = Mathf.Min(.75f, player.Stats.CritChance + amount); break;
                    case UpgradeKind.Regeneration: player.Stats.Regen += amount; break;
                    case UpgradeKind.DashCooldown: player.Stats.DashCooldown = Mathf.Max(.7f, player.Stats.DashCooldown * (1f - amount)); break;
                }
            }
            pending--; Choices.Clear();
            if (pending > 0) PresentChoices();
            else GameManager.Instance.ResumeAfterUpgrade();
        }

        /// <summary>Shared by application and UI so custom assets always show their real bonus.</summary>
        public float EffectiveAmount(UpgradeDefinition definition)
        {
            if (definition == null) return 0;
            float multiplier = GameBalance.Current.Progression.UpgradePowerMultiplier;
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier)) multiplier = 1;
            float amount = definition.Amount * Mathf.Clamp(multiplier, .1f, 10);
            if (float.IsNaN(amount) || float.IsInfinity(amount)) amount = 0;
            if (definition.Kind == UpgradeKind.ExtraStrike) return Mathf.Clamp(Mathf.RoundToInt(definition.Amount), 1, 2);
            if (definition.Kind == UpgradeKind.AttackSpeed || definition.Kind == UpgradeKind.DashCooldown) return Mathf.Clamp(amount, 0, .6f);
            return Mathf.Max(0, amount);
        }

        public string Describe(UpgradeDefinition definition)
        {
            if (definition == null) return "";
            if (Rank(definition) >= definition.MaxRank) return "Restore 50% of maximum health.";
            float amount = EffectiveAmount(definition);
            string number = amount.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            string percent = (amount * 100).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
            switch (definition.Kind)
            {
                case UpgradeKind.Damage: return "+" + number + " damage on every bat strike.";
                case UpgradeKind.AttackSpeed: return "Attack cooldown reduced by " + percent + "%. Minimum 0.12s.";
                case UpgradeKind.MaxHealth: return "+" + number + " maximum HP and heal " + number + " HP.";
                case UpgradeKind.MoveSpeed: return "+" + number + " movement speed.";
                case UpgradeKind.Lifesteal: return "Heal for an additional " + percent + "% of damage dealt. Maximum 30%.";
                case UpgradeKind.FireBat: return "+" + number + " fire damage on every hit.";
                case UpgradeKind.Shockwave: return "+" + number + " radial shockwave damage per attack.";
                case UpgradeKind.Range: return "+" + number + " melee reach; the ground outline expands.";
                case UpgradeKind.ExtraStrike: return "+" + number + " follow-up strike per attack. Maximum 4.";
                case UpgradeKind.CriticalChance: return "+" + percent + "% critical chance. Maximum 75%.";
                case UpgradeKind.Regeneration: return "Regenerate +" + number + " HP every second.";
                case UpgradeKind.DashCooldown: return "Dash cooldown reduced by " + percent + "%. Minimum 0.7s.";
                default: return definition.Description;
            }
        }

        private void EnsureDefinitions()
        {
            if (definitions != null && definitions.Length > 0) return;
            definitions = Resources.LoadAll<UpgradeDefinition>("Upgrades");
            if (definitions.Length > 0) return;
            definitions = new[]
            {
                Make(UpgradeKind.Damage,"BONK HARDER","Stronger bat strikes.",UpgradeRarity.Common,8,12),
                Make(UpgradeKind.AttackSpeed,"TUNG TURBO","Shorter attack cooldown.",UpgradeRarity.Common,6,.18f),
                Make(UpgradeKind.MaxHealth,"BIG LOG ENERGY","More maximum health and immediate healing.",UpgradeRarity.Common,8,40),
                Make(UpgradeKind.MoveSpeed,"SAND SPRINTER","Faster movement.",UpgradeRarity.Common,5,.85f),
                Make(UpgradeKind.Lifesteal,"SIPHON SAHUR","Heal when dealing damage.",UpgradeRarity.Rare,6,.05f),
                Make(UpgradeKind.FireBat,"SPICY TIMBER","Additional fire damage on every hit.",UpgradeRarity.Rare,6,10),
                Make(UpgradeKind.Shockwave,"ISLAND SLAM","Radial shockwave when attacking.",UpgradeRarity.Epic,5,16),
                Make(UpgradeKind.Range,"LONGER BONK","More melee reach.",UpgradeRarity.Common,5,.5f),
                Make(UpgradeKind.ExtraStrike,"DOUBLE TUNG","One additional strike per attack.",UpgradeRarity.Epic,2,1),
                Make(UpgradeKind.CriticalChance,"LUCK OF IRELAND","More chance to deal double damage.",UpgradeRarity.Rare,6,.12f),
                Make(UpgradeKind.Regeneration,"COCONUT RECOVERY","Regenerate health every second.",UpgradeRarity.Rare,6,1.3f),
                Make(UpgradeKind.DashCooldown,"SLIPPERY LOG","Shorter dash cooldown.",UpgradeRarity.Common,5,.22f)
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
