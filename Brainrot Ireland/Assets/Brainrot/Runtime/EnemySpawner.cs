using System.Collections.Generic;
using UnityEngine;

namespace Brainrot
{
    public enum EnemyKind { Normal, Fast, Tank, MiniBoss, King }

    public sealed class EnemySpawner : MonoBehaviour
    {
        public readonly List<EnemyController> Active = new List<EnemyController>(128);
        readonly Dictionary<EnemyKind, ObjectPool<EnemyController>> pools = new Dictionary<EnemyKind, ObjectPool<EnemyController>>();
        float spawnClock;
        public static int MaxEnemies => Mathf.Clamp(Application.isMobilePlatform ? GameBalance.Current.Spawning.MaxMobile : GameBalance.Current.Spawning.MaxDesktop, 1, 250);
        public int CurrentAliveLimit
        {
            get
            {
                var curve = GameBalance.Current.Spawning.AliveLimit;
                float elapsed = GameManager.Instance != null ? GameManager.Instance.Elapsed : 0;
                return Mathf.Clamp(Mathf.FloorToInt(curve != null ? curve.Evaluate(elapsed) : 6), 1, MaxEnemies);
            }
        }
        ObjectPool<EnemyController> Pool(EnemyKind kind)
        {
            if (!pools.TryGetValue(kind, out var pool))
            {
                pool = new ObjectPool<EnemyController>(() =>
                {
                    var go = new GameObject("Shark_" + kind);
                    go.transform.SetParent(transform, false);
                    return go.AddComponent<EnemyController>();
                }, MaxEnemies);
                pools.Add(kind, pool);
            }
            return pool;
        }
        public EnemyController Spawn(EnemyKind kind, Vector3 position, float difficulty = 1)
        {
            if (Active.Count >= MaxEnemies && kind != EnemyKind.King && kind != EnemyKind.MiniBoss) return null;
            if (Active.Count >= MaxEnemies + 4) return null;
            var enemy = Pool(kind).Get();
            enemy.Initialize(this, kind, Mathf.Max(1, difficulty), position);
            Active.Add(enemy);
            return enemy;
        }
        public void Tick(float dt)
        {
            var game = GameManager.Instance;
            if (game == null || !game.IsPlaying || dt <= 0) return;
            spawnClock -= dt;
            if (spawnClock <= 0)
            {
                var settings = GameBalance.Current.Spawning;
                // One spawn per beat; no delayed catch-up bursts after a slow frame.
                spawnClock = Mathf.Max(.35f, settings.Interval != null ? settings.Interval.Evaluate(game.Elapsed) : 5);
                int ordinary = 0;
                foreach (var active in Active) if (active.Alive && !active.IsBoss) ordinary++;
                if (ordinary < CurrentAliveLimit)
                {
                    float fast = game.Elapsed >= settings.FastUnlock ? Mathf.Clamp01(settings.FastChance) : 0;
                    float tank = game.Elapsed >= settings.TankUnlock ? Mathf.Clamp01(settings.TankChance) : 0;
                    float roll = Random.value;
                    EnemyKind kind = roll < fast ? EnemyKind.Fast : roll < fast + tank ? EnemyKind.Tank : EnemyKind.Normal;
                    float angle = Random.Range(0f, Mathf.PI * 2);
                    Spawn(kind, new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * Mathf.Max(23, settings.SpawnRadius), 1 + game.Elapsed / 60 * Mathf.Max(0, settings.DifficultyPerMinute));
                }
            }
            for (int i = Active.Count - 1; i >= 0 && game.IsPlaying; i--)
                if (i < Active.Count) Active[i].Tick(dt);
        }
        internal void Despawn(EnemyController enemy)
        {
            if (!Active.Remove(enemy)) return;
            Pool(enemy.Kind).Release(enemy);
        }
        public void ResetRun()
        {
            for (int i = Active.Count - 1; i >= 0; i--) Pool(Active[i].Kind).Release(Active[i]);
            Active.Clear();
            spawnClock = Mathf.Max(0, GameBalance.Current.Spawning.InitialDelay);
        }
    }
}
