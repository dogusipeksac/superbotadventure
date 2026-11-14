using SuperBot.Audio;
using SuperBot.Core;
using UnityEngine;

namespace SuperBot.Player
{
    /// <summary>Squash & stretch, dust and haptics for jumps and landings.</summary>
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerFeedback : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private PooledParticle dustPrefab;
        [SerializeField] private ParticleSystem hoverParticles;
        [SerializeField] private float jumpStretch = 0.12f;

        private PlayerMotor motor;
        private Vector3 baseScale = Vector3.one;
        private Vector3 fromScale = Vector3.one;
        private float animStart = -10f;
        private float animDuration = 0.15f;

        private void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            if (visualRoot != null) baseScale = visualRoot.localScale;
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerLanded += HandleLanded;
            motor.Jumped += HandleJumped;
            motor.DoubleJumped += HandleDoubleJumped;
            motor.Dashed += HandleDashed;
            motor.SpinStarted += HandleSpin;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerLanded -= HandleLanded;
            motor.Jumped -= HandleJumped;
            motor.DoubleJumped -= HandleDoubleJumped;
            motor.Dashed -= HandleDashed;
            motor.SpinStarted -= HandleSpin;
        }

        private void HandleDoubleJumped()
        {
            HandleJumped();
            AudioService.Play(SfxId.DoubleJump);
        }

        private void HandleDashed() => AudioService.Play(SfxId.Dash);
        private void HandleSpin() => AudioService.Play(SfxId.Spin);

        private void HandleLanded(float impactSpeed)
        {
            var cfg = motor.Config;
            float strength = Mathf.Clamp01(impactSpeed / cfg.hardLandingSpeed);
            float amount = cfg.landSquashAmount * Mathf.Lerp(0.4f, 1f, strength);
            Play(new Vector3(1f + amount * 0.5f, 1f - amount, 1f + amount * 0.5f), cfg.landSquashTime);

            if (dustPrefab != null) PoolManager.SpawnStatic(dustPrefab, transform.position, Quaternion.identity);
            AudioService.Play(SfxId.Land);

            if (ServiceLocator.TryGet(out HapticManager haptics))
            {
                if (impactSpeed >= cfg.hardLandingSpeed) haptics.Medium();
                else haptics.Light();
            }
        }

        private void HandleJumped()
        {
            if (motor.JumpCount <= 1) AudioService.Play(SfxId.Jump);
            Play(new Vector3(1f - jumpStretch * 0.5f, 1f + jumpStretch, 1f - jumpStretch * 0.5f), 0.18f);
            if (dustPrefab != null && motor.IsGrounded) PoolManager.SpawnStatic(dustPrefab, transform.position, Quaternion.identity);
        }

        private void Play(Vector3 scaleFactor, float duration)
        {
            fromScale = Vector3.Scale(baseScale, scaleFactor);
            animDuration = Mathf.Max(0.01f, duration);
            animStart = Time.time;
        }

        private void LateUpdate()
        {
            if (hoverParticles != null)
            {
                if (motor.IsHovering && !hoverParticles.isEmitting) hoverParticles.Play();
                else if (!motor.IsHovering && hoverParticles.isEmitting) hoverParticles.Stop();
            }

            if (visualRoot == null) return;
            float t = (Time.time - animStart) / animDuration;
            if (t >= 1f)
            {
                if (visualRoot.localScale != baseScale) visualRoot.localScale = baseScale;
                return;
            }
            float ease = 1f - (1f - t) * (1f - t); // ease-out quad
            visualRoot.localScale = Vector3.LerpUnclamped(fromScale, baseScale, ease);
        }
    }
}
