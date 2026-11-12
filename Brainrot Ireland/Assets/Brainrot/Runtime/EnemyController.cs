using UnityEngine;

namespace Brainrot
{
    public sealed class EnemyController : MonoBehaviour
    {
        public bool Alive { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsBoss => Kind == EnemyKind.King || Kind == EnemyKind.MiniBoss;
        public EnemyKind Kind { get; private set; }
        EnemySpawner owner;
        CharacterVisual visual;
        float speed, damage, attackClock, abilityClock, phaseClock, size, difficulty;
        int abilityIndex, phase;
        bool shockHit;
        Vector3 knockVelocity, dashDirection, targetArea;
        GameObject warning;
        Color tint;
        float Radius => Kind == EnemyKind.King ? 2.1f : Kind == EnemyKind.MiniBoss ? 1.4f : .7f;

        public void Initialize(EnemySpawner owner, EnemyKind kind, float difficulty, Vector3 position)
        {
            this.owner = owner;
            Kind = kind;
            this.difficulty = difficulty;
            size = kind == EnemyKind.King ? 3f : kind == EnemyKind.MiniBoss ? 1.9f : kind == EnemyKind.Tank ? 1.45f : kind == EnemyKind.Fast ? .8f : 1f;
            tint = kind == EnemyKind.King ? new Color(.52f,.24f,.84f) : kind == EnemyKind.MiniBoss ? new Color(.94f,.34f,.3f) : kind == EnemyKind.Tank ? new Color(.22f,.42f,.6f) : kind == EnemyKind.Fast ? new Color(.15f,.88f,.77f) : new Color(.23f,.65f,.95f);
            float baseHealth = kind == EnemyKind.King ? 1800 : kind == EnemyKind.MiniBoss ? 420 : kind == EnemyKind.Tank ? 140 : kind == EnemyKind.Fast ? 25 : 48;
            MaxHealth = Health = baseHealth * difficulty;
            speed = (kind == EnemyKind.Fast ? 5.7f : kind == EnemyKind.Tank ? 2.1f : IsBoss ? 2.8f : 3.25f) * Mathf.Min(1.5f, 1 + (difficulty - 1) * .07f);
            damage = (kind == EnemyKind.King ? 28 : kind == EnemyKind.MiniBoss ? 19 : kind == EnemyKind.Tank ? 14 : 8) * Mathf.Sqrt(difficulty);
            if (visual == null) visual = CharacterVisual.Create(transform, true, tint, size);
            transform.position = position;
            transform.rotation = Quaternion.identity;
            knockVelocity = Vector3.zero;
            attackClock = .7f;
            abilityClock = IsBoss ? 3 : 9999;
            phase = abilityIndex = 0;
            Alive = true;
            if (warning != null) warning.SetActive(false);
        }
        public void Tick(float dt)
        {
            var game = GameManager.Instance;
            if (!Alive || game == null || !game.IsPlaying || game.Player == null || game.Player.Health <= 0) return;
            Vector3 toPlayer = game.Player.transform.position - transform.position;
            toPlayer.y = 0;
            float distance = toPlayer.magnitude;
            Vector3 direction = distance > .001f ? toPlayer / distance : transform.forward;
            attackClock -= dt;
            abilityClock -= dt;
            if (phase != 0)
            {
                TickAbility(dt, direction);
                visual.Animate(phase == 2 ? 1 : 0, phase == 2);
                return;
            }
            if (IsBoss && abilityClock <= 0)
            {
                phase = 1;
                phaseClock = 1.25f;
                dashDirection = direction;
                ShowWarning(abilityIndex % 4 == 0 ? 3 : abilityIndex % 4 == 1 ? 1 : 6.5f);
                if (abilityIndex % 4 == 3)
                {
                    targetArea = game.Player.transform.position;
                    targetArea.y = .035f;
                    warning.transform.position = targetArea;
                }
                return;
            }
            float stop = Radius + .5f;
            Vector3 move = distance > stop ? direction * speed : Vector3.zero;
            // A tiny deterministic lateral drift breaks perfectly overlapping crowds.
            if (distance > 3) move += Vector3.Cross(direction, Vector3.up) * Mathf.Sin(GetInstanceID() * .71f + game.Elapsed) * .35f;
            transform.position += (move + knockVelocity) * dt;
            knockVelocity = Vector3.Lerp(knockVelocity, Vector3.zero, dt * 8);
            if (direction.sqrMagnitude > .1f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), dt * 10);
            bool attack = distance < stop + .55f && attackClock <= 0;
            if (attack)
            {
                attackClock = IsBoss ? 1.2f : 1;
                game.Player.TakeDamage(damage, transform.position);
            }
            visual.Animate(move.magnitude, attackClock > .82f);
        }
        void TickAbility(float dt, Vector3 direction)
        {
            phaseClock -= dt;
            int ability = abilityIndex % 4;
            if (phase == 1)
            {
                if (phaseClock > 0) return;
                if (warning != null) warning.SetActive(false);
                if (ability == 0)
                {
                    phase = 2;
                    phaseClock = .65f;
                    transform.rotation = Quaternion.LookRotation(dashDirection);
                    attackClock = 0;
                    return;
                }
                if (ability == 1)
                {
                    phase = 3;
                    phaseClock = 1.4f;
                    shockHit = false;
                    if (warning != null) warning.SetActive(true);
                    return;
                }
                if (ability == 3)
                {
                    HitEffects.Burst(targetArea + Vector3.up, new Color(1,.42f,.12f), 24);
                    if (Vector3.Distance(targetArea, GameManager.Instance.Player.transform.position) < 6.5f)
                        GameManager.Instance.Player.TakeDamage(damage * 1.4f, targetArea);
                }
                else
                {
                    int count = Kind == EnemyKind.King ? 6 : 3;
                    for (int i = 0; i < count; i++)
                    {
                        float a = i * Mathf.PI * 2 / count;
                        owner.Spawn(EnemyKind.Normal, transform.position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 3, difficulty);
                    }
                    HitEffects.Burst(transform.position + Vector3.up * 2, tint, 18);
                }
                FinishAbility();
            }
            else if (phase == 3)
            {
                float radius = Mathf.Lerp(1, 10, 1 - phaseClock / 1.4f);
                if (warning != null) warning.transform.localScale = new Vector3(radius * 2, .014f, radius * 2);
                float distance = Vector3.Distance(transform.position, GameManager.Instance.Player.transform.position);
                if (!shockHit && Mathf.Abs(distance - radius) < 1.2f)
                {
                    shockHit = true;
                    GameManager.Instance.Player.TakeDamage(damage, transform.position);
                }
                if (phaseClock <= 0)
                {
                    if (warning != null) warning.SetActive(false);
                    FinishAbility();
                }
            }
            else
            {
                Vector3 next = transform.position + dashDirection * (18 * dt);
                Vector2 planar = new Vector2(next.x, next.z);
                if (planar.magnitude > 23) { planar = planar.normalized * 23; next.x = planar.x; next.z = planar.y; }
                transform.position = next;
                attackClock -= dt;
                if (attackClock <= 0 && Vector3.Distance(transform.position, GameManager.Instance.Player.transform.position) < Radius + 1)
                {
                    GameManager.Instance.Player.TakeDamage(damage * 1.7f, transform.position);
                    attackClock = 1;
                }
                if (phaseClock <= 0) FinishAbility();
            }
        }
        void FinishAbility()
        {
            phase = 0;
            abilityIndex++;
            abilityClock = Kind == EnemyKind.King ? 4 : 6;
        }
        void ShowWarning(float radius)
        {
            if (warning == null)
            {
                warning = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                warning.name = "Boss attack warning";
                warning.transform.SetParent(transform, false);
                Destroy(warning.GetComponent<Collider>());
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                var mat = new Material(shader);
                mat.color = new Color(1,.18f,.12f);
                warning.GetComponent<Renderer>().sharedMaterial = mat;
            }
            warning.transform.localPosition = new Vector3(0, .035f, 0);
            warning.transform.localScale = new Vector3(radius * 2, .014f, radius * 2);
            warning.SetActive(true);
        }
        public void TakeDamage(float amount, Vector3 knockback)
        {
            if (!Alive || amount <= 0) return;
            Health = Mathf.Max(0, Health - amount);
            knockback.y = 0;
            knockVelocity += knockback / (IsBoss ? 4 : Kind == EnemyKind.Tank ? 2 : 1);
            HitEffects.Burst(transform.position + Vector3.up * size, tint, 4);
            if (Health > 0) return;
            Alive = false;
            if (warning != null) warning.SetActive(false);
            HitEffects.Burst(transform.position + Vector3.up, tint, IsBoss ? 30 : 10);
            int xp = Kind == EnemyKind.King ? 250 : Kind == EnemyKind.MiniBoss ? 75 : Kind == EnemyKind.Tank ? 14 : Kind == EnemyKind.Fast ? 7 : 9;
            GameManager.Instance.Reward(xp, 1);
            if (IsBoss) GameManager.Instance.Notify(Kind == EnemyKind.King ? "KING TRALALA DEFEATED! +250 XP" : "MINI-BOSS DEFEATED! +75 XP", 4);
            owner.Despawn(this);
        }
        void OnDestroy()
        {
            if (warning != null)
            {
                var renderer = warning.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null) Destroy(renderer.sharedMaterial);
            }
        }
    }
}
