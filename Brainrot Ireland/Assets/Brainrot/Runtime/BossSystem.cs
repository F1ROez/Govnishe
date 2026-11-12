using UnityEngine;

namespace Brainrot
{
    public sealed class BossSystem : MonoBehaviour
    {
        float nextMini = 120, nextKing = 300;
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
        public void ResetRun() { nextMini = 120; nextKing = 300; warned = false; }
        public void Tick(float dt)
        {
            var game = GameManager.Instance;
            if (game == null || !game.IsPlaying) return;
            float elapsed = game.Elapsed;
            if (!warned && elapsed >= nextKing - 12)
            {
                warned = true;
                game.Notify("THE OCEAN TREMBLES... KING TRALALA IS COMING!", 7);
            }
            if (elapsed >= nextKing)
            {
                Spawn(EnemyKind.King, 1 + elapsed / 600);
                game.Notify("KING TRALALA — DODGE THE RED ATTACK ZONES!", 6);
                nextKing += 300;
                warned = false;
            }
            if (elapsed >= nextMini)
            {
                // Keep the fifth-minute boss entrance readable.
                if (Mathf.Abs(elapsed - (nextKing - 300)) > 20) Spawn(EnemyKind.MiniBoss, 1 + elapsed / 500);
                nextMini += 120;
            }
        }
        static void Spawn(EnemyKind kind, float difficulty)
        {
            float a = Random.Range(0, Mathf.PI * 2);
            GameManager.Instance.Spawner.Spawn(kind, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 27, difficulty);
        }
    }
}
