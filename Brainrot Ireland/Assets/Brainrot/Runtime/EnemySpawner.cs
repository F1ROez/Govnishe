using System.Collections.Generic;
using UnityEngine;

namespace Brainrot
{
    public enum EnemyKind { Normal, Fast, Tank, MiniBoss, King }

    public sealed class EnemySpawner : MonoBehaviour
    {
        public readonly List<EnemyController> Active = new List<EnemyController>(192);
        readonly Dictionary<EnemyKind, ObjectPool<EnemyController>> pools = new Dictionary<EnemyKind, ObjectPool<EnemyController>>();
        float spawnClock;
        public static int MaxEnemies => Application.isMobilePlatform ? 60 : 140;

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
            // Reserve the last few slots for scripted bosses and their escorts.
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
            if (game == null || !game.IsPlaying) return;
            spawnClock -= dt;
            if (spawnClock <= 0)
            {
                float minutes = game.Elapsed / 60f;
                spawnClock = Mathf.Max(.16f, 1.6f / (1 + minutes * .65f));
                int group = 1 + Mathf.Min(3, Mathf.FloorToInt(minutes / 2));
                for (int i = 0; i < group; i++)
                {
                    float roll = Random.value;
                    EnemyKind kind = game.Elapsed > 35 && roll < .22f ? EnemyKind.Fast :
                        game.Elapsed > 65 && roll > .77f ? EnemyKind.Tank : EnemyKind.Normal;
                    float angle = Random.Range(0f, Mathf.PI * 2);
                    Spawn(kind, new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 28f, 1 + minutes * .28f);
                }
            }
            // Enemies can return themselves to the pool during this loop.
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
            spawnClock = 1;
        }
    }
}
