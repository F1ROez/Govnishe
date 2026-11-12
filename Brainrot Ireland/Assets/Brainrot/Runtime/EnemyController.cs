using UnityEngine;

namespace Brainrot
{
    public enum EnemyAction { Chasing, MeleeWindup, ChargeWindup, Charging, Recovery, Staggered, BossWindup, BossWave }

    /// <summary>Low-cost, committed attacks: a tell, one impact, then an escape window.</summary>
    public sealed class EnemyController : MonoBehaviour
    {
        public bool Alive { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsBoss => Kind == EnemyKind.King || Kind == EnemyKind.MiniBoss;
        public EnemyKind Kind { get; private set; }
        public EnemyAction Action { get; private set; }
        public bool IsPreparingAttack => Action == EnemyAction.MeleeWindup || Action == EnemyAction.ChargeWindup || Action == EnemyAction.BossWindup;

        EnemySpawner owner;
        CharacterVisual visual;
        EnemyTelegraph telegraph;
        EnemyTuning tuning;
        float speed, damage, stateClock, attackClock, chargeClock, abilityClock, size, difficulty, waveRadius;
        int abilityIndex;
        bool attackHit;
        Vector3 knockVelocity, committedDirection, targetArea;
        Color tint;
        float Radius => Kind == EnemyKind.King ? 2.1f : Kind == EnemyKind.MiniBoss ? 1.4f : Kind == EnemyKind.Fast ? .6f : .7f;
        float AttackRange => Mathf.Max(Radius + .5f, tuning.AttackRange);

        public void Initialize(EnemySpawner owner, EnemyKind kind, float difficulty, Vector3 position)
        {
            this.owner = owner;
            Kind = kind;
            this.difficulty = Mathf.Max(1, difficulty);
            tuning = GameBalance.Current.Enemy(kind);
            size = kind == EnemyKind.King ? 3f : kind == EnemyKind.MiniBoss ? 1.9f : kind == EnemyKind.Tank ? 1.45f : kind == EnemyKind.Fast ? .8f : 1f;
            tint = kind == EnemyKind.King ? new Color(.52f,.24f,.84f) : kind == EnemyKind.MiniBoss ? new Color(.94f,.34f,.3f) : kind == EnemyKind.Tank ? new Color(.22f,.42f,.6f) : kind == EnemyKind.Fast ? new Color(.15f,.88f,.77f) : new Color(.23f,.65f,.95f);
            MaxHealth = Health = Mathf.Max(1, tuning.Health) * this.difficulty;
            speed = Mathf.Max(.1f, tuning.Speed) * Mathf.Min(1.5f, 1 + (this.difficulty - 1) * .07f);
            damage = Mathf.Max(0, tuning.Damage) * Mathf.Sqrt(this.difficulty);
            if (visual == null) visual = CharacterVisual.Create(transform, true, tint, size);
            if (telegraph == null) telegraph = new EnemyTelegraph(transform);
            transform.position = position;
            transform.rotation = Quaternion.identity;
            committedDirection = Vector3.forward;
            targetArea = knockVelocity = Vector3.zero;
            stateClock = waveRadius = 0;
            attackClock = .6f;
            chargeClock = .8f;
            abilityClock = IsBoss ? 3 : float.PositiveInfinity;
            abilityIndex = 0;
            attackHit = false;
            Action = EnemyAction.Chasing;
            Alive = true;
            telegraph.Hide();
            visual.ResetFeedback();
        }

        public void Tick(float dt)
        {
            var game = GameManager.Instance;
            if (!Alive || game == null || !game.IsPlaying || game.Player == null || game.Player.Health <= 0 || dt <= 0) return;
            attackClock -= dt;
            chargeClock -= dt;
            abilityClock -= dt;
            // Knockback is a short, damped displacement; it never creates per-enemy rigidbodies.
            transform.position += knockVelocity * dt;
            knockVelocity *= Mathf.Exp(-10 * dt);
            if (knockVelocity.sqrMagnitude < .001f) knockVelocity = Vector3.zero;

            Vector3 delta = Flat(game.Player.transform.position - transform.position);
            float distance = delta.magnitude;
            Vector3 direction = distance > .001f ? delta / distance : transform.forward;
            switch (Action)
            {
                case EnemyAction.MeleeWindup:
                    stateClock -= dt;
                    visual.Animate(0, true);
                    if (stateClock <= 0)
                    {
                        // Range and facing are checked at impact: stepping aside or backwards avoids it.
                        if (distance <= AttackRange && Vector3.Dot(committedDirection, direction) > .3f)
                            game.Player.TakeDamage(damage, transform.position);
                        EnterRecovery(tuning.Recovery);
                    }
                    return;
                case EnemyAction.ChargeWindup:
                    stateClock -= dt;
                    visual.Animate(0, true);
                    if (stateClock <= 0) BeginCharge();
                    return;
                case EnemyAction.Charging:
                    TickCharge(dt, game.Player.transform.position);
                    return;
                case EnemyAction.BossWindup:
                    stateClock -= dt;
                    visual.Animate(0, true);
                    if (stateClock <= 0) ResolveBossAbility();
                    return;
                case EnemyAction.BossWave:
                    TickBossWave(dt, distance);
                    return;
                case EnemyAction.Recovery:
                case EnemyAction.Staggered:
                    stateClock -= dt;
                    visual.Animate(0, false);
                    if (stateClock <= 0) Action = EnemyAction.Chasing;
                    return;
            }
            if (IsBoss && abilityClock <= 0) { PrepareBossAbility(direction); return; }
            if (!IsBoss && tuning.CanCharge && chargeClock <= 0 && distance >= Mathf.Max(1, tuning.ChargeMinDistance) && distance <= Mathf.Max(tuning.ChargeMinDistance, tuning.ChargeMaxDistance))
            {
                committedDirection = direction;
                transform.rotation = Quaternion.LookRotation(direction);
                Action = EnemyAction.ChargeWindup;
                stateClock = Mathf.Max(.15f, tuning.ChargeWindup);
                telegraph.Charge(transform.position, direction, Mathf.Max(1, tuning.ChargeSpeed * tuning.ChargeDuration), Radius + .5f);
                visual.Animate(0, true);
                return;
            }
            if (distance <= AttackRange && attackClock <= 0)
            {
                committedDirection = direction;
                transform.rotation = Quaternion.LookRotation(direction);
                Action = EnemyAction.MeleeWindup;
                stateClock = Mathf.Max(.1f, tuning.MeleeWindup);
                telegraph.Cone(transform.position, direction, AttackRange);
                visual.Animate(0, true);
                return;
            }
            float stop = Mathf.Max(Radius + .4f, AttackRange * .8f);
            Vector3 move = distance > stop ? direction * speed : Vector3.zero;
            if (distance > 3) move += Vector3.Cross(direction, Vector3.up) * Mathf.Sin(GetInstanceID() * .71f + game.Elapsed) * .22f;
            transform.position += move * dt;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), dt * 10);
            visual.Animate(move.magnitude, false);
        }

        void BeginCharge()
        {
            Action = EnemyAction.Charging;
            stateClock = Mathf.Max(.1f, tuning.ChargeDuration);
            chargeClock = Mathf.Max(.5f, tuning.ChargeCooldown);
            attackHit = false;
            telegraph.Hide();
        }
        void TickCharge(float dt, Vector3 playerPosition)
        {
            float step = Mathf.Min(dt, Mathf.Max(0, stateClock));
            Vector3 start = transform.position;
            Vector3 next = start + committedDirection * (Mathf.Max(1, tuning.ChargeSpeed) * step);
            Vector2 flat = Vector2.ClampMagnitude(new Vector2(next.x, next.z), 30);
            next.x = flat.x; next.z = flat.y;
            transform.position = next;
            // Segment test prevents a fast dash skipping over the player at a low frame rate.
            if (!attackHit && SweptHitXZ(start, next, playerPosition, Radius + .5f))
            {
                attackHit = true;
                GameManager.Instance.Player.TakeDamage(damage * (IsBoss ? 1.7f : 1.25f), transform.position);
            }
            stateClock -= dt;
            visual.Animate(6, true);
            if (stateClock <= 0)
            {
                if (IsBoss) FinishBossAbility();
                else EnterRecovery(tuning.Recovery);
            }
        }
        public static bool SweptHitXZ(Vector3 start, Vector3 end, Vector3 target, float radius)
        {
            Vector3 segment = Flat(end - start);
            Vector3 offset = Flat(target - start);
            float t = segment.sqrMagnitude > .000001f ? Mathf.Clamp01(Vector3.Dot(offset, segment) / segment.sqrMagnitude) : 0;
            return (offset - segment * t).sqrMagnitude <= Mathf.Max(0, radius) * Mathf.Max(0, radius);
        }
        static Vector3 Flat(Vector3 value) { value.y = 0; return value; }
        void EnterRecovery(float duration)
        {
            telegraph.Hide();
            Action = EnemyAction.Recovery;
            stateClock = Mathf.Max(.1f, duration);
        }

        void PrepareBossAbility(Vector3 direction)
        {
            committedDirection = direction;
            transform.rotation = Quaternion.LookRotation(direction);
            targetArea = GameManager.Instance.Player.transform.position;
            Action = EnemyAction.BossWindup;
            stateClock = Mathf.Max(.5f, tuning.ChargeWindup);
            int ability = abilityIndex % 4;
            if (ability == 0) telegraph.Charge(transform.position, direction, Mathf.Max(1, tuning.ChargeSpeed * tuning.ChargeDuration), Radius + .5f);
            else if (ability == 3) telegraph.Circle(targetArea, 6.5f);
            else telegraph.Circle(transform.position, ability == 1 ? 10 : 3);
        }
        void ResolveBossAbility()
        {
            int ability = abilityIndex % 4;
            telegraph.Hide();
            if (ability == 0) { BeginCharge(); return; }
            if (ability == 1)
            {
                Action = EnemyAction.BossWave;
                stateClock = 1.4f;
                waveRadius = 1;
                attackHit = false;
                telegraph.Circle(transform.position, waveRadius);
                return;
            }
            if (ability == 2)
            {
                int count = Kind == EnemyKind.King ? 6 : 3;
                for (int i = 0; i < count; i++)
                {
                    float a = i * Mathf.PI * 2 / count;
                    owner.Spawn(EnemyKind.Normal, transform.position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 3, difficulty);
                }
                HitEffects.Burst(transform.position + Vector3.up * 2, tint, 18);
            }
            else
            {
                HitEffects.Burst(targetArea + Vector3.up, new Color(1,.42f,.12f), 24);
                if (Flat(targetArea - GameManager.Instance.Player.transform.position).sqrMagnitude <= 6.5f * 6.5f)
                    GameManager.Instance.Player.TakeDamage(damage * 1.4f, targetArea);
            }
            FinishBossAbility();
        }
        void TickBossWave(float dt, float distance)
        {
            stateClock -= dt;
            float previousRadius = waveRadius;
            waveRadius = Mathf.Lerp(1, 10, Mathf.Clamp01(1 - stateClock / 1.4f));
            telegraph.Circle(transform.position, waveRadius);
            if (!attackHit && distance >= previousRadius - .6f && distance <= waveRadius + .6f)
            {
                attackHit = true;
                GameManager.Instance.Player.TakeDamage(damage, transform.position);
            }
            visual.Animate(0, true);
            if (stateClock <= 0) FinishBossAbility();
        }
        void FinishBossAbility()
        {
            abilityIndex++;
            abilityClock = Kind == EnemyKind.King ? 4 : 6;
            EnterRecovery(Mathf.Max(.5f, tuning.Recovery));
        }

        public void TakeDamage(float amount, Vector3 knockback)
        {
            if (!Alive || amount <= 0) return;
            Health = Mathf.Max(0, Health - amount);
            knockback.y = 0;
            knockVelocity = Vector3.ClampMagnitude(knockVelocity + knockback / Mathf.Max(.2f, tuning.KnockbackResistance), 7);
            var feedback = GameBalance.Current.Combat;
            visual.FlashHit(Mathf.Max(0, feedback.HitFlashDuration), Mathf.Clamp01(feedback.HitFlashStrength));
            if (!IsBoss && Action != EnemyAction.Charging)
            {
                // A hit cancels a pending normal attack but bosses retain their commitments.
                telegraph.Hide();
                Action = EnemyAction.Staggered;
                stateClock = Mathf.Max(.02f, tuning.HitStagger);
                attackClock = .2f;
            }
            if (Health > 0) return;
            Alive = false;
            telegraph.Hide();
            HitEffects.Burst(transform.position + Vector3.up, tint, IsBoss ? 24 : 8);
            int xp = Mathf.Max(0, tuning.Experience);
            GameManager.Instance.Reward(xp, 1);
            if (IsBoss) GameManager.Instance.Notify((Kind == EnemyKind.King ? "KING TRALALA DEFEATED! +" : "MINI-BOSS DEFEATED! +") + xp + " XP", 4);
            owner.Despawn(this);
        }
        void OnDisable() { if (telegraph != null) telegraph.Hide(); if (visual != null) visual.ResetFeedback(); }
    }

    /// <summary>Reused outline tells; no filled discs obscure the fight.</summary>
    sealed class EnemyTelegraph
    {
        static Material material;
        readonly LineRenderer line;
        readonly Vector3[] points = new Vector3[35];
        public EnemyTelegraph(Transform owner)
        {
            var go = new GameObject("Attack telegraph");
            go.transform.SetParent(owner, false);
            line = go.AddComponent<LineRenderer>();
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                material = new Material(shader) { name = "Enemy attack outline", color = new Color(1,.36f,.08f) };
            }
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.widthMultiplier = .07f;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            Hide();
        }
        public void Hide() { line.enabled = false; }
        void Show(int count)
        {
            line.enabled = true;
            line.positionCount = count;
            for (int i = 0; i < count; i++) line.SetPosition(i, points[i]);
        }
        static Vector3 Ground(Vector3 point) { point.y = .075f; return point; }
        public void Circle(Vector3 origin, float radius)
        {
            origin = Ground(origin);
            for (int i = 0; i < 33; i++)
            {
                float a = i * Mathf.PI * 2 / 32;
                points[i] = origin + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * radius;
            }
            Show(33);
        }
        public void Cone(Vector3 origin, Vector3 direction, float radius)
        {
            origin = Ground(origin);
            points[0] = origin;
            for (int i = 0; i <= 24; i++) points[i + 1] = origin + Quaternion.Euler(0, Mathf.Lerp(-72, 72, i / 24f), 0) * direction * radius;
            points[26] = origin;
            Show(27);
        }
        public void Charge(Vector3 origin, Vector3 direction, float length, float width)
        {
            origin = Ground(origin);
            Vector3 side = Vector3.Cross(Vector3.up, direction) * width;
            Vector3 end = origin + direction * length;
            points[0] = origin - side; points[1] = end - side; points[2] = end + direction * .5f;
            points[3] = end + side; points[4] = origin + side; points[5] = origin - side;
            Show(6);
        }
    }
}
