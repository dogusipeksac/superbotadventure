using SuperBot.Controls;
using SuperBot.Player;
using UnityEngine;

namespace SuperBot.CameraSystem
{
    /// <summary>
    /// Orbit camera driven by the right-side touch zone. SphereCast collision pulls the camera in instantly
    /// when something blocks the view and eases it back out. Auto-recenters behind the movement direction.
    /// </summary>
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private PlayerMotor targetMotor;

        [Header("Orbit")]
        [SerializeField] private float distance = 6f;
        [SerializeField] private float minDistance = 1.2f;
        [SerializeField] private float heightOffset = 1.5f;
        [SerializeField] private float yawSensitivity = 0.15f;
        [SerializeField] private float pitchSensitivity = 0.1f;
        [SerializeField] private float minPitch = -20f;
        [SerializeField] private float maxPitch = 60f;
        [SerializeField] private float startPitch = 20f;
        [SerializeField] private float followSmoothTime = 0.08f;

        [Header("Collision")]
        [SerializeField] private float collisionRadius = 0.3f;
        [SerializeField] private LayerMask collisionMask = 1;
        [SerializeField] private float returnSmoothTime = 0.25f;

        [Header("Auto recenter")]
        [SerializeField] private float autoRecenterDelay = 2.5f;
        [SerializeField] private float autoRecenterSpeed = 90f;

        private float yaw;
        private float pitch;
        private float currentDistance;
        private float distanceVelocity;
        private Vector3 smoothedTarget;
        private Vector3 followVelocity;
        private float lastLookTime;
        private Transform overrideView;

        /// <summary>Temporarily frame a fixed view (e.g. shop preview). Pass null to return to orbit.</summary>
        public void SetOverride(Transform view) => overrideView = view;

        public Transform Target
        {
            get => target;
            set
            {
                target = value;
                targetMotor = value != null ? value.GetComponent<PlayerMotor>() : null;
                SnapToTarget();
            }
        }

        private void Start()
        {
            if (target != null && targetMotor == null) targetMotor = target.GetComponent<PlayerMotor>();
            pitch = startPitch;
            yaw = target != null ? target.eulerAngles.y : transform.eulerAngles.y;
            SnapToTarget();
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            smoothedTarget = target.position;
            followVelocity = Vector3.zero;
            currentDistance = distance;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;

            if (overrideView != null)
            {
                float k = 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime);
                transform.SetPositionAndRotation(Vector3.Lerp(transform.position, overrideView.position, k),
                    Quaternion.Slerp(transform.rotation, overrideView.rotation, k));
                return;
            }

            Vector2 look = MobileInputRouter.Instance != null ? MobileInputRouter.Instance.State.LookDelta : Vector2.zero;
            if (look.sqrMagnitude > 0.01f) lastLookTime = Time.time;
            yaw += look.x * yawSensitivity;
            pitch = Mathf.Clamp(pitch - look.y * pitchSensitivity, minPitch, maxPitch);

            if (targetMotor != null && Time.time - lastLookTime > autoRecenterDelay)
            {
                Vector3 v = targetMotor.HorizontalVelocity;
                if (v.sqrMagnitude > 1f)
                {
                    float targetYaw = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
                    yaw = Mathf.MoveTowardsAngle(yaw, targetYaw, autoRecenterSpeed * dt);
                }
            }

            // Follow (teleports snap instantly).
            if ((target.position - smoothedTarget).sqrMagnitude > 100f) SnapToTarget();
            smoothedTarget = Vector3.SmoothDamp(smoothedTarget, target.position, ref followVelocity, followSmoothTime);
            Vector3 pivot = smoothedTarget + Vector3.up * heightOffset;

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 back = rotation * Vector3.back;

            float desired = distance;
            if (Physics.SphereCast(pivot, collisionRadius, back, out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
                desired = Mathf.Max(minDistance, hit.distance - 0.05f);

            if (desired < currentDistance)
            {
                currentDistance = desired;
                distanceVelocity = 0f;
            }
            else
            {
                currentDistance = Mathf.SmoothDamp(currentDistance, desired, ref distanceVelocity, returnSmoothTime);
            }

            transform.SetPositionAndRotation(pivot + back * currentDistance, rotation);
        }
    }
}
