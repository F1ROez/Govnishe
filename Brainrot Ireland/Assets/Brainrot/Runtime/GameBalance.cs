using System;
using UnityEngine;
namespace Brainrot
{
    [CreateAssetMenu(menuName="TungTung/Game Balance",fileName="GameBalance")]
    public sealed class GameBalance : ScriptableObject
    {
        public const string ResourcePath = "GameBalance";
        static GameBalance fallback;
        public static GameBalance Current {
            get {
                if (GameManager.Instance != null && GameManager.Instance.Balance != null) return GameManager.Instance.Balance;
                if (fallback == null) {
                    fallback = Resources.Load<GameBalance>(ResourcePath);
                    if (fallback == null) { fallback = CreateInstance<GameBalance>(); fallback.hideFlags = HideFlags.DontSave; }
                }
                return fallback;
            }
        }
        [Tooltip("Параметры нового забега. Спавн теперь зависит от текущей зоны острова.")]
        public PlayerTuning Player = new PlayerTuning();
        [Min(.1f),Tooltip("Размер модели игрока. 1 — базовый размер; применяется сразу. Не меняет дальность ударов.")]
        public float PlayerModelScale=1;
        [Min(.1f),Tooltip("Общий размер моделей всех врагов. Умножается на размер выбранного типа врага.")]
        public float EnemyModelScale=1;
        public StaminaTuning Stamina = new StaminaTuning();
        public CombatTuning Combat = new CombatTuning();
        public WaveTuning Waves = new WaveTuning();
        public BodyTuning Bodies = new BodyTuning();
        public OctopusTuning Octopus = new OctopusTuning();
        public SpawnTuning Spawning = new SpawnTuning();
        public ExplorationTuning Exploration = new ExplorationTuning();
        public ProgressionTuning Progression = new ProgressionTuning();
        public BossTuning Bosses = new BossTuning();
        public OceanTuning Ocean = new OceanTuning();
        public EnemyTuning Normal = new EnemyTuning();
        public EnemyTuning Fast = new EnemyTuning { Health=35,Speed=4.1f,Damage=11,CanCharge=true,ChargeMinDistance=4,ChargeMaxDistance=10,ChargeWindup=1.05f,ChargeSpeed=14,ChargeDuration=.5f,ChargeCooldown=7,Experience=10 };
        public EnemyTuning Tank = new EnemyTuning { Health=155,Speed=2.1f,Damage=18,MeleeWindup=.95f,Recovery=1.6f,KnockbackResistance=2,Experience=20 };
        public EnemyTuning MiniBoss = new EnemyTuning { Health=500,Speed=2.8f,Damage=22,AttackRange=2.5f,MeleeWindup=.9f,Recovery=1.4f,KnockbackResistance=4,HitStagger=0,Experience=100 };
        public EnemyTuning King = new EnemyTuning { Health=2000,Speed=2.6f,Damage=30,AttackRange=3.2f,MeleeWindup=1.1f,Recovery=1.6f,KnockbackResistance=6,HitStagger=0,Experience=300 };

        public EnemyTuning BlueOctopus=new EnemyTuning {Health=75,Speed=3,Damage=0,Experience=0};
        public EnemyTuning Lirili = new EnemyTuning {Health=100,Speed=2.4f,Damage=16,CanCharge=true,ChargeWindup=1.2f,ChargeSpeed=11,ChargeDuration=.7f,ChargeCooldown=7,KnockbackResistance=1.6f,Experience=18};
        public EnemyTuning Patapim = new EnemyTuning {Health=150,Speed=1.9f,Damage=23,AttackRange=3,MeleeWindup=1.1f,Recovery=1.6f,KnockbackResistance=1.8f,Experience=25};
        public EnemyTuning Cappuccino = new EnemyTuning {Health=85,Speed=3.7f,Damage=19,CanCharge=true,ChargeWindup=.7f,ChargeSpeed=14,ChargeDuration=.3f,ChargeCooldown=4,Recovery=.8f,Experience=30};
        public EnemyTuning Crocodilo = new EnemyTuning {Health=210,Speed=2.6f,Damage=28,CanCharge=true,ChargeWindup=1.3f,ChargeSpeed=12,ChargeDuration=.65f,ChargeCooldown=7,AttackRange=2.5f,KnockbackResistance=2,Experience=40};

        void OnEnable(){EnsureExploration();}
        public void EnsureExploration(){if(Stamina==null)Stamina=new StaminaTuning();if(Exploration==null)Exploration=new ExplorationTuning();if(Waves==null)Waves=new WaveTuning();if(Bodies==null)Bodies=new BodyTuning();if(Octopus==null)Octopus=new OctopusTuning();}
        public EnemyTuning Enemy(EnemyKind kind) {
            switch (kind) { case EnemyKind.BlueberrinniOctopusini:return BlueOctopus;case EnemyKind.LiriliLarila:return Lirili;case EnemyKind.BrrBrrPatapim:return Patapim;case EnemyKind.CappuccinoAssassino:return Cappuccino;case EnemyKind.BombardiroCrocodilo:return Crocodilo;case EnemyKind.Fast:return Fast;case EnemyKind.Tank:return Tank;case EnemyKind.MiniBoss:return MiniBoss;case EnemyKind.King:return King;default:return Normal; }
        }
        public int RequiredXp(int level) {
            double n=Math.Max(0,(double)level-1);
            double value=Progression.FirstLevelXp + Progression.XpLinearGrowth*n + Progression.XpQuadraticGrowth*n*n;
            if(double.IsNaN(value)||double.IsInfinity(value))return int.MaxValue;
            return (int)Math.Min(int.MaxValue,Math.Max(1,Math.Ceiling(value)));
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetCached(){fallback=null;}
    }
    [Serializable] public sealed class PlayerTuning {
        [Header("Начальные характеристики")]
        [Min(1)]public float Damage=24;
        [Min(.12f)]public float AttackInterval=.65f;
        [Min(1)]public float MaxHealth=100;
        [Min(.1f)]public float MoveSpeed=6;
        [Min(.5f)]public float Range=3.2f;
        [Range(0,1)]public float CritChance=.05f;
        [Header("Передвижение и камера")]
        public bool CameraLocked=true;
        [Min(.1f)]public float CameraDistance=9.5f;
        [Range(-179,179)]public float CameraPitch=38;
        [Min(.1f)]public float TurnSpeed=16;
        [Min(1)]public float SprintMultiplier=1.35f;
        [Min(.1f)]public float DashSpeed=20;
        [Min(.05f)]public float DashDuration=.18f;
        [Min(.1f)]public float DashCooldown=3;
    }
    [Serializable] public sealed class CombatTuning {
        [Range(20,180)]public float ArcDegrees=130;
        [Min(0)]public float InnerOmniRadius=.8f;
        [Min(0)]public float Knockback=4.5f;
        [Range(0,5)]public float KnockbackMultiplier=2;
        [Range(.15f,.6f)]public float SwingDuration=.32f;
        [Range(0,.2f)]public float ImpactDelay=.09f;
        [Min(.2f)]public float HeavyChargeSeconds=.6f;
        [Min(1)]public float StrongDamageMultiplier=2.5f;
        [Min(.1f)]public float StrongCooldownSeconds=10;
        [Range(1,3)]public float HeavyDamageMultiplier=1.35f;
        [Min(1)]public float HeavyCooldownMultiplier=1.4f;
        [Range(.03f,.5f)]public float HitFlashDuration=.16f;
        [Range(0,.65f)]public float HitFlashStrength=.25f;
        public bool ShowAttackRange=true;
    }
    [Serializable]public sealed class OctopusTuning {
        [Range(0,.5f)]public float SpawnChance=.18f;
        [Min(1)]public int FirstWave=2;
        [Min(4)]public float AimDistance=15;
        [Min(.3f)]public float AimSeconds=1.1f;
        [Min(.4f)]public float JumpSeconds=1.2f;
        [Min(1)]public float JumpHeight=7;
        [Min(.5f)]public float LandingRadius=1.3f;
        [Min(1)]public int EscapeClicks=12;
        [Min(1)]public float RetreatSpeed=6;
    }
    [Serializable]public sealed class StaminaTuning {
        [Min(1)]public float Maximum=100;
        [Min(.1f)]public float DrainPerSecond=25,RestorePerSecond=20;
        [Min(0)]public float RestoreDelay=.7f;
        [Min(1)]public float RestartThreshold=15;
    }
    [Serializable] public sealed class WaveTuning {
        [Min(10)]public float Duration=60;
        [Min(1)]public int MaxDesktop=64,MaxMobile=32;
        [Min(0)]public float Rest=8;
        [Min(.5f)]public float SlowInterval=4.5f;
        [Min(.15f)]public float PeakInterval=.65f;
        [Min(0)]public float InitialDelay=2;
        [Min(1)]public int EdgeAliveLimit=10,PeakAliveLimit=24;
        [Min(0)]public int ExtraAlivePerWave=2;
    }
    [Serializable] public sealed class BodyTuning {
        [Min(.3f)]public float TumbleSeconds=1.8f;
        [Min(1)]public float CorpseSeconds=7;
        [Min(1)]public float DeathBodyLifetime=4;
        [Min(1)]public float LaunchSpeed=11;
        [Min(0)]public float UpSpeed=3,Spin=12;
        [Range(0,1)]public float ChainPush=.55f;
        [Min(1)]public int MaxCorpsesPC=18,MaxCorpsesMobile=8;
    }
    [Serializable] public sealed class SpawnTuning {
        [Min(0)]public float InitialDelay=7;
        [Tooltip("Seconds of run on X; seconds between individual spawns on Y.")]
        public AnimationCurve Interval=new AnimationCurve(new Keyframe(0,5),new Keyframe(60,4),new Keyframe(180,2.6f),new Keyframe(300,1.7f),new Keyframe(600,.85f));
        [Tooltip("Seconds of run on X; soft maximum of ordinary enemies on Y. Bosses use reserved slots.")]
        public AnimationCurve AliveLimit=new AnimationCurve(new Keyframe(0,5),new Keyframe(60,8),new Keyframe(180,15),new Keyframe(300,25),new Keyframe(600,45));
        [Min(1)]public int MaxDesktop=100;
        [Min(1)]public int MaxMobile=50;
        [Min(0)]public float FastUnlock=60,TankUnlock=100;
        [Range(0,1)]public float FastChance=.22f,TankChance=.18f;
        [Min(0)]public float DifficultyPerMinute=.18f;
        [Min(23)]public float SpawnRadius=28;
    }
    [Serializable] public sealed class ExplorationTuning {
        [Min(1)]public float DetectionRadius=12;
        [Min(2)]public float LoseInterestRadius=20;
        [Min(3)]public float LeashDistance=25;
        [Min(0)]public float WanderRadius=6;
        [Min(0)]public float WanderSpeed=.8f;
        [Min(.1f)]public float IdleMin=1.5f,IdleMax=4;
        [Min(3)]public float SpawnMinDistance=14,SpawnMaxDistance=27;
        [Min(.5f)]public float SpawnIntervalMin=3,SpawnIntervalMax=6;
        [Min(1)]public int ActiveZoneLimit=10,RetainedZoneLimit=8;
        [Min(1)]public int MaxDesktop=80,MaxMobile=40;
        [Min(0)]public int ZoneEntryPopulation=4;
        [Min(0)]public float DifficultyPerZone=.35f;
    }
    [Serializable] public sealed class EnemyTuning {
        [Min(.1f),Tooltip("Размер модели этого вида врага; применяется вместе с общим размером врагов.")]
        public float ModelScale=1;
        [Min(1)]public float Health=48;
        [Min(.1f)]public float Speed=3.1f;
        [Min(0)]public float Damage=8;
        [Min(.5f)]public float AttackRange=1.8f;
        [Min(.15f)]public float MeleeWindup=.65f;
        [Min(.1f)]public float Recovery=1.1f;
        [Header("Атака рывком")]
        public bool CanCharge;
        [Min(1)]public float ChargeMinDistance=4,ChargeMaxDistance=10;
        [Min(.25f)]public float ChargeWindup=1.05f;
        [Min(1)]public float ChargeSpeed=14;
        [Min(.1f)]public float ChargeDuration=.5f;
        [Min(.5f)]public float ChargeCooldown=7;
        [Header("Получение урона и награда")]
        [Min(.1f)]public float KnockbackResistance=1;
        [Range(0,.5f)]public float HitStagger=.16f;
        [Min(0)]public int Experience=9;
    }
    [Serializable] public sealed class ProgressionTuning {
        [Min(1)]public int FirstLevelXp=54;
        [Min(0)]public float XpLinearGrowth=25;
        [Min(0)]public float XpQuadraticGrowth=8;
        [Tooltip("Applies to every selected upgrade, including existing editable catalog assets.")]
        [Range(.25f,3)]public float UpgradePowerMultiplier=1.5f;
    }
    [Serializable] public sealed class BossTuning {
        [Min(30)]public float MiniInterval=120,KingInterval=300;
        [Min(1)]public float WarningLead=12;
    }
    [Serializable] public sealed class OceanTuning {
        public Color DeepColor=new Color(.015f,.35f,.55f);
        public Color ShallowColor=new Color(.12f,.75f,.78f);
        [Range(0,2)]public float WaveSpeed=.6f;
        [Range(.05f,2)]public float WaveScale=.45f;
        [Range(0,.5f)]public float WaveStrength=.12f;
    }
}
