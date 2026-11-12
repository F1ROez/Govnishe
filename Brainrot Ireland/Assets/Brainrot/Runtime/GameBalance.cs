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
        [Tooltip("Changes are applied to new runs. Spawn curves are evaluated in seconds.")]
        public PlayerTuning Player = new PlayerTuning();
        public CombatTuning Combat = new CombatTuning();
        public SpawnTuning Spawning = new SpawnTuning();
        public ProgressionTuning Progression = new ProgressionTuning();
        public BossTuning Bosses = new BossTuning();
        public OceanTuning Ocean = new OceanTuning();
        public EnemyTuning Normal = new EnemyTuning();
        public EnemyTuning Fast = new EnemyTuning { Health=35,Speed=4.1f,Damage=11,CanCharge=true,ChargeMinDistance=4,ChargeMaxDistance=10,ChargeWindup=1.05f,ChargeSpeed=14,ChargeDuration=.5f,ChargeCooldown=7,Experience=10 };
        public EnemyTuning Tank = new EnemyTuning { Health=155,Speed=2.1f,Damage=18,MeleeWindup=.95f,Recovery=1.6f,KnockbackResistance=2,Experience=20 };
        public EnemyTuning MiniBoss = new EnemyTuning { Health=500,Speed=2.8f,Damage=22,AttackRange=2.5f,MeleeWindup=.9f,Recovery=1.4f,KnockbackResistance=4,HitStagger=0,Experience=100 };
        public EnemyTuning King = new EnemyTuning { Health=2000,Speed=2.6f,Damage=30,AttackRange=3.2f,MeleeWindup=1.1f,Recovery=1.6f,KnockbackResistance=6,HitStagger=0,Experience=300 };

        public EnemyTuning Enemy(EnemyKind kind) {
            switch (kind) { case EnemyKind.Fast:return Fast;case EnemyKind.Tank:return Tank;case EnemyKind.MiniBoss:return MiniBoss;case EnemyKind.King:return King;default:return Normal; }
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
        [Header("Starting stats")]
        [Min(1)]public float Damage=24;
        [Min(.12f)]public float AttackInterval=.65f;
        [Min(1)]public float MaxHealth=100;
        [Min(.1f)]public float MoveSpeed=6;
        [Min(.5f)]public float Range=3.2f;
        [Range(0,1)]public float CritChance=.05f;
        [Header("Movement and camera")]
        public bool CameraLocked=true;
        [Min(.1f)]public float CameraDistance=9.5f;
        [Range(15,70)]public float CameraPitch=38;
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
        [Range(.03f,.5f)]public float HitFlashDuration=.16f;
        [Range(0,.65f)]public float HitFlashStrength=.25f;
        public bool ShowAttackRange=true;
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
    [Serializable] public sealed class EnemyTuning {
        [Min(1)]public float Health=48;
        [Min(.1f)]public float Speed=3.1f;
        [Min(0)]public float Damage=8;
        [Min(.5f)]public float AttackRange=1.8f;
        [Min(.15f)]public float MeleeWindup=.65f;
        [Min(.1f)]public float Recovery=1.1f;
        [Header("Charge attack")]
        public bool CanCharge;
        [Min(1)]public float ChargeMinDistance=4,ChargeMaxDistance=10;
        [Min(.25f)]public float ChargeWindup=1.05f;
        [Min(1)]public float ChargeSpeed=14;
        [Min(.1f)]public float ChargeDuration=.5f;
        [Min(.5f)]public float ChargeCooldown=7;
        [Header("Hit reaction and reward")]
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
