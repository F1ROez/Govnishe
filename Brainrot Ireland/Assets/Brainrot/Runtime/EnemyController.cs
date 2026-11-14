using UnityEngine;

namespace Brainrot
{
    public enum EnemyAction
    {
        Chasing = 0, MeleeWindup = 1, ChargeWindup = 2, Charging = 3, Recovery = 4, Staggered = 5,
        BossWindup = 6, BossWave = 7, Idle = 8, Wandering = 9, Biting = 10
    }

    /// <summary>Local wildlife: idle, wander, notice a nearby player, attack, then lose interest.</summary>
    public sealed class EnemyController : MonoBehaviour
    {
        public bool Alive { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsBoss => false;
        public EnemyKind Kind { get; private set; }
        public EnemyAction Action { get; private set; }
        public IslandZone HomeZone { get; private set; }
        public Vector3 HomePosition { get; private set; }
        public bool IsAlerted { get; private set; }
        public bool IsPreparingAttack => Action == EnemyAction.MeleeWindup || Action == EnemyAction.ChargeWindup;

        public EnemyPhysicsBody PhysicsBody {get;private set;}
        EnemySpawner owner;OctopusAttack octopus;
        CharacterVisual visual;
        EnemyTelegraph telegraph;
        EnemyTuning tuning;
        float speed, damage, stateClock, attackClock, chargeClock;
        bool attackHit;
        Vector3 knockVelocity, committedDirection, wanderTarget;
        float Radius => Kind == EnemyKind.LiriliLarila ? 1.15f : Kind == EnemyKind.BrrBrrPatapim ? 1.05f : Kind == EnemyKind.BombardiroCrocodilo ? .95f : Kind == EnemyKind.CappuccinoAssassino ? .55f : .7f;
        float AttackRange => Mathf.Max(Radius + .5f, tuning.AttackRange);

        public void Initialize(EnemySpawner owner, EnemyKind kind, float difficulty, Vector3 position)
        {
            this.owner = owner; Kind = kind;
            if(PhysicsBody==null)PhysicsBody=gameObject.AddComponent<EnemyPhysicsBody>();PhysicsBody.ResetBody();
            tuning = GameBalance.Current.Enemy(kind);
            HomeZone = IslandMap.GetZone(position);
            HomePosition = position;
            HomePosition = new Vector3(HomePosition.x, 0, HomePosition.z);
            PhysicsBody.PrepareSpawn(HomePosition,Quaternion.Euler(0, Random.Range(0, 360), 0));
            MaxHealth = Health = Mathf.Max(1, tuning.Health) * Mathf.Max(1, difficulty);
            speed = Mathf.Max(.1f, tuning.Speed);
            damage = Mathf.Max(0, tuning.Damage) * Mathf.Sqrt(Mathf.Max(1, difficulty));
            if (visual == null) visual = CharacterVisual.CreateEnemy(transform, kind, Skin(kind), Size(kind));
            visual.transform.localScale=Vector3.one*Size(kind)*Mathf.Max(.1f,GameBalance.Current.EnemyModelScale)*Mathf.Max(.1f,tuning.ModelScale);
            if (telegraph == null) telegraph = new EnemyTelegraph(transform);
            committedDirection = transform.forward;
            knockVelocity = Vector3.zero;
            wanderTarget = HomePosition;
            attackClock = .6f; chargeClock = .8f;
            attackHit = IsAlerted = false;
            Alive = true;
            EnterIdle();
            visual.ResetFeedback();visual.ResetBite();
            if(kind==EnemyKind.BlueberrinniOctopusini){if(octopus==null)octopus=gameObject.AddComponent<OctopusAttack>();octopus.Initialize(this,owner);}
        }
        static Color Skin(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.LiriliLarila: return new Color(.48f,.70f,.43f);
                case EnemyKind.BrrBrrPatapim: return new Color(.45f,.29f,.13f);
                case EnemyKind.CappuccinoAssassino: return new Color(.87f,.80f,.65f);
                case EnemyKind.BombardiroCrocodilo: return new Color(.29f,.49f,.24f);
                default: return new Color(.23f,.65f,.95f);
            }
        }
        void LateUpdate(){if(visual!=null&&tuning!=null)visual.transform.localScale=Vector3.one*Size(Kind)*Mathf.Max(.1f,GameBalance.Current.EnemyModelScale)*Mathf.Max(.1f,tuning.ModelScale);}
        static float Size(EnemyKind kind)
        {
            return kind == EnemyKind.LiriliLarila ? 1.25f : kind == EnemyKind.BrrBrrPatapim ? 1.45f : kind == EnemyKind.BombardiroCrocodilo ? 1.25f : kind == EnemyKind.CappuccinoAssassino ? .95f : 1;
        }

        public void Tick(float dt)
        {
            var game = GameManager.Instance;
            if (!Alive || game == null || !game.IsPlaying || game.Player == null || game.Player.Health <= 0 || dt <= 0) return;
            if(PhysicsBody!=null&&PhysicsBody.IsPhysical)return;
            if(Kind==EnemyKind.BlueberrinniOctopusini){octopus.Tick(dt);return;}
            attackClock -= dt; chargeClock -= dt;
            // Integrate damped knockback analytically so the distance is stable at low frame rates.
            float decay = Mathf.Exp(-6 * dt);
            MoveWithinZone(knockVelocity * ((1 - decay) / 6));
            knockVelocity *= decay;
            if (knockVelocity.sqrMagnitude < .001f) knockVelocity = Vector3.zero;

            Vector3 playerPosition = game.Player.transform.position;
            Vector3 delta = Flat(playerPosition - transform.position);
            float distance = delta.magnitude;
            var settings = GameBalance.Current.Exploration;
            IsAlerted=true;
            if(Action==EnemyAction.Idle||Action==EnemyAction.Wandering)Action=EnemyAction.Chasing;

            Vector3 direction = distance > .001f ? delta / distance : transform.forward;
            switch (Action)
            {
                case EnemyAction.MeleeWindup:
                    stateClock-=dt;
                    if(distance>Radius+.6f)MoveWithinZone(direction*speed*dt);
                    Face(direction,dt);committedDirection=direction;
                    telegraph.Cone(transform.position,direction,AttackRange);visual.Animate(speed,true);
                    if(stateClock<=0){
                        visual.PlayBite(.32f);Action=EnemyAction.Biting;stateClock=.32f;attackHit=false;
                    }
                    return;
                case EnemyAction.Biting:
                    stateClock-=dt;visual.Animate(speed,true);
                    if(distance>Radius+.6f)MoveWithinZone(direction*speed*dt);
                    if(!attackHit&&stateClock<=.2f){attackHit=true;
                        Vector3 impact=Flat(game.Player.transform.position-transform.position);
                        if(impact.magnitude<=AttackRange&&Vector3.Dot(transform.forward,impact.normalized)>.3f)game.Player.TakeDamage(damage,transform.position);
                    }
                    if(stateClock<=0)EnterRecovery(tuning.Recovery);
                    return;
                case EnemyAction.ChargeWindup:
                    stateClock -= dt; visual.Animate(0, true);
                    if (stateClock <= 0) BeginCharge();
                    return;
                case EnemyAction.Charging:
                    TickCharge(dt, playerPosition);
                    return;
                case EnemyAction.Recovery:
                    if(distance>Radius+.6f)MoveWithinZone(direction*speed*dt);Face(direction,dt);visual.Animate(speed,false);
                    stateClock-=dt;if(stateClock<=0)Action=EnemyAction.Chasing;return;
                case EnemyAction.Staggered:
                    stateClock -= dt; visual.Animate(0, false);
                    if (stateClock <= 0) Action = EnemyAction.Chasing;
                    return;
            }
            if (tuning.CanCharge && chargeClock <= 0 && distance >= Mathf.Max(1, tuning.ChargeMinDistance) && distance <= Mathf.Max(tuning.ChargeMinDistance, tuning.ChargeMaxDistance))
            {
                committedDirection = direction;
                transform.rotation = Quaternion.LookRotation(direction);
                Action = EnemyAction.ChargeWindup;
                stateClock = Mathf.Max(.25f, tuning.ChargeWindup);
                telegraph.Charge(transform.position, direction, Mathf.Max(1, tuning.ChargeSpeed * tuning.ChargeDuration), Radius + .5f, HomeZone);
                visual.Animate(0, true);
                return;
            }
            if (distance <= AttackRange && attackClock <= 0)
            {
                committedDirection = direction;
                transform.rotation = Quaternion.LookRotation(direction);
                Action = EnemyAction.MeleeWindup;
                stateClock = Mathf.Max(.15f, tuning.MeleeWindup);
                telegraph.Cone(transform.position, direction, AttackRange);
                visual.Animate(0, true);
                return;
            }
            float stop = Mathf.Max(Radius + .4f, AttackRange * .8f);
            Vector3 move = distance > stop ? direction * speed : Vector3.zero;
            if (distance > 3) move += Vector3.Cross(direction, Vector3.up) * Mathf.Sin(GetInstanceID() * .71f + game.Elapsed) * .18f;
            MoveWithinZone(move * dt);
            Face(direction, dt);
            visual.Animate(move.magnitude, false);
        }

        void TickAmbient(float dt)
        {
            if (Action == EnemyAction.Staggered)
            {
                stateClock -= dt;
                if (stateClock <= 0) EnterIdle();
                visual.Animate(0, false);return;
            }
            if (Action != EnemyAction.Idle && Action != EnemyAction.Wandering) EnterIdle();
            stateClock -= dt;
            if (Action == EnemyAction.Idle)
            {
                visual.Animate(0, false);
                if (stateClock > 0) return;
                if (Random.value < .35f) { EnterIdle(); return; }
                var settings = GameBalance.Current.Exploration;
                Vector2 offset = Random.insideUnitCircle * Mathf.Max(0, settings.WanderRadius);
                wanderTarget = IslandMap.ClampToZone(HomePosition + new Vector3(offset.x, 0, offset.y), HomeZone);
                Action = EnemyAction.Wandering; stateClock = Random.Range(3, 7);
            }
            Vector3 delta = Flat(wanderTarget - transform.position);
            if (delta.sqrMagnitude <= .12f || stateClock <= 0) { EnterIdle(); visual.Animate(0, false); return; }
            float wanderSpeed = Mathf.Min(speed * .5f, Mathf.Max(0, GameBalance.Current.Exploration.WanderSpeed));
            Vector3 step = delta.normalized * Mathf.Min(delta.magnitude, wanderSpeed * dt);
            MoveWithinZone(step);Face(delta.normalized, dt);
            visual.Animate(wanderSpeed, false);
        }
        void EnterIdle()
        {
            telegraph.Hide();
            Action = EnemyAction.Idle;
            var settings = GameBalance.Current.Exploration;
            float min = Mathf.Max(.2f, settings.IdleMin);
            stateClock = Random.Range(min, Mathf.Max(min, settings.IdleMax));
        }
        void LoseInterest()
        {
            IsAlerted = false;attackHit = false;
            attackClock = .6f;chargeClock = Mathf.Max(chargeClock, .8f);
            EnterIdle();
        }
        void MoveWithinZone(Vector3 displacement)
        {
            Vector3 next = transform.position + displacement;
            next.y = 0;transform.position = next;
        }
        void Face(Vector3 direction, float dt)
        {
            if (direction.sqrMagnitude > .001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Mathf.Clamp01(dt * 8));
        }
        void BeginCharge()
        {
            Action = EnemyAction.Charging;
            stateClock = Mathf.Max(.1f, tuning.ChargeDuration);
            chargeClock = Mathf.Max(.5f, tuning.ChargeCooldown);
            attackHit = false;telegraph.Hide();
        }
        void TickCharge(float dt, Vector3 playerPosition)
        {
            float step = Mathf.Min(dt, Mathf.Max(0, stateClock));
            Vector3 start = transform.position;
            MoveWithinZone(committedDirection * (Mathf.Max(1, tuning.ChargeSpeed) * step));
            if (!attackHit && SweptHitXZ(start, transform.position, playerPosition, Radius + .5f))
            {
                attackHit = true;
                GameManager.Instance.Player.TakeDamage(damage * 1.25f, transform.position);
            }
            stateClock -= dt;visual.Animate(6, true);
            if (stateClock <= 0) EnterRecovery(tuning.Recovery);
        }
        public static bool SweptHitXZ(Vector3 start, Vector3 end, Vector3 target, float radius)
        {
            Vector3 segment = Flat(end - start), offset = Flat(target - start);
            float t = segment.sqrMagnitude > .000001f ? Mathf.Clamp01(Vector3.Dot(offset, segment) / segment.sqrMagnitude) : 0;
            return (offset - segment * t).sqrMagnitude <= Mathf.Max(0, radius) * Mathf.Max(0, radius);
        }
        static Vector3 Flat(Vector3 value) { value.y = 0; return value; }
        void EnterRecovery(float duration)
        {
            telegraph.Hide();Action = EnemyAction.Recovery;stateClock = Mathf.Max(.1f, duration);
        }

        public void TakeDamage(float amount, Vector3 knockback)
        {
            if (!Alive || amount <= 0) return;
            if(octopus!=null&&(knockback.sqrMagnitude>.01f||amount>=Health))octopus.Interrupt();
            Health = Mathf.Max(0, Health - amount);knockback.y = 0;
            var feedback = GameBalance.Current.Combat;
            visual.FlashHit(Mathf.Max(0, feedback.HitFlashDuration), Mathf.Clamp01(feedback.HitFlashStrength));
            var game = GameManager.Instance;
            if (game != null && game.Player != null)
            {
                Vector3 target = game.Player.transform.position;
                float lose = Mathf.Max(1, GameBalance.Current.Exploration.LoseInterestRadius);
                float leash = Mathf.Max(2, GameBalance.Current.Exploration.LeashDistance);
                if (IslandMap.GetZone(target) == HomeZone && Flat(target - transform.position).sqrMagnitude <= lose * lose && Flat(target - HomePosition).sqrMagnitude <= leash * leash) IsAlerted = true;
            }
            if (Action != EnemyAction.Charging)
            {
                telegraph.Hide();visual.ResetBite();Action = EnemyAction.Staggered;
                stateClock = Mathf.Max(.02f, tuning.HitStagger);attackClock = .2f;
            }
            if (Health > 0){if(knockback.sqrMagnitude>.01f)Tumble(knockback);return;}
            Alive = false;IsAlerted = false;telegraph.Hide();
            HitEffects.Burst(transform.position + Vector3.up, Skin(Kind), 8);
            GameManager.Instance.Reward(Mathf.Max(0, tuning.Experience), 1);
            Vector3 launch=knockback.sqrMagnitude>.01f?knockback:(transform.position-GameManager.Instance.Player.transform.position).normalized*3;
            PhysicsBody.Launch(launch,true);owner.Killed(this);
        }
        public void Tumble(Vector3 velocity){if(!Alive)return;if(octopus!=null)octopus.Interrupt();visual.ResetBite();telegraph.Hide();Action=EnemyAction.Staggered;PhysicsBody.Launch(velocity,false);}
        public void OnRecovered(){if(!Alive)return;Action=EnemyAction.Chasing;attackClock=.5f;visual.ResetFeedback();}
        void OnDisable() { IsAlerted = false;if (telegraph != null) telegraph.Hide();if (visual != null) visual.ResetFeedback(); }
    }

    /// <summary>Reusable attack outlines. No opaque warning discs cover the terrain.</summary>
    sealed class EnemyTelegraph
    {
        static Material material;
        readonly LineRenderer line;
        readonly Vector3[] points = new Vector3[35];
        public EnemyTelegraph(Transform owner)
        {
            var go = new GameObject("Предупреждение атаки");go.transform.SetParent(owner, false);line = go.AddComponent<LineRenderer>();
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                material = new Material(shader) { name = "Контур атаки врага", color = new Color(1,.36f,.08f) };
            }
            line.sharedMaterial = material;line.useWorldSpace = true;line.widthMultiplier = .07f;line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows = false;Hide();
        }
        public void Hide() { line.enabled = false; }
        void Show(int count)
        {
            line.enabled = true;line.positionCount = count;
            for (int i = 0; i < count; i++) line.SetPosition(i, points[i]);
        }
        static Vector3 Ground(Vector3 point) { point.y = .075f;return point; }
        public void Cone(Vector3 origin, Vector3 direction, float radius)
        {
            origin = Ground(origin);points[0] = origin;
            for (int i = 0; i <= 24; i++) points[i + 1] = origin + Quaternion.Euler(0, Mathf.Lerp(-72, 72, i / 24f), 0) * direction * radius;
            points[26] = origin;Show(27);
        }
        public void Charge(Vector3 origin, Vector3 direction, float length, float width, IslandZone zone)
        {
            origin = Ground(origin);Vector3 side = Vector3.Cross(Vector3.up, direction) * width;
            Vector3 end = Ground(IslandMap.ClampToZone(origin + direction * length, zone));
            points[0] = origin - side;points[1] = end - side;points[2] = end + direction * .5f;
            points[3] = end + side;points[4] = origin + side;points[5] = origin - side;Show(6);
        }
    }
}
