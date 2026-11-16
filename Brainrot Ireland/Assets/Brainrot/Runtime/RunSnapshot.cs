using System;
using UnityEngine;
namespace Brainrot
{
    [Serializable] public sealed class RunSnapshot
    {
        public const int CurrentVersion=1;
        public int Version=CurrentVersion;
        public float Elapsed;
        public int Kills;
        public int ChallengeMaskEarnedThisRun;
        public PlayerRunSnapshot Player;
        public WaveRunSnapshot Wave;
        public SpawnerRunSnapshot Spawner;
    }
    [Serializable] public sealed class PlayerRunSnapshot
    {
        public Vector3 Position;
        public float RotationYaw,CameraYaw,CameraPitch,Health,Stamina,DashCooldown,HeavyCooldown;
    }
    [Serializable] public sealed class WaveRunSnapshot
    {
        public int Number=1;
        public float Time,RestTime;
        public bool Resting,WaitingForClear;
    }
    [Serializable] public sealed class SpawnerRunSnapshot
    {
        public float SpawnClock;
        public int AnnouncedWave;
        // Added fields default to zero in legacy checkpoints; the spawner migrates these on restore.
        public int BudgetWave,PlannedCount,SpawnedCount,ResolvedCount;
        public bool OctopusIntroduced,OrcaladonIntroduced;
        public EnemyRunSnapshot[] Enemies=Array.Empty<EnemyRunSnapshot>();
    }
    [Serializable] public sealed class EnemyRunSnapshot
    {
        public int Kind;
        public Vector3 Position;
        public float RotationYaw,Health,MaxHealth;
    }
}
