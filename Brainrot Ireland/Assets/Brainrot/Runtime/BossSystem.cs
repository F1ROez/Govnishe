using UnityEngine;

namespace Brainrot
{
    public sealed class BossSystem : MonoBehaviour
    {
        float nextMini, nextKing, lastKing = float.NegativeInfinity;
        bool warned;
        public EnemyController CurrentBoss
        {
            get
            {
                var game = GameManager.Instance;
                if (game == null || game.Spawner == null) return null;
                foreach (var enemy in game.Spawner.Active)
                    if (enemy.Alive && enemy.IsBoss) return enemy;
                return null;
            }
        }
        public void ResetRun()
        {
            var settings = GameBalance.Current.Bosses;
            nextMini = Mathf.Max(1, settings.MiniInterval);
            nextKing = Mathf.Max(1, settings.KingInterval);
            lastKing = float.NegativeInfinity;
            warned = false;
        }
        public void Tick(float dt)
        {
            var game = GameManager.Instance;
            if (game == null || !game.IsPlaying) return;
            var settings = GameBalance.Current.Bosses;
            float elapsed = game.Elapsed;
            if (!warned && elapsed >= nextKing - Mathf.Max(0, settings.WarningLead))
            {
                warned = true;
                game.Notify("THE OCEAN TREMBLES... KING TRALALA IS COMING!", 7);
            }
            if (elapsed >= nextKing)
            {
                Spawn(EnemyKind.King, 1 + elapsed / 600);
                game.Notify("KING TRALALA — DODGE THE ORANGE ATTACK OUTLINES!", 6);
                lastKing = elapsed;
                nextKing = elapsed + Mathf.Max(1, settings.KingInterval);
                warned = false;
            }
            if (elapsed >= nextMini)
            {
                if (elapsed - lastKing > 20 && nextKing - elapsed > 20) Spawn(EnemyKind.MiniBoss, 1 + elapsed / 500);
                nextMini = elapsed + Mathf.Max(1, settings.MiniInterval);
            }
        }
        static void Spawn(EnemyKind kind, float difficulty)
        {
            float a = Random.Range(0, Mathf.PI * 2);
            GameManager.Instance.Spawner.Spawn(kind, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * Mathf.Max(23, GameBalance.Current.Spawning.SpawnRadius), difficulty);
        }
    }
}
