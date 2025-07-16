using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif

namespace Hypertonic.Modules.UltimateSockets.Examples
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif

    //// <summary>
    /// This class is used to manage the settings when entering and exxiting the demo scene.
    /// </summary>
    public class DemoSceneManager : MonoBehaviour
    {
        public const string SelectedSettingsKey = "ULTIMATE_SOCKET_SELECTED_SETTINGS_PATH_KEY";

        private const string _demoMouseInteractionAssetPath = "Assets/Hypertonic Games/Ultimate Sockets/Examples/Settings/Ultimate Socket Settings.asset";

        private const string _mouseInteractionDemoSceneName = "Demo - Mouse Interactions";
        private const string _XRITDemoSceneName = "Demo - XR Interaction Toolkit";

        private static string _currentSettingsConfigPath = string.Empty;

#if UNITY_EDITOR
        static DemoSceneManager()
        {
            EditorSceneManager.sceneOpened += HandleSceneOpened;
        }

        private static void HandleSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.name.Equals(_mouseInteractionDemoSceneName) || scene.name.Equals(_XRITDemoSceneName))
            {
                Debug.Log("Loaded into demo scene. Setting demo socket settings.");

                if (string.IsNullOrEmpty(_currentSettingsConfigPath))
                {
                    _currentSettingsConfigPath = PlayerPrefs.GetString(SelectedSettingsKey);
                }

                SaveSelectedSettings(_demoMouseInteractionAssetPath);
            }
            else if (!string.IsNullOrEmpty(_currentSettingsConfigPath))
            {
                Debug.Log("Leaving the demo scene. Setting previous socket settings.");
                SaveSelectedSettings(_currentSettingsConfigPath);
                _currentSettingsConfigPath = string.Empty;
            }
        }

        public static void SaveSelectedSettings(string settingsPath)
        {
            PlayerPrefs.SetString(SelectedSettingsKey, settingsPath);
            PlayerPrefs.Save();
            Debug.Log("Saved Ultimate Socket settings to path: " + settingsPath);
        }
#endif
    }
}
