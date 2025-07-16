using Hypertonic.Modules.UltimateSockets.Editor.Utilities;
using Hypertonic.Modules.UltimateSockets.Enums;
using Hypertonic.Modules.UltimateSockets.Highlighters;
using Hypertonic.Modules.UltimateSockets.Models;
using Hypertonic.Modules.UltimateSockets.Models.ScriptableObjects;
using Hypertonic.Modules.XR.PlacePoints.UltimateSockets.Models.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Editor
{
    [CustomEditor(typeof(SocketHologram))]
    public class SocketHologramEditor : UnityEditor.Editor
    {
        private UltimateSocketSettings _ultimateSocketSettings;

        private SocketHologram _socketHologram;

        private SerializedProperty _hologramMode;

        private SerializedProperty _hologramGameObject;
        private SerializedProperty _hologramPrefab;
        private SerializedProperty _placeableHologramMaterial;
        private SerializedProperty _nonPlaceableHologramMaterial;
        private SerializedProperty _itemSpecificHologramPrefabSettings;

        private SerializedProperty _itemSpecificDefaultPrefab;
        private SerializedProperty _itemSpecificDefaultItemType;


        private void OnEnable()
        {
            _socketHologram = target as SocketHologram;

            if (_socketHologram == null)
            {
                return;
            }

            CheckAndAssignReferences();

            LoadSettings();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHologramModeDropdown();

            DetermineSettingsToDraw();

            serializedObject.ApplyModifiedProperties();
        }

        private void LoadSettings()
        {
            _ultimateSocketSettings = UltimateSocketUtilities.LoadSettings();
        }
        private void DetermineSettingsToDraw()
        {
            switch (_socketHologram.HologramMode)
            {
                case HologramMode.Dynamic: DrawDynamicSettings(); break;
                case HologramMode.ItemSpecific: DrawItemSpecificSettings(); break;
                case HologramMode.GameObject: DrawGameObjectSettings(); break;
                case HologramMode.SinglePrefab: DrawPrefabSettings(); break;
                case HologramMode.None: break;
                default:
                    Debug.LogErrorFormat(this, "Unknown hologram mode: [{0}]", _socketHologram.HologramMode); break;
            }
        }

        private void DrawHologramModeDropdown()
        {
            _hologramMode.enumValueIndex = EditorGUILayout.Popup("Hologram Mode", _hologramMode.enumValueIndex, _hologramMode.enumDisplayNames);
        }

        private void DrawDynamicSettings()
        {
            // No need to set a prefab. It'll be created based on the item tag
            DrawHologramMaterialField();
        }

        private void DrawItemSpecificSettings()
        {
            EditorGUILayout.PropertyField(_itemSpecificHologramPrefabSettings);

            if (_itemSpecificHologramPrefabSettings.objectReferenceValue == null)
            {
                DrawCreateHologramPrefabSettingsButton();
            }
            else
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Choose how to handle what happens if an item moves into hologram range but hasn't got a prefab assigned below. You can either create one dynamically based on the item entering the area, or define a prefab to use as default.", MessageType.None);
                EditorGUILayout.PropertyField(_itemSpecificDefaultItemType);

                EditorGUILayout.Space();
                if (_itemSpecificDefaultItemType.enumValueIndex == (int)ItemSpecificDefaultItemType.DefaultPrefab)
                {
                    EditorGUILayout.PropertyField(_itemSpecificDefaultPrefab);
                }

                DrawHologramPrefabSettingsEditor();
            }
        }

        private void DrawCreateHologramPrefabSettingsButton()
        {
            EditorGUILayout.Space();

            if (GUILayout.Button("Create Hologram Prefab Settings"))
            {
                CreateHologramPrefabSettings();
            }
        }

        private void CreateHologramPrefabSettings()
        {
            string defaultFileName = "NewHologramPrefabSettings.asset";
            string path = EditorUtility.SaveFilePanel("Save Hologram Prefab Settings", "Assets", defaultFileName, "asset");

            if (!string.IsNullOrEmpty(path))
            {
                path = FileUtil.GetProjectRelativePath(path);

                HologramPrefabSettings newSettings = ScriptableObject.CreateInstance<HologramPrefabSettings>();
                AssetDatabase.CreateAsset(newSettings, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorGUIUtility.PingObject(newSettings);

                _itemSpecificHologramPrefabSettings.objectReferenceValue = newSettings;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawHologramPrefabSettingsEditor()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();

            HologramPrefabSettings hologramPrefabSettings = _itemSpecificHologramPrefabSettings.objectReferenceValue as HologramPrefabSettings;

            if (hologramPrefabSettings != null)
            {
                EditorGUILayout.HelpBox("Assign a prefab for each placeable item tag. The prefab will be used as the hologram when the item is being placed.", MessageType.None);
                EditorGUILayout.Space();

                PlaceableItemTags placeableItemTags = _ultimateSocketSettings.PlaceableItemTags;

                if (placeableItemTags != null)
                {
                    foreach (string itemTag in placeableItemTags.Tags)
                    {
                        DrawHologramPrefabRow(hologramPrefabSettings, itemTag);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No placeable item tags found. Please define them in the Ultimate Sockets Settings window.", MessageType.Warning);
                }
            }
        }

        private void DrawHologramPrefabRow(HologramPrefabSettings hologramPrefabSettings, string itemTag)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(itemTag, GUILayout.Width(150));

            HologramPrefabConfig hologramPrefabConfig = hologramPrefabSettings.HologramPrefabConfigs.Find(x => x.ItemTag == itemTag);

            GameObject oldPrefab = hologramPrefabConfig != null ? hologramPrefabConfig.Prefab : null;
            GameObject newPrefab = (GameObject)EditorGUILayout.ObjectField(oldPrefab, typeof(GameObject), false);

            if (oldPrefab != newPrefab)
            {
                if (hologramPrefabConfig == null)
                {
                    hologramPrefabConfig = new HologramPrefabConfig { ItemTag = itemTag };
                    hologramPrefabSettings.HologramPrefabConfigs.Add(hologramPrefabConfig);
                }

                hologramPrefabConfig.Prefab = newPrefab;
                EditorUtility.SetDirty(hologramPrefabSettings);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGameObjectSettings()
        {
            EditorGUILayout.PropertyField(_hologramGameObject);
        }

        private void DrawPrefabSettings()
        {
            EditorGUILayout.PropertyField(_hologramPrefab);
        }

        private void DrawHologramMaterialField()
        {
            EditorGUILayout.PropertyField(_placeableHologramMaterial);
            EditorGUILayout.PropertyField(_nonPlaceableHologramMaterial);
        }

        private void CheckAndAssignReferences()
        {
            _hologramMode = serializedObject.FindProperty("_hologramMode");

            if (_hologramMode == null)
            {
                Debug.LogErrorFormat(this, "Could not find hologram mode property on [{0}]", _socketHologram.name);
            }

            _hologramGameObject = serializedObject.FindProperty("_hologramGameObject");

            if (_hologramGameObject == null)
            {
                Debug.LogErrorFormat(this, "Could not find hologram game object property on [{0}]", _socketHologram.name);
            }

            _hologramPrefab = serializedObject.FindProperty("_hologramPrefab");

            if (_hologramPrefab == null)
            {
                Debug.LogErrorFormat(this, "Could not find hologram prefab property on [{0}]", _socketHologram.name);
            }

            _placeableHologramMaterial = serializedObject.FindProperty("_placeableHologramMaterial");

            if (_placeableHologramMaterial == null)
            {
                Debug.LogErrorFormat(this, "Could not find placeable hologram material property on [{0}]", _socketHologram.name);
            }

            _nonPlaceableHologramMaterial = serializedObject.FindProperty("_nonPlaceableHologramMaterial");

            if (_nonPlaceableHologramMaterial == null)
            {
                Debug.LogErrorFormat(this, "Could not find non placeable hologram material property on [{0}]", _socketHologram.name);
            }

            _itemSpecificHologramPrefabSettings = serializedObject.FindProperty("_itemSpecificHologramPrefabSettings");

            if (_itemSpecificHologramPrefabSettings == null)
            {
                Debug.LogErrorFormat(this, "Could not find the item specific hologram prefab settings property on [{0}]", _socketHologram.name);
            }

            _itemSpecificDefaultPrefab = serializedObject.FindProperty("_itemSpecificDefaultPrefab");

            if (_itemSpecificDefaultPrefab == null)
            {
                Debug.LogErrorFormat(this, "Could not find item specific default prefab property on [{0}]", _socketHologram.name);
            }

            _itemSpecificDefaultItemType = serializedObject.FindProperty("_itemSpecificDefaultItemType");

            if (_itemSpecificDefaultItemType == null)
            {
                Debug.LogErrorFormat(this, "Could not find item specific default item type property on [{0}]", _socketHologram.name);
            }
        }
    }
}
