using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WordBubblePop.EditorTools
{
    /// <summary>
    /// Sets the app name, app ID, portrait mode and icon for Android and iOS.
    /// Runs once automatically; you can run it again from the menu:
    /// Word Bubble Pop > Apply Project Settings
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string AppId = "com.deeksha.wordbubblepop";
        const string DoneKey = "WordBubblePop.SettingsApplied";

        static ProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (PlayerSettings.productName != "Word Bubble Pop" && !SessionState.GetBool(DoneKey, false))
                {
                    SessionState.SetBool(DoneKey, true);
                    Apply();
                }
            };
        }

        [MenuItem("Word Bubble Pop/Apply Project Settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = "Deeksha";
            PlayerSettings.productName = "Word Bubble Pop";
            PlayerSettings.bundleVersion = "1.0.0";

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, AppId);

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.SplashScreen.backgroundColor = new Color32(0xBF, 0xE9, 0xF2, 0xFF);

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/AppIcon.png");
            if (icon != null)
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

            EnsureScene();

            AssetDatabase.SaveAssets();
            Debug.Log("Word Bubble Pop: project settings applied (name, app ID " + AppId + ", portrait, icon).");
        }

        /// <summary>Creates Assets/Scenes/Main.unity and adds it to the build list if there is no scene yet.</summary>
        static void EnsureScene()
        {
            const string scenePath = "Assets/Scenes/Main.unity";
            if (EditorBuildSettings.scenes.Length > 0) return;

            if (!File.Exists(scenePath))
            {
                Directory.CreateDirectory("Assets/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, scenePath);
            }
            else
            {
                EditorSceneManager.OpenScene(scenePath);
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
        }
    }
}
