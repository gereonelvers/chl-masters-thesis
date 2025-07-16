using System.Collections.Generic;
using Hypertonic.Modules.UltimateSockets.Sockets;
using Hypertonic.Modules.UltimateSockets.Sockets.Stacking;
using Hypertonic.Modules.XR.PlacePoints.UltimateSockets.Models.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Editor.Utilities
{
    public static class UltimateSocketUtilities
    {
        public const string SelectedSettingsKey = "ULTIMATE_SOCKET_SELECTED_SETTINGS_PATH_KEY";

        public static UltimateSocketSettings LoadSettings()
        {
            string settingsPath = PlayerPrefs.GetString(SelectedSettingsKey);

            if (string.IsNullOrEmpty(settingsPath))
            {
                Debug.Log("No ultimate socket settings have been selected.");
                return null;
            }

            UltimateSocketSettings ultimateSocketSettings = AssetDatabase.LoadAssetAtPath<UltimateSocketSettings>(settingsPath);

            if (ultimateSocketSettings == null)
            {
                Debug.LogWarning("UltimateSocketSettings not found at path: " + settingsPath);
                return null;
            }

            return ultimateSocketSettings;
        }


        public static List<string> GetTags(UltimateSocketSettings ultimateSocketSettings)
        {
            return ultimateSocketSettings.PlaceableItemTags.Tags;
        }

        public static List<string> GetTags()
        {
            UltimateSocketSettings ultimateSocketSettings = LoadSettings();

            if (ultimateSocketSettings == null)
            {
                Debug.LogError("No Ultimate Socket settings found. Please set the settings in the Ultimate Socket Settings window.");
                return new List<string>();
            }

            return GetTags(ultimateSocketSettings);
        }

        public static void SaveSettings(UltimateSocketSettings settings)
        {
            if (settings == null)
            {
                PlayerPrefs.DeleteKey(SelectedSettingsKey);
                PlayerPrefs.Save();
                Debug.Log("Cleared Ultimate Socket settings.");
            }

            string settingsPath = AssetDatabase.GetAssetPath(settings);

            if (string.IsNullOrEmpty(settingsPath))
            {
                Debug.LogWarning("Failed to save Ultimate Socket settings. Asset path is empty.");
                return;
            }

            SaveSelectedSettings(settingsPath);
        }

        public static void SaveSelectedSettings(string settingsPath)
        {
            PlayerPrefs.SetString(SelectedSettingsKey, settingsPath);
            PlayerPrefs.Save();
            Debug.Log("Saved Ultimate Socket settings to path: " + settingsPath);
        }

        public static GameObject CreateStackUIAsync(Socket socket)
        {
            string[] guids = AssetDatabase.FindAssets("[UltimateSockets] Stack Count UI t:Prefab", new[] { "Assets" });

            if (guids.Length == 0)
            {
                Debug.LogError("Failed to find Stack Count UI prefab.");
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject stackCountUIPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (stackCountUIPrefab == null)
            {
                Debug.LogError("Failed to load Stack Count UI prefab.");
                return null;
            }

            GameObject stackCountUI = GameObject.Instantiate(stackCountUIPrefab);

            stackCountUI.name = "Socket | Stack Count UI";
            stackCountUI.transform.SetParent(socket.transform, false);

            stackCountUI.transform.localPosition = Vector3.zero;
            stackCountUI.transform.localRotation = stackCountUIPrefab.transform.rotation;


            Vector3 realScale = stackCountUI.transform.lossyScale;

            if (realScale.x < 1f || realScale.y < 1f || realScale.z < 1f)
            {
                float scaleFactor = Mathf.Min(1f / realScale.x, 1f / realScale.y, 1f / realScale.z);

                stackCountUI.transform.localScale *= scaleFactor;
            }

            if (!stackCountUI.TryGetComponent(out StackItemCountDisplayController stackItemCountDisplayController))
            {
                Debug.LogError("Failed find the stack item count display controller on the Stack Count UI.");
                return null;
            }

            stackItemCountDisplayController.SetSocket(socket);

            return stackCountUI;
        }
    }
}
