using UnityEngine;

namespace SuperBot.UI
{
    /// <summary>Fits this RectTransform (stretched full-screen) into Screen.safeArea (notches, home bar).</summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform rect;
        private Rect lastSafeArea;
        private Vector2Int lastScreen;

        private void OnEnable()
        {
            rect = (RectTransform)transform;
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea || Screen.width != lastScreen.x || Screen.height != lastScreen.y)
                Apply();
        }

        private void Apply()
        {
            Rect safe = Screen.safeArea;
            lastSafeArea = safe;
            lastScreen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width; min.y /= Screen.height;
            max.x /= Screen.width; max.y /= Screen.height;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
