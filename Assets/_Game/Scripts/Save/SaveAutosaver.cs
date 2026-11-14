using SuperBot.Core;
using UnityEngine;

namespace SuperBot.Save
{
    /// <summary>Flushes dirty saves every few seconds and whenever the app is paused or quits.</summary>
    public sealed class SaveAutosaver : MonoBehaviour
    {
        [SerializeField] private float interval = 5f;
        private float nextSaveTime;

        private void Update()
        {
            if (Time.unscaledTime < nextSaveTime) return;
            nextSaveTime = Time.unscaledTime + interval;
            Flush(false);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Flush(true);
        }

        private void OnApplicationQuit() => Flush(true);

        private static void Flush(bool force)
        {
            if (!ServiceLocator.TryGet(out SaveService save)) return;
            if (force || save.IsDirty) save.Save();
        }
    }
}
