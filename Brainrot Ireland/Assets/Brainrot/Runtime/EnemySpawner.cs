using System.Collections.Generic;
using UnityEngine;

namespace Brainrot
{
    // Keep legacy numeric values for existing serialized assets. Legacy bosses are disabled.
    public enum EnemyKind
    {
        Normal = 0, Fast = 1, Tank = 2, MiniBoss = 3, King = 4,
        LiriliLarila = 5, BrrBrrPatapim = 6, CappuccinoAssassino = 7, BombardiroCrocodilo = 8
    }

    /// <summary>Random local population, independent of survival time or waves.</summary>
    public sealed class EnemySpawner : MonoBehaviour
    {
        public readonly List<EnemyController> Active = new List<EnemyController>(64);
        readonly Dictionary<EnemyKind, ObjectPool<EnemyController>> pools = new Dictionary<EnemyKind, ObjectPool<EnemyController>>();
        float spawnClock;
        bool zoneInitialized;
        IslandZone lastZone;
        public static int MaxEnemies => Mathf.Clamp(Application.isMobilePlatform ? GameBalance.Current.Exploration.MaxMobile : GameBalance.Current.Exploration.MaxDesktop, 1, 150);
        public IslandZone CurrentZone => GameManager.Instance != null && GameManager.Instance.Player != null ? IslandMap.GetZone(GameManager.Instance.Player.transform.position) : IslandZone.Beach;
        public int CurrentAliveLimit => Mathf.Clamp(GameBalance.Current.Exploration.ActiveZoneLimit, 1, MaxEnemies);

        ObjectPool<EnemyController> Pool(EnemyKind kind)
        {
            if (!pools.TryGetValue(kind, out var pool))
            {
                pool = new ObjectPool<EnemyController>(() =>
                {
                    var go = new GameObject("Враг_" + kind);
                    go.transform.SetParent(transform, false);
                    return go.AddComponent<EnemyController>();
                }, MaxEnemies);
                pools.Add(kind, pool);
            }
            return pool;
        }

        /// <summary>Also enforces zone/species rules for editor tools and scripted spawning.</summary>
        public EnemyController Spawn(EnemyKind kind, Vector3 position, float difficulty = 1)
        {
            if (kind == EnemyKind.MiniBoss || kind == EnemyKind.King || Active.Count >= MaxEnemies) return null;
            IslandZone zone = CurrentZone;
            if (!IslandMap.IsWalkable(position) || IslandMap.GetZone(position) != zone || kind != IslandMap.EnemyForZone(zone)) return null;
            if (CountInZone(zone) >= CurrentAliveLimit) return null;
            position.y = 0;
            var enemy = Pool(kind).Get();
            enemy.Initialize(this, kind, Mathf.Max(1, difficulty), position);
            Active.Add(enemy);
            return enemy;
        }

        public void Tick(float dt)
        {
            var game = GameManager.Instance;
            if (game == null || !game.IsPlaying || game.Player == null || dt <= 0) return;
            IslandZone zone = CurrentZone;
            var settings = GameBalance.Current.Exploration;
            if (!zoneInitialized || zone != lastZone)
            {
                zoneInitialized = true;
                lastZone = zone;
                TrimInactiveZones(zone);
                int missing = Mathf.Min(CurrentAliveLimit, Mathf.Max(0, settings.ZoneEntryPopulation)) - CountInZone(zone);
                for (int i = 0; i < missing; i++) TrySpawnLocal(zone);
                spawnClock = NextInterval();
            }
            else
            {
                spawnClock -= dt;
                if (spawnClock <= 0)
                {
                    // One randomized arrival; no accumulated burst after a slow frame.
                    spawnClock = NextInterval();
                    if (CountInZone(zone) < CurrentAliveLimit) TrySpawnLocal(zone);
                }
            }
            // All retained enemies receive a cheap idle/wander tick, never remote pursuit.
            for (int i = Active.Count - 1; i >= 0 && game.IsPlaying; i--)
                if (i < Active.Count) Active[i].Tick(dt);
        }

        bool TrySpawnLocal(IslandZone zone)
        {
            var game = GameManager.Instance;
            var settings = GameBalance.Current.Exploration;
            float min = Mathf.Max(3, settings.SpawnMinDistance);
            float max = Mathf.Max(min, settings.SpawnMaxDistance);
            if (!IslandMap.TrySpawnPoint(zone, game.Player.transform.position, min, max, out var point)) return false;
            float difficulty = 1 + (int)zone * Mathf.Max(0, settings.DifficultyPerZone);
            return Spawn(IslandMap.EnemyForZone(zone), point, difficulty) != null;
        }
        float NextInterval()
        {
            var settings = GameBalance.Current.Exploration;
            float min = Mathf.Max(.5f, settings.SpawnIntervalMin);
            float max = Mathf.Max(min, settings.SpawnIntervalMax);
            return Random.Range(min, max);
        }
        public int CountInZone(IslandZone zone)
        {
            int count = 0;
            foreach (var enemy in Active) if (enemy.Alive && enemy.HomeZone == zone) count++;
            return count;
        }
        void TrimInactiveZones(IslandZone current)
        {
            int retained = Mathf.Max(0, GameBalance.Current.Exploration.RetainedZoneLimit);
            for (int zoneIndex = 0; zoneIndex < 5; zoneIndex++)
            {
                var zone = (IslandZone)zoneIndex;
                if (zone == current) continue;
                int remaining = CountInZone(zone);
                for (int i = Active.Count - 1; i >= 0 && remaining > retained; i--)
                {
                    if (Active[i].HomeZone != zone) continue;
                    Despawn(Active[i]);remaining--;
                }
            }
            // A low mobile/custom hard cap must not prevent the newly entered zone populating.
            int targetRoom = Mathf.Min(CurrentAliveLimit, Mathf.Max(0, GameBalance.Current.Exploration.ZoneEntryPopulation));
            for (int i = Active.Count - 1; i >= 0 && Active.Count > MaxEnemies - targetRoom; i--)
                if (Active[i].HomeZone != current) Despawn(Active[i]);
        }
        internal void Despawn(EnemyController enemy)
        {
            if (enemy == null || !Active.Remove(enemy)) return;
            Pool(enemy.Kind).Release(enemy);
        }
        public void ResetRun()
        {
            for (int i = Active.Count - 1; i >= 0; i--) Pool(Active[i].Kind).Release(Active[i]);
            Active.Clear();
            zoneInitialized = false;
            spawnClock = 0;
        }
    }
}
