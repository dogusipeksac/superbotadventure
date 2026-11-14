using SuperBot.Collectibles;
using SuperBot.Core;
using SuperBot.Player;
using UnityEngine;

namespace SuperBot.World
{
    /// <summary>
    /// Ground enemy with a small state machine: Patrol -> Chase -> Stunned -> Dead.
    /// Hurts the player on contact unless the player spins or stomps it.
    /// Needs a trigger collider (+ kinematic Rigidbody) for contact detection.
    /// </summary>
    public sealed class PatrolEnemy : MonoBehaviour, IDamageable
    {
        [SerializeField] private Vector3[] waypointOffsets = { new Vector3(-3f, 0f, 0f), new Vector3(3f, 0f, 0f) };
        [SerializeField] private float patrolSpeed = 1.8f;
        [SerializeField] private float chaseSpeed = 3.2f;
        [SerializeField] private float detectRange = 6f;
        [SerializeField] private float loseRange = 9f;
        [SerializeField] private float maxChaseDistanceFromHome = 8f;
        [SerializeField] private float stunTime = 1f;
        [SerializeField] private int maxHp = 1;
        [SerializeField] private float stompBounce = 11f;
        [SerializeField] private LayerMask sightBlockers = 1;
        [SerializeField] private Bolt boltPrefab;
        [SerializeField] private int boltDrop = 2;
        [SerializeField] private PooledParticle deathFx;
        [SerializeField] private Transform visual;

        private interface IEnemyState
        {
            void Enter();
            void Tick(float dt);
            void Exit();
        }

        private IEnemyState state;
        private PatrolState patrol;
        private ChaseState chase;
        private StunnedState stunned;
        private DeadState dead;

        private Vector3 home;
        private int hp;
        private PlayerHealth player;

        private void Awake()
        {
            home = transform.position;
            hp = maxHp;
            patrol = new PatrolState(this);
            chase = new ChaseState(this);
            stunned = new StunnedState(this);
            dead = new DeadState(this);
            if (TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;
        }

        private void Start()
        {
            ServiceLocator.TryGet(out player);
            ChangeState(patrol);
        }

        private void Update()
        {
            if (player == null) ServiceLocator.TryGet(out player);
            state?.Tick(Time.deltaTime);
        }

        private void ChangeState(IEnemyState next)
        {
            if (state == next) return;
            state?.Exit();
            state = next;
            state.Enter();
        }

        // ---- Damage ----

        public void TakeHit(Vector3 source)
        {
            if (state == dead || state == stunned) return;
            hp--;
            if (hp <= 0) ChangeState(dead);
            else ChangeState(stunned);
        }

        private void OnTriggerStay(Collider other)
        {
            if (state == dead || !other.TryGetComponent(out PlayerHealth hitPlayer)) return;
            PlayerMotor motor = hitPlayer.Motor;

            if (motor.IsSpinning)
            {
                TakeHit(other.transform.position);
                return;
            }

            bool fromAbove = other.transform.position.y > transform.position.y + 0.6f && motor.Velocity.y < 0f;
            if (fromAbove)
            {
                TakeHit(other.transform.position);
                motor.Launch(stompBounce);
                return;
            }

            if (state != stunned) hitPlayer.TakeHit(transform.position);
        }

        // ---- Helpers ----

        private bool CanSeePlayer(float range)
        {
            if (player == null || player.IsDead) return false;
            Vector3 eye = transform.position + Vector3.up * 0.5f;
            Vector3 to = player.transform.position + Vector3.up * 0.7f - eye;
            if (to.sqrMagnitude > range * range) return false;
            return !Physics.Raycast(eye, to.normalized, to.magnitude, sightBlockers, QueryTriggerInteraction.Ignore);
        }

        private void MoveTowards(Vector3 target, float speed, float dt)
        {
            target.y = transform.position.y;
            Vector3 to = target - transform.position;
            if (to.sqrMagnitude < 0.0004f) return;
            transform.position = Vector3.MoveTowards(transform.position, target, speed * dt);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to, Vector3.up), 360f * dt);
        }

        private void DropLoot()
        {
            Vector3 center = transform.position + Vector3.up * 0.5f;
            if (deathFx != null) PoolManager.SpawnStatic(deathFx, center, Quaternion.identity);
            if (boltPrefab == null) return;
            for (int i = 0; i < boltDrop; i++)
            {
                Bolt b = PoolManager.SpawnStatic(boltPrefab, center, Quaternion.identity);
                b.Pop(new Vector3(Random.Range(-2f, 2f), 7f, Random.Range(-2f, 2f)));
            }
        }

        // ---- States ----

        private sealed class PatrolState : IEnemyState
        {
            private readonly PatrolEnemy e;
            private int index;
            public PatrolState(PatrolEnemy e) { this.e = e; }
            public void Enter() { }
            public void Exit() { }

            public void Tick(float dt)
            {
                if (e.CanSeePlayer(e.detectRange)) { e.ChangeState(e.chase); return; }
                if (e.waypointOffsets == null || e.waypointOffsets.Length == 0) return;
                Vector3 target = e.home + e.waypointOffsets[index];
                e.MoveTowards(target, e.patrolSpeed, dt);
                Vector3 flat = target - e.transform.position; flat.y = 0f;
                if (flat.sqrMagnitude < 0.04f) index = (index + 1) % e.waypointOffsets.Length;
            }
        }

        private sealed class ChaseState : IEnemyState
        {
            private readonly PatrolEnemy e;
            public ChaseState(PatrolEnemy e) { this.e = e; }
            public void Enter() { }
            public void Exit() { }

            public void Tick(float dt)
            {
                bool tooFar = (e.transform.position - e.home).sqrMagnitude > e.maxChaseDistanceFromHome * e.maxChaseDistanceFromHome;
                if (tooFar || !e.CanSeePlayer(e.loseRange)) { e.ChangeState(e.patrol); return; }
                e.MoveTowards(e.player.transform.position, e.chaseSpeed, dt);
            }
        }

        private sealed class StunnedState : IEnemyState
        {
            private readonly PatrolEnemy e;
            private float until;
            public StunnedState(PatrolEnemy e) { this.e = e; }
            public void Enter()
            {
                until = Time.time + e.stunTime;
                if (e.visual != null) e.visual.localScale = new Vector3(1.2f, 0.6f, 1.2f);
            }
            public void Exit() { if (e.visual != null) e.visual.localScale = Vector3.one; }
            public void Tick(float dt) { if (Time.time >= until) e.ChangeState(e.patrol); }
        }

        private sealed class DeadState : IEnemyState
        {
            private readonly PatrolEnemy e;
            public DeadState(PatrolEnemy e) { this.e = e; }
            public void Enter()
            {
                e.DropLoot();
                if (ServiceLocator.TryGet(out HapticManager h)) h.Light();
                e.gameObject.SetActive(false);
            }
            public void Exit() { }
            public void Tick(float dt) { }
        }
    }
}
