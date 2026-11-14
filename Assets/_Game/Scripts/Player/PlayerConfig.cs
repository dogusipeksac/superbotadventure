using UnityEngine;

namespace SuperBot.Player
{
    [CreateAssetMenu(menuName = "SuperBot/Player Config", fileName = "PlayerConfig")]
    public sealed class PlayerConfig : ScriptableObject
    {
        [Header("Ground movement")]
        public float moveSpeed = 7f;
        public float acceleration = 60f;
        public float deceleration = 70f;
        [Range(0f, 1f)] public float airControl = 0.6f;
        public float rotationSpeed = 720f;

        [Header("Gravity")]
        public float gravity = -30f;
        public float fallGravityMultiplier = 1.6f;
        public float maxFallSpeed = -25f;

        [Header("Jump")]
        public float jumpHeight = 2.2f;
        public float doubleJumpHeight = 1.6f;
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.12f;

        [Header("Jetpack hover")]
        public float hoverDuration = 1.2f;
        public float hoverFallSpeed = -1.5f;

        [Header("Dash")]
        public float dashSpeed = 18f;
        public float dashDuration = 0.18f;
        public float dashCooldown = 0.6f;

        [Header("Spin attack")]
        public float spinDuration = 0.4f;
        public float spinRadius = 1.4f;
        public float spinCooldown = 0.15f;

        [Header("Landing feel")]
        public float landSquashAmount = 0.25f;
        public float landSquashTime = 0.15f;
        public float hardLandingSpeed = 14f;
    }
}
