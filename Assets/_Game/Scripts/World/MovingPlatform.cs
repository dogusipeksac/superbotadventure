using System.Collections.Generic;
using UnityEngine;

namespace SuperBot.World
{
    /// <summary>
    /// Ping-pongs between waypoints (local offsets from the start position). Moves in Update before the player
    /// (execution order) and exposes <see cref="FrameDelta"/>, which PlayerMotor applies while standing on it.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class MovingPlatform : MonoBehaviour
    {
        [SerializeField] private List<Vector3> waypointOffsets = new List<Vector3> { Vector3.zero, new Vector3(0f, 0f, 8f) };
        [SerializeField] private float speed = 2.5f;
        [SerializeField] private float waitTime = 0.6f;

        public Vector3 FrameDelta { get; private set; }

        private Vector3 origin;
        private int index;
        private int direction = 1;
        private float waitUntil;

        private void Awake()
        {
            origin = transform.position;
            if (TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = true;
                rb.interpolation = RigidbodyInterpolation.None;
            }
        }

        private void Update()
        {
            FrameDelta = Vector3.zero;
            if (waypointOffsets.Count < 2 || Time.time < waitUntil) return;

            Vector3 target = origin + waypointOffsets[index];
            Vector3 before = transform.position;
            Vector3 after = Vector3.MoveTowards(before, target, speed * Time.deltaTime);
            transform.position = after;
            FrameDelta = after - before;

            if ((after - target).sqrMagnitude < 0.0001f)
            {
                if (index + direction >= waypointOffsets.Count || index + direction < 0) direction = -direction;
                index += direction;
                waitUntil = Time.time + waitTime;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 o = Application.isPlaying ? origin : transform.position;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypointOffsets.Count; i++)
            {
                Gizmos.DrawWireCube(o + waypointOffsets[i], Vector3.one * 0.4f);
                if (i > 0) Gizmos.DrawLine(o + waypointOffsets[i - 1], o + waypointOffsets[i]);
            }
        }
#endif
    }
}
