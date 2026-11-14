using UnityEditor;
using UnityEditor.SceneManagement;

namespace SuperBot.EditorTools
{
    /// <summary>
    /// Pressing Play always starts from the first scene in Build Settings (TestIsland), whatever scene is open.
    /// Toggle with SuperBot/Play From First Build Scene.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStartScene
    {
        private const string MenuPath = "SuperBot/Play From First Build Scene";
        private const string PrefKey = "SuperBot.PlayFromFirstScene";

        static PlayModeStartScene()
        {
            EditorApplication.delayCall += Apply;
            EditorBuildSettings.sceneListChanged += Apply;
        }

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        [MenuItem(MenuPath, priority = 40)]
        private static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        [MenuItem("SuperBot/Open Test Scene", priority = 41)]
        private static void OpenTestScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene("Assets/_Game/Scenes/Island01.unity");
        }

        private static void Apply()
        {
            SceneAsset first = null;
            if (Enabled && EditorBuildSettings.scenes.Length > 0)
                first = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[0].path);
            EditorSceneManager.playModeStartScene = first;
        }
    }
}
