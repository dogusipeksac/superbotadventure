using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuperBot.Core
{
    /// <summary>Async scene loads behind a black fade. Lives on the persistent Bootstrapper.</summary>
    public sealed class SceneLoader : MonoBehaviour
    {
        [SerializeField] private float fadeTime = 0.25f;

        private CanvasGroup fade;
        public bool IsLoading { get; private set; }

        private void Awake()
        {
            ServiceLocator.Register(this);
            BuildFadeCanvas();
        }

        private void OnDestroy() => ServiceLocator.Unregister(this);

        public static void LoadScene(string sceneName)
        {
            if (ServiceLocator.TryGet(out SceneLoader loader)) loader.Load(sceneName);
            else SceneManager.LoadScene(sceneName);
        }

        public void Load(string sceneName)
        {
            if (IsLoading) return;
            StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;
            fade.blocksRaycasts = true;
            yield return Fade(0f, 1f);

            if (ServiceLocator.TryGet(out PauseService pause)) pause.SetPaused(false);
            PauseService.GameplayActive = false;
            Time.timeScale = 1f;

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            if (op != null)
                while (!op.isDone) yield return null;
            else
                Debug.LogError($"[SceneLoader] Scene '{sceneName}' is not in Build Settings.");

            yield return null;
            yield return Fade(1f, 0f);
            fade.blocksRaycasts = false;
            IsLoading = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                fade.alpha = Mathf.Lerp(from, to, t / fadeTime);
                yield return null;
            }
            fade.alpha = to;
        }

        private void BuildFadeCanvas()
        {
            var go = new GameObject("[Fade]");
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            go.AddComponent<GraphicRaycaster>();
            fade = go.AddComponent<CanvasGroup>();
            fade.alpha = 0f;
            fade.blocksRaycasts = false;
            fade.interactable = false;

            var img = new GameObject("Black", typeof(RectTransform)).AddComponent<Image>();
            img.transform.SetParent(go.transform, false);
            img.color = Color.black;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
