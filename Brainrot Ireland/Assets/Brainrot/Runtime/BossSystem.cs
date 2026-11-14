using UnityEngine;
namespace Brainrot
{
    /// <summary>Compatibility component. Boss content is deliberately disabled for exploration.</summary>
    public sealed class BossSystem : MonoBehaviour
    {
        public EnemyController CurrentBoss => null;
        public void ResetRun() { }
        public void Tick(float dt) { }
    }
}
