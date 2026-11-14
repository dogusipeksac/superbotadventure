using System;
using System.Collections.Generic;
using SuperBot.Controls;
using SuperBot.Core;
using SuperBot.World;
using UnityEngine;

namespace SuperBot.Player
{
    /// <summary>
    /// Camera-relative CharacterController platformer motor: coyote time, jump buffering, double jump,
    /// jetpack hover, dash, spin attack, jump pads, moving platforms and stomping.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DefaultExecutionOrder(-50)]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private LayerMask spinHitMask = ~0;
        [SerializeField] private float stompMinFallSpeed = 2f;

        public event Action Jumped;
        public event Action DoubleJumped;
        public event Action Dashed;
        public event Action SpinStarted;
        public event Action<ControllerColliderHit> OnHit;

        public PlayerConfig Config => config;
        public Vector3 Velocity => new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
        public Vector3 HorizontalVelocity => horizontalVelocity;
        public bool IsGrounded => grounded;
        public bool IsDashing => Time.time < dashEndTime;
        public bool IsSpinning => Time.time < spinEndTime;
        public bool IsHovering { get; private set; }
        public int JumpCount => jumpCount;
        public bool InputEnabled { get; set; } = true;

        private CharacterController controller;
        private Collider ownCollider;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private bool grounded;
        private float lastGroundedTime = -10f;
        private float lastJumpPressedTime = -10f;
        private int jumpCount;
        private bool doubleJumpUsed;
        private bool airDashUsed;
        private float hoverTimeUsed;
        private float airTime;
        private float dashEndTime = -10f;
        private float nextDashTime;
        private Vector3 dashDirection;
        private float spinEndTime = -10f;
        private float nextSpinTime;
        private float preMoveVerticalVelocity;
        private Vector3 externalDelta;

        private MovingPlatform groundPlatform;
        private MovingPlatform hitPlatformThisFrame;
        private Collider lastGroundCollider;

        private readonly Collider[] spinHits = new Collider[16];
        private readonly HashSet<IDamageable> spinVictims = new HashSet<IDamageable>();

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            ownCollider = controller;
            if (config == null) config = ScriptableObject.CreateInstance<PlayerConfig>();
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            float now = Time.time;

            PlayerInputState input = InputEnabled && MobileInputRouter.Instance != null ? MobileInputRouter.Instance.State : null;
            Vector2 move = input != null ? input.Move : Vector2.zero;
            bool jumpPressed = input != null && input.JumpPressed;
            bool jumpHeld = input != null && input.JumpHeld;

            if (jumpPressed) lastJumpPressedTime = now;

            // Ride moving platforms (they move earlier in the frame).
            Vector3 frameDelta = externalDelta;
            externalDelta = Vector3.zero;
            if (grounded && groundPlatform != null) frameDelta += groundPlatform.FrameDelta;

            // ---- Horizontal ----
            Vector3 wish = CameraRelative(move);
            if (IsDashing)
            {
                horizontalVelocity = dashDirection * config.dashSpeed;
            }
            else
            {
                float control = grounded ? 1f : config.airControl;
                Vector3 target = wish * config.moveSpeed;
                float rate = (wish.sqrMagnitude > 0.0001f ? config.acceleration : config.deceleration) * control;
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, rate * dt);

                if (wish.sqrMagnitude > 0.0001f)
                {
                    Quaternion look = Quaternion.LookRotation(wish, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, look, config.rotationSpeed * dt);
                }
            }

            // ---- Jump (buffer + coyote) / double jump ----
            bool buffered = now - lastJumpPressedTime <= config.jumpBufferTime;
            bool canGroundJump = jumpCount == 0 && (grounded || now - lastGroundedTime <= config.coyoteTime);
            if (buffered && canGroundJump)
            {
                verticalVelocity = JumpVelocity(config.jumpHeight);
                jumpCount = 1;
                grounded = false;
                lastJumpPressedTime = -10f;
                lastGroundedTime = -10f;
                Jumped?.Invoke();
            }
            else if (jumpPressed && !grounded && !doubleJumpUsed)
            {
                verticalVelocity = JumpVelocity(config.doubleJumpHeight);
                doubleJumpUsed = true;
                jumpCount = 2;
                lastJumpPressedTime = -10f;
                DoubleJumped?.Invoke();
            }

            // ---- Dash ----
            if (input != null && input.DashPressed && now >= nextDashTime && (grounded || !airDashUsed))
            {
                dashDirection = wish.sqrMagnitude > 0.0001f ? wish.normalized : transform.forward;
                transform.rotation = Quaternion.LookRotation(dashDirection, Vector3.up);
                dashEndTime = now + config.dashDuration;
                nextDashTime = now + config.dashCooldown;
                if (!grounded) airDashUsed = true;
                verticalVelocity = Mathf.Max(verticalVelocity, 0f);
                Dashed?.Invoke();
            }

            // ---- Spin ----
            if (input != null && input.AttackPressed && !IsSpinning && now >= nextSpinTime)
            {
                spinEndTime = now + config.spinDuration;
                nextSpinTime = spinEndTime + config.spinCooldown;
                spinVictims.Clear();
                SpinStarted?.Invoke();
            }

            // ---- Gravity / hover ----
            IsHovering = false;
            if (IsDashing)
            {
                verticalVelocity = 0f;
            }
            else
            {
                float g = config.gravity * (verticalVelocity < 0f ? config.fallGravityMultiplier : 1f);
                verticalVelocity = Mathf.Max(verticalVelocity + g * dt, config.maxFallSpeed);

                if (!grounded && doubleJumpUsed && jumpHeld && verticalVelocity < 0f && hoverTimeUsed < config.hoverDuration)
                {
                    IsHovering = true;
                    hoverTimeUsed += dt;
                    verticalVelocity = Mathf.Max(verticalVelocity, config.hoverFallSpeed);
                }
            }
            if (grounded && verticalVelocity < 0f) verticalVelocity = -2f; // keep snapped to slopes

            // ---- Move ----
            preMoveVerticalVelocity = verticalVelocity;
            hitPlatformThisFrame = null;
            bool wasGrounded = grounded;
            Vector3 motion = (horizontalVelocity + Vector3.up * verticalVelocity) * dt + frameDelta;
            controller.Move(motion);

            grounded = controller.isGrounded;
            groundPlatform = grounded ? hitPlatformThisFrame : null;

            if ((controller.collisionFlags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
                verticalVelocity = 0f;

            if (grounded)
            {
                if (!wasGrounded && airTime > 0.1f)
                    GameEvents.RaisePlayerLanded(-preMoveVerticalVelocity);
                lastGroundedTime = now;
                airTime = 0f;
                jumpCount = 0;
                doubleJumpUsed = false;
                airDashUsed = false;
                hoverTimeUsed = 0f;
            }
            else
            {
                airTime += dt;
            }
        }

        private void FixedUpdate()
        {
            if (!IsSpinning) return;
            Vector3 center = transform.position + Vector3.up * (controller.height * 0.5f);
            int count = Physics.OverlapSphereNonAlloc(center, config.spinRadius, spinHits, spinHitMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Collider c = spinHits[i];
                if (c == ownCollider || c.transform.IsChildOf(transform)) continue;
                var target = c.GetComponentInParent<IDamageable>();
                if (target == null || !spinVictims.Add(target)) continue;
                target.TakeHit(transform.position);
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            OnHit?.Invoke(hit);

            if (hit.normal.y < 0.7f) return;

            // Platform detection (only re-query when the ground collider changes).
            if (hit.collider != lastGroundCollider)
            {
                lastGroundCollider = hit.collider;
                cachedPlatform = hit.collider.GetComponentInParent<MovingPlatform>();
            }
            if (cachedPlatform != null) hitPlatformThisFrame = cachedPlatform;

            // Stomp.
            if (preMoveVerticalVelocity < -stompMinFallSpeed)
            {
                var stompable = hit.collider.GetComponentInParent<IStompable>();
                if (stompable != null)
                {
                    float bounce = stompable.OnStomped(transform.position);
                    if (bounce > 0f) Launch(bounce);
                }
            }
        }

        private MovingPlatform cachedPlatform;

        // ---- External API ----

        /// <summary>Jump pads, stomps: sets upward velocity and refreshes air abilities.</summary>
        public void Launch(float upVelocity)
        {
            verticalVelocity = upVelocity;
            preMoveVerticalVelocity = upVelocity;
            grounded = false;
            jumpCount = 1;
            doubleJumpUsed = false;
            airDashUsed = false;
            hoverTimeUsed = 0f;
            lastGroundedTime = -10f;
            dashEndTime = -10f;
        }

        public void AddPlatformDelta(Vector3 delta) => externalDelta += delta;

        public void Knockback(Vector3 direction, float horizontalSpeed, float upVelocity)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) direction = -transform.forward;
            horizontalVelocity = direction.normalized * horizontalSpeed;
            verticalVelocity = upVelocity;
            grounded = false;
            dashEndTime = -10f;
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = true;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            externalDelta = Vector3.zero;
            groundPlatform = null;
            dashEndTime = -10f;
            spinEndTime = -10f;
            Physics.SyncTransforms();
        }

        private float JumpVelocity(float height) => Mathf.Sqrt(2f * height * -config.gravity);

        private Vector3 CameraRelative(Vector2 move)
        {
            if (move.sqrMagnitude < 0.0001f) return Vector3.zero;
            Vector3 fwd, right;
            if (cameraTransform != null)
            {
                fwd = cameraTransform.forward; fwd.y = 0f;
                if (fwd.sqrMagnitude < 0.0001f) fwd = cameraTransform.up; fwd.y = 0f;
                fwd.Normalize();
                right = new Vector3(fwd.z, 0f, -fwd.x);
            }
            else
            {
                fwd = Vector3.forward;
                right = Vector3.right;
            }
            return Vector3.ClampMagnitude(fwd * move.y + right * move.x, 1f);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (config == null) return;
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.7f, config.spinRadius);
        }
#endif
    }
}
