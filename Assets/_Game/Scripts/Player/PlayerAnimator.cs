using UnityEngine;

namespace SuperBot.Player
{
    /// <summary>
    /// Drives either a Mecanim Animator (if one with a controller is present) or a legacy Animation component
    /// (glTFast imports Polyfork clips as legacy by default). Missing clips are simply skipped.
    /// Spin is procedural (rotates the visual) because no spin clip exists yet.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Animation legacyAnimation;
        [SerializeField] private Transform spinVisual;
        [SerializeField] private float crossFade = 0.15f;

        [Header("Legacy clip names")]
        [SerializeField] private string idleClip = "Idle";
        [SerializeField] private string walkClip = "Walk";
        [SerializeField] private string runClip = "Run";
        [SerializeField] private string airClip = "Jog";
        [SerializeField] private string hardLandClip = "Hard_landing";

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int SpinHash = Animator.StringToHash("Spin");
        private static readonly int DashHash = Animator.StringToHash("Dash");
        private static readonly int HardLandHash = Animator.StringToHash("HardLand");

        private PlayerMotor motor;
        private string currentLegacy;
        private float lockLegacyUntil;
        private float spinAngle;

        private bool UseAnimator => animator != null && animator.runtimeAnimatorController != null;

        private void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (legacyAnimation == null) legacyAnimation = GetComponentInChildren<Animation>();
        }

        private void OnEnable()
        {
            motor.Jumped += OnJump;
            motor.DoubleJumped += OnJump;
            motor.Dashed += OnDash;
            motor.SpinStarted += OnSpin;
            Core.GameEvents.OnPlayerLanded += OnLanded;
        }

        private void OnDisable()
        {
            motor.Jumped -= OnJump;
            motor.DoubleJumped -= OnJump;
            motor.Dashed -= OnDash;
            motor.SpinStarted -= OnSpin;
            Core.GameEvents.OnPlayerLanded -= OnLanded;
        }

        private void Update()
        {
            float speed01 = motor.Config != null ? motor.HorizontalVelocity.magnitude / motor.Config.moveSpeed : 0f;

            if (UseAnimator)
            {
                animator.SetFloat(SpeedHash, speed01);
                animator.SetBool(GroundedHash, motor.IsGrounded);
                animator.SetFloat(VerticalSpeedHash, motor.Velocity.y);
            }
            else if (legacyAnimation != null && Time.time >= lockLegacyUntil)
            {
                string clip;
                if (!motor.IsGrounded) clip = airClip;
                else if (speed01 > 0.6f) clip = runClip;
                else if (speed01 > 0.1f) clip = walkClip;
                else clip = idleClip;
                PlayLegacy(clip);
            }

            // Procedural spin until a real spin clip exists.
            if (spinVisual != null)
            {
                if (motor.IsSpinning)
                {
                    spinAngle += 1080f * Time.deltaTime;
                    spinVisual.localRotation = Quaternion.Euler(0f, spinAngle, 0f);
                }
                else if (spinAngle != 0f)
                {
                    spinAngle = 0f;
                    spinVisual.localRotation = Quaternion.identity;
                }
            }
        }

        private void PlayLegacy(string clip, bool force = false)
        {
            if (string.IsNullOrEmpty(clip) || (!force && clip == currentLegacy)) return;
            if (legacyAnimation[clip] == null) return;
            legacyAnimation[clip].wrapMode = WrapMode.Loop;
            legacyAnimation.CrossFade(clip, crossFade);
            currentLegacy = clip;
        }

        private void OnJump() { if (UseAnimator) animator.SetTrigger(JumpHash); }
        private void OnDash() { if (UseAnimator) animator.SetTrigger(DashHash); }
        private void OnSpin() { if (UseAnimator) animator.SetTrigger(SpinHash); }

        private void OnLanded(float impact)
        {
            if (motor.Config == null || impact < motor.Config.hardLandingSpeed) return;
            if (UseAnimator)
            {
                animator.SetTrigger(HardLandHash);
            }
            else if (legacyAnimation != null && legacyAnimation[hardLandClip] != null)
            {
                legacyAnimation[hardLandClip].wrapMode = WrapMode.Once;
                legacyAnimation.CrossFade(hardLandClip, 0.05f);
                currentLegacy = hardLandClip;
                lockLegacyUntil = Time.time + 0.5f;
            }
        }
    }
}
