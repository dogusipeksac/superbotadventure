using UnityEngine;

namespace SuperBot.World
{
    /// <summary>Slow cinematic orbit used by the main menu.</summary>
    public sealed class OrbitCamera : MonoBehaviour
    {
        [SerializeField] private Vector3 center = Vector3.zero;
        [SerializeField] private float radius = 9f;
        [SerializeField] private float height = 4f;
        [SerializeField] private float degreesPerSecond = 8f;
        [SerializeField] private float lookHeight = 1f;

        private float angle;

        private void LateUpdate()
        {
            angle += degreesPerSecond * Time.unscaledDeltaTime;
            float rad = angle * Mathf.Deg2Rad;
            transform.position = center + new Vector3(Mathf.Sin(rad) * radius, height, Mathf.Cos(rad) * radius);
            transform.LookAt(center + Vector3.up * lookHeight);
        }
    }
}
