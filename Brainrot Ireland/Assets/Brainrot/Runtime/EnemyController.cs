using UnityEngine;

namespace Brainrot
{
    public enum EnemyAction
    {
        Chasing = 0, MeleeWindup = 1, ChargeWindup = 2, Charging = 3, Recovery = 4, Staggered = 5,
        BossWindup = 6, BossWave = 7, Idle = 8, Wandering = 9, Biting = 10, SlamWindup = 11, SlamImpact = 12
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
        public bool IsPreparingAttack => Action == EnemyAction.MeleeWindup || Action == EnemyAction.ChargeWindup || Action == EnemyAction.SlamWindup;

        public EnemyPhysicsBody PhysicsBody {get;private set;}
        EnemySpawner owner;OctopusAttack octopus;OrcaladonAttack orcaladon;
        CharacterVisual visual;
        EnemyTelegraph telegraph;
        EnemyTuning tuning;
        float speed, damage, stateClock, attackClock, chargeClock, restoredGrace;
        bool attackHit;
        Vector3 knockVelocity, committedDirection, wanderTarget;
        float Radius => Kind == EnemyKind.Orcaladon ? Mathf.Max(.1f,GameBalance.Current.Orcaladon.CollisionRadius) : Kind == EnemyKind.LiriliLarila ? 1.15f : Kind == EnemyKind.BrrBrrPatapim ? 1.05f : Kind == EnemyKind.BombardiroCrocodilo ? .95f : Kind == EnemyKind.CappuccinoAssassino ? .55f : .7f;
        public float CrowdRadius=>Mathf.Clamp(Radius*Mathf.Max(.1f,GameBalance.Current.EnemyModelScale)*Mathf.Max(.1f,tuning!=null?tuning.ModelScale:1),.2f,3);
        public bool CrowdParticipant=>Alive&&gameObject.activeInHierarchy&&(PhysicsBody==null||!PhysicsBody.IsPhysical)&&
            (octopus==null||octopus.Phase==OctopusPhase.Approach||octopus.Phase==OctopusPhase.Aiming);
        public bool CrowdMovable=>CrowdParticipant&&(octopus==null||octopus.Phase==OctopusPhase.Approach)&&
            (orcaladon==null||orcaladon.Phase==OrcaladonPhase.Approach||orcaladon.Phase==OrcaladonPhase.Recovery)&&
            Action!=EnemyAction.Charging&&Action!=EnemyAction.ChargeWindup;
        public void ApplyCrowdCorrection(Vector3 correction){
            if(!CrowdMovable)return;correction.y=0;MoveWithinZone(correction);
            if(Action==EnemyAction.MeleeWindup)ShowMeleeWarning();
        }
        void ShowMeleeWarning(){
            if(owner==null||owner.IsMeleeWarningVisible(this))telegraph.Cone(transform.position,committedDirection,AttackRange,1-stateClock/Mathf.Max(.15f,tuning.MeleeWindup));
            else telegraph.Hide();
        }
        float AttackRange => Mathf.Max(Radius + .5f, tuning.AttackRange);

        public void Initialize(EnemySpawner owner, EnemyKind kind, float difficulty, Vector3 position)
        {
            this.owner = owner; Kind = kind;
            if(PhysicsBody==null)PhysicsBody=gameObject.AddComponent<EnemyPhysicsBody>();PhysicsBody.ResetBody();
            var capsule=GetComponent<CapsuleCollider>();if(capsule!=null){capsule.radius=kind==EnemyKind.Orcaladon?Mathf.Max(.1f,GameBalance.Current.Orcaladon.CollisionRadius):.55f;capsule.height=kind==EnemyKind.Orcaladon?Mathf.Max(3.4f,capsule.radius*2):2;capsule.center=Vector3.up*capsule.height*.5f;}
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
            attackClock = .6f; chargeClock = .8f;restoredGrace=0;
            attackHit = IsAlerted = false;
            Alive = true;
            EnterIdle();
            visual.ResetFeedback();visual.ResetBite();
            if(kind==EnemyKind.BlueberrinniOctopusini){if(octopus==null)octopus=gameObject.AddComponent<OctopusAttack>();octopus.Initialize(this,owner);}
            if(kind==EnemyKind.Orcaladon){if(orcaladon==null)orcaladon=gameObject.AddComponent<OrcaladonAttack>();orcaladon.Initialize(this,visual);}
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
            if(restoredGrace>0){restoredGrace=Mathf.Max(0,restoredGrace-dt);visual.Animate(0,false);return;}
            if(Kind==EnemyKind.BlueberrinniOctopusini){octopus.Tick(dt);visual.Animate(octopus.Phase==OctopusPhase.Approach||octopus.Phase==OctopusPhase.Retreat?speed:0,false);return;}
            if(Kind==EnemyKind.Orcaladon){
                ApplyKnockback(dt);
                orcaladon.Tick(dt);IsAlerted=true;
                switch(orcaladon.Phase){
                    case OrcaladonPhase.ChargeAim:Action=EnemyAction.ChargeWindup;break;
                    case OrcaladonPhase.Charging:Action=EnemyAction.Charging;break;
                    case OrcaladonPhase.SlamWindup:Action=EnemyAction.SlamWindup;break;
                    case OrcaladonPhase.SlamImpact:Action=EnemyAction.SlamImpact;break;
                    case OrcaladonPhase.Recovery:Action=EnemyAction.Recovery;break;
                    default:Action=EnemyAction.Chasing;break;
                }return;
            }
            attackClock -= dt; chargeClock -= dt;
            ApplyKnockback(dt);

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
                    float windupSpeed=FollowPlayer(direction,distance,dt);
                    Face(direction,dt);committedDirection=direction;
                    ShowMeleeWarning();visual.Animate(windupSpeed,true);
                    if(stateClock<=0){
                        telegraph.Hide();visual.PlayBite(.32f);Action=EnemyAction.Biting;stateClock=.32f;attackHit=false;
                    }
                    return;
                case EnemyAction.Biting:
                    stateClock-=dt;
                    float biteSpeed=FollowPlayer(direction,distance,dt);visual.Animate(biteSpeed,true);
                    if(!attackHit&&stateClock<=.2f){attackHit=true;
                        Vector3 impact=Flat(game.Player.transform.position-transform.position);
                        if(impact.sqrMagnitude<=AttackRange*AttackRange&&(impact.sqrMagnitude<.000001f||Vector3.Dot(transform.forward,impact.normalized)>.3f))game.Player.TakeDamage(damage,transform.position);
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
                    float recoverySpeed=FollowPlayer(direction,distance,dt);Face(direction,dt);visual.Animate(recoverySpeed,false);
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
                telegraph.ChargeStraight(transform.position, direction, Mathf.Max(1, tuning.ChargeSpeed * tuning.ChargeDuration), Radius + .5f);
                visual.Animate(0, true);
                return;
            }
            if (distance <= AttackRange && attackClock <= 0 && (owner==null||owner.CanBeginMeleeAttack(this)))
            {
                committedDirection = direction;
                transform.rotation = Quaternion.LookRotation(direction);
                Action = EnemyAction.MeleeWindup;
                stateClock = Mathf.Max(.15f, tuning.MeleeWindup);
                ShowMeleeWarning();
                visual.Animate(0, true);
                return;
            }
            float stop = Mathf.Max(Radius + .4f, AttackRange * .8f);
            Vector3 step = direction * Mathf.Min(speed*dt,Mathf.Max(0,distance-stop));
            if (distance > 3) step += Vector3.Cross(direction, Vector3.up) * Mathf.Sin(GetInstanceID() * .71f + game.Elapsed) * (.18f*dt);
            MoveWithinZone(step);
            Face(direction, dt);
            visual.Animate(step.magnitude/dt, false);
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
            if(displacement.sqrMagnitude<=.0000000001f)return;
            Vector3 next = transform.position + displacement;
            next.y = 0;transform.position = next;
        }
        float FollowPlayer(Vector3 direction,float distance,float dt)
        {
            float step=Mathf.Min(speed*dt,Mathf.Max(0,distance-Radius-.6f));MoveWithinZone(direction*step);return step/dt;
        }
        void ApplyKnockback(float dt)
        {
            if(knockVelocity.sqrMagnitude<.001f){knockVelocity=Vector3.zero;return;}
            // Integrate damped knockback analytically; avoid touching Transform for every unpushed enemy.
            float decay=Mathf.Exp(-6*dt);MoveWithinZone(knockVelocity*((1-decay)/6));knockVelocity*=decay;
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

        public EnemyRunSnapshot CaptureRunState(){
            return new EnemyRunSnapshot{Kind=(int)Kind,Position=transform.position,RotationYaw=transform.eulerAngles.y,Health=Health,MaxHealth=MaxHealth};
        }
        public void RestoreRunState(EnemyRunSnapshot snapshot){
            if(snapshot==null||!Alive)return;
            var position=snapshot.Position;position.y=0;
            if(float.IsNaN(position.x)||float.IsInfinity(position.x)||float.IsNaN(position.z)||float.IsInfinity(position.z)||Mathf.Abs(position.x)>100||Mathf.Abs(position.z)>100)return;
            float yaw=float.IsNaN(snapshot.RotationYaw)||float.IsInfinity(snapshot.RotationYaw)?0:snapshot.RotationYaw;
            PhysicsBody.PrepareSpawn(position,Quaternion.Euler(0,yaw,0));
            MaxHealth=float.IsNaN(snapshot.MaxHealth)||float.IsInfinity(snapshot.MaxHealth)?Mathf.Max(1,tuning.Health):Mathf.Clamp(snapshot.MaxHealth,1,1000000);
            Health=float.IsNaN(snapshot.Health)||float.IsInfinity(snapshot.Health)?MaxHealth:Mathf.Clamp(snapshot.Health,.001f,MaxHealth);
            if(HomePosition.sqrMagnitude<.01f)HomePosition=EnemySpawner.OceanSpawnPoint(yaw*Mathf.Deg2Rad);
            knockVelocity=Vector3.zero;attackHit=false;restoredGrace=.8f;attackClock=.8f;chargeClock=.8f;
            committedDirection=transform.forward;telegraph.Hide();visual.ResetBite();visual.ResetFeedback();Action=EnemyAction.Chasing;
            if(octopus!=null)octopus.Initialize(this,owner);
            if(orcaladon!=null)orcaladon.ResetAttack();
        }

        public void TakeDamage(float amount, Vector3 knockback)
        {
            if (!Alive || amount <= 0) return;
            if(octopus!=null&&(knockback.sqrMagnitude>.01f||amount>=Health))octopus.Interrupt();
            Health = Mathf.Max(0, Health - amount);knockback.y = 0;
            var feedback = GameBalance.Current.Combat;
            visual.FlashHit(Mathf.Max(0, feedback.HitFlashDuration), Mathf.Clamp01(feedback.HitFlashStrength));
            if(Kind==EnemyKind.Orcaladon&&Health>0){
                if(knockback.sqrMagnitude>.01f){knockVelocity=knockback.normalized*Mathf.Max(0,GameBalance.Current.Orcaladon.HeavyPushSpeed);orcaladon.Interrupt();}
                return;
            }
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
                stateClock = Mathf.Max(.02f, tuning.HitStagger);attackClock = Mathf.Max(attackClock,.35f);
            }
            if (Health > 0){if(knockback.sqrMagnitude>.01f)Tumble(knockback);return;}
            Alive = false;IsAlerted = false;telegraph.Hide();
            if(game!=null&&game.IsPlaying)game.Hearts?.Drop(Kind,transform.position);
            if(orcaladon!=null)orcaladon.ResetAttack();
            HitEffects.Burst(transform.position + Vector3.up, Skin(Kind), 8);
            GameManager.Instance.Reward(Mathf.Max(0, tuning.Experience), 1);
            if(game!=null&&game.IsPlaying)game.Coins?.Drop(Kind,transform.position,game.Kills);
            Vector3 launch=knockback.sqrMagnitude>.01f?knockback:(transform.position-GameManager.Instance.Player.transform.position).normalized*3;
            PhysicsBody.Launch(launch,true);owner.Killed(this);
        }
        public void Tumble(Vector3 velocity){if(!Alive)return;if(Kind==EnemyKind.Orcaladon){if(velocity.sqrMagnitude>.0001f)knockVelocity=velocity.normalized*Mathf.Max(0,GameBalance.Current.Orcaladon.HeavyPushSpeed);orcaladon?.Interrupt();return;}if(octopus!=null)octopus.Interrupt();visual.ResetBite();telegraph.Hide();Action=EnemyAction.Staggered;PhysicsBody.Launch(velocity,false);}
        public void OnRecovered(){if(!Alive)return;Action=EnemyAction.Chasing;attackClock=.5f;visual.ResetFeedback();}
        void OnDisable() { IsAlerted = false;if (telegraph != null) telegraph.Hide();if (orcaladon != null) orcaladon.ResetAttack();if (visual != null) visual.ResetFeedback(); }
    }

    /// <summary>Shared material and pooled geometry: high-contrast borders without opaque ground discs.</summary>
    sealed class EnemyTelegraph
    {
        static Material material;
        static readonly int BaseColor=Shader.PropertyToID("_BaseColor"),ColorId=Shader.PropertyToID("_Color");
        static readonly Vector2[] cone=BuildCone();
        readonly LineRenderer line,border;
        readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
        readonly Vector3[] points=new Vector3[35];
        public EnemyTelegraph(Transform owner)
        {
            if(material==null){
                var shader=Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Unlit/Color");
                material=new Material(shader){name="Читаемые предупреждения атак",color=Color.white,renderQueue=3000};
                if(material.HasProperty("_Surface")){
                    material.SetFloat("_Surface",1);material.SetFloat("_ZWrite",0);
                    material.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);material.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.SetOverrideTag("RenderType","Transparent");
                }
            }
            border=Create(owner,"Тёмная кайма предупреждения",1);line=Create(owner,"Предупреждение атаки",2);Hide();
        }
        static Vector2[] BuildCone(){
            var result=new Vector2[25];float angle=Mathf.Acos(.3f)*Mathf.Rad2Deg;
            for(int i=0;i<result.Length;i++){float a=Mathf.Lerp(-angle,angle,i/24f)*Mathf.Deg2Rad;result[i]=new Vector2(Mathf.Sin(a),Mathf.Cos(a));}
            return result;
        }
        static LineRenderer Create(Transform owner,string title,int order){
            var go=new GameObject(title);go.transform.SetParent(owner,false);var renderer=go.AddComponent<LineRenderer>();
            renderer.sharedMaterial=material;renderer.useWorldSpace=true;renderer.numCapVertices=2;renderer.numCornerVertices=2;renderer.sortingOrder=order;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;return renderer;
        }
        public void Hide(){line.enabled=false;border.enabled=false;}
        void Colorize(LineRenderer renderer,Color color){block.SetColor(BaseColor,color);block.SetColor(ColorId,color);renderer.SetPropertyBlock(block);}
        void Show(int count,float progress=0,bool special=false){
            float width=special?.075f:Mathf.Lerp(.045f,.065f,Mathf.Clamp01(progress));
            line.widthMultiplier=width;border.widthMultiplier=width*2.2f;
            Colorize(border,new Color(.32f,.035f,.015f,.72f));
            Colorize(line,Color.Lerp(new Color(1,.72f,.12f,.95f),new Color(1,.23f,.08f,1),Mathf.Clamp01(progress)));
            line.enabled=border.enabled=true;if(line.positionCount!=count)line.positionCount=count;if(border.positionCount!=count)border.positionCount=count;
            for(int i=0;i<count;i++){line.SetPosition(i,points[i]);border.SetPosition(i,points[i]);}
        }
        static Vector3 Ground(Vector3 point){point.y=.075f;return point;}
        public void Cone(Vector3 origin,Vector3 direction,float radius,float progress=0){
            origin=Ground(origin);direction.y=0;direction=direction.sqrMagnitude>.0001f?direction.normalized:Vector3.forward;
            var side=Vector3.Cross(Vector3.up,direction);points[0]=origin;
            for(int i=0;i<cone.Length;i++)points[i+1]=origin+(side*cone[i].x+direction*cone[i].y)*radius;
            points[26]=origin;Show(27,progress);
        }
        public void ChargeStraight(Vector3 origin,Vector3 direction,float length,float width,float progress=1){
            origin=Ground(origin);Vector3 side=Vector3.Cross(Vector3.up,direction)*width,end=origin+direction*length;
            points[0]=origin-side;points[1]=end-side;points[2]=end+direction*.5f;points[3]=end+side;points[4]=origin+side;points[5]=origin-side;Show(6,progress,true);
        }
        public void Circle(Vector3 origin,float radius,float progress=1){
            origin=Ground(origin);for(int i=0;i<=32;i++){float angle=i*Mathf.PI*2/32;points[i]=origin+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;}Show(33,progress,true);
        }
    }
}
