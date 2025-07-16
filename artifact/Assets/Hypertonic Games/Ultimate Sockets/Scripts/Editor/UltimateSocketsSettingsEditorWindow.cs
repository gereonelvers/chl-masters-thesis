using UnityEngine;
using UnityEditor;
using Hypertonic.Modules.XR.PlacePoints.UltimateSockets.Models.ScriptableObjects;
using Hypertonic.Modules.UltimateSockets.Models;
using Hypertonic.Modules.UltimateSockets.Editor.Utilities;
using System.Collections.Generic;
using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using Hypertonic.Modules.Utilities;
using System;
using System.IO;

namespace Hypertonic.Modules.UltimateSockets.Editor
{
    public class UltimateSocketsSettingsEditorWindow : EditorWindow
    {
        public delegate void UltimateSockeSettingsEvent();
        public static event UltimateSockeSettingsEvent OnUltimateSocketSettingsChanged;

        private const string _selectedSettingsKey = "ULTIMATE_SOCKETS_SETTINGS_KEY";
        private const int _tags_per_page = 20;

        private static UltimateSocketSettings _settings;

        // Used to check if the settings have changed
        private PlaceableItemTags _previousPlaceableItemTags;
        private static UnityEditor.Editor _placeableItemTagEditor;

        private readonly string[] _tabs = { "Settings", "Tags", "Posing Prefabs", };

        private int _tabIndex = 0;

        private int _currentTagPage = 0;
        private bool _overrideExistingPosingPrefab = true;
        private string _posingObjectPrefabName = string.Empty;
        private GameObject _posingObjectSource;

        private string _posingObjectSaveFilePath;

        private void OnEnable()
        {
            LoadSettings();

            if (_settings == null)
            {
                return;
            }

            UpdatePlaceableTagEditor();
        }

        private void OnDisable()
        {
            // Save the selected settings to EditorPrefs when the window is disabled
            if (_settings != null)
            {
                string settingsPath = AssetDatabase.GetAssetPath(_settings);
                EditorPrefs.SetString(_selectedSettingsKey, settingsPath);
            }
            else
            {
                EditorPrefs.DeleteKey(_selectedSettingsKey);
            }
        }

        [MenuItem("Tools/Hypertonic/Ultimate Sockets Settings")]
        public static void ShowWindow()
        {
            UltimateSocketsSettingsEditorWindow window = GetWindow<UltimateSocketsSettingsEditorWindow>();
            window.titleContent = new GUIContent("UltimateSockets Settings");
            window.Show();
        }

        private void OnGUI()
        {
            if (_settings == null || _settings.PlaceableItemTags == null)
            {
                DrawSettingsTab();
                return;
            }

            DrawTabs();

            DetermineTabToDraw();

            if (_settings != null)
            {
                EditorUtility.SetDirty(_settings);
            }
        }

        private void LoadSettings()
        {
            _settings = UltimateSocketUtilities.LoadSettings();

            if (_settings == null)
            {
                Debug.LogWarning("No settings found");
                return;
            }
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginVertical();

            _tabIndex = GUILayout.Toolbar(_tabIndex, _tabs);

            EditorGUILayout.EndVertical();
        }

        private void DetermineTabToDraw()
        {
            switch (_tabIndex)
            {
                case 0: DrawSettingsTab(); break;
                case 1: DrawTagsTab(); break;
                case 2: DrawPosingTab(); break;
            }
        }

        private void DrawSettingsTab()
        {
            DrawSettingsFieldRegion();

            if (_settings == null)
            {
                DrawCreateNewSettingsButton();
                return;
            }

            DrawPlaceableItemTagRegion();

            if (_settings.PlaceableItemTags == null)
            {
                DrawCreateNewPlaceableItemTagsButton();
                return;
            }

            if (_placeableItemTagEditor == null)
            {
                _placeableItemTagEditor = UnityEditor.Editor.CreateEditor(_settings.PlaceableItemTags);
            }

            DrawCheckPlaceableItemsHaveValidTagsRegion();
        }

        private void DrawSettingsFieldRegion()
        {
            EditorGUILayout.Space();

            UltimateSocketSettings settings = (UltimateSocketSettings)EditorGUILayout.ObjectField(
                         "Ultimate Socket Settings",
                         _settings,
                         typeof(UltimateSocketSettings),
                         false
                     );

            EditorGUILayout.Space();

            if (_settings != settings)
            {
                UltimateSocketUtilities.SaveSettings(settings);

                OnUltimateSocketSettingsChanged?.Invoke();

                UpdatePlaceableTagEditor();
            }

            _settings = settings;
        }

        private void DrawCreateNewSettingsButton()
        {
            if (GUILayout.Button("Create New Settings"))
            {
                string path = EditorUtility.SaveFilePanelInProject("Create New Ultimate Socket Settings", "Ultimate Socket Settings", "asset", "Create new settings asset for Ultimate Sockets");

                if (!string.IsNullOrEmpty(path))
                {
                    _settings = ScriptableObject.CreateInstance<UltimateSocketSettings>();
                    AssetDatabase.CreateAsset(_settings, path);
                    AssetDatabase.SaveAssets();

                    UltimateSocketUtilities.SaveSettings(_settings);

                    OnUltimateSocketSettingsChanged?.Invoke();
                }
            }
        }

        private void DrawPlaceableItemTagRegion()
        {
            EditorGUILayout.Space();

            _settings.PlaceableItemTags = (PlaceableItemTags)EditorGUILayout.ObjectField(
                "Placeable Item Tags",
                _settings.PlaceableItemTags,
                typeof(PlaceableItemTags),
                false
            );

            EditorGUILayout.Space();

            if (_settings.PlaceableItemTags != _previousPlaceableItemTags)
            {
                OnUltimateSocketSettingsChanged?.Invoke();

                UpdatePlaceableTagEditor();
            }

            _previousPlaceableItemTags = _settings.PlaceableItemTags;
        }

        private void DrawCreateNewPlaceableItemTagsButton()
        {
            if (GUILayout.Button("Create Placeable Item Tag Settings"))
            {
                string path = EditorUtility.SaveFilePanelInProject("Create New Placeable Item Socket Settings", "Placeable Item Tag Settings", "asset", "Create new settings asset for Ultimate Sockets Placeable Item Tags");

                if (!string.IsNullOrEmpty(path))
                {
                    _settings.PlaceableItemTags = ScriptableObject.CreateInstance<PlaceableItemTags>();
                    AssetDatabase.CreateAsset(_settings.PlaceableItemTags, path);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        private void DrawCheckPlaceableItemsHaveValidTagsRegion()
        {
            if (GUILayout.Button("Check Placeable Items Have Valid Tags"))
            {
                CheckPlaceableItemsHaveValidTags();
            }
        }

        private void CheckPlaceableItemsHaveValidTags()
        {
            // Get a list of tags
            List<string> tags = _settings.PlaceableItemTags.Tags;

            // Find all of the placeable items in the scene.
            PlaceableItem[] placeableItems = FindObjectsOfType<PlaceableItem>();

            bool invalidTagFound = false;
            // Check each placeable item has a tag in the tag list
            foreach (PlaceableItem placeableItem in placeableItems)
            {
                if (!tags.Contains(placeableItem.ItemTag))
                {
                    Debug.LogErrorFormat(placeableItem, "The placeable item doesn't have a valid tag.");

                    invalidTagFound = true;
                }
            }

            string message;

            if (invalidTagFound)
            {
                message = "One or more placeable items don't have a valid tag. Please add a tag to the Placeable Item Tags list.";
            }
            else
            {
                message = "All placeable items have valid tags.";
            }

            EditorUtility.DisplayDialog("Ultimate Sockets", message, "Ok");
        }

        private void UpdatePlaceableTagEditor()
        {
            if (_settings != null && _settings.PlaceableItemTags != null)
            {
                _placeableItemTagEditor = UnityEditor.Editor.CreateEditor(_settings.PlaceableItemTags);
            }
        }

        private void DrawTagsTab()
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Define the placeable items in the project.", MessageType.None, true);
            EditorGUILayout.Space();

            bool tagsModified = false;

            if (_settings.PlaceableItemTags == null || _settings.PlaceableItemTags.Tags == null)
            {
                EditorGUILayout.LabelField("No tags available.");
                return;
            }

            int totalTags = _settings.PlaceableItemTags.Tags.Count;
            int totalPages = Mathf.CeilToInt((float)totalTags / _tags_per_page);

            // Display paginated tags
            int startIndex = _currentTagPage * _tags_per_page;
            int endIndex = Mathf.Min(startIndex + _tags_per_page, totalTags);

            for (int i = startIndex; i < endIndex; i++)
            {
                EditorGUILayout.BeginHorizontal();
                string currentTag = _settings.PlaceableItemTags.Tags[i];
                string newTag = EditorGUILayout.TextField(_settings.PlaceableItemTags.Tags[i]);

                if (newTag != currentTag)
                {
                    _settings.PlaceableItemTags.Tags[i] = newTag;
                    tagsModified = true;
                }

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    _settings.PlaceableItemTags.Tags.RemoveAt(i);
                    i--;
                    endIndex--;
                    tagsModified = true;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space();

            // Pagination controls
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Previous", GUILayout.Width(100)) && _currentTagPage > 0)
            {
                _currentTagPage--;
            }

            EditorGUILayout.LabelField($"Page {_currentTagPage + 1} of {totalPages}", EditorStyles.centeredGreyMiniLabel);

            if (GUILayout.Button("Next", GUILayout.Width(100)) && _currentTagPage < totalPages - 1)
            {
                _currentTagPage++;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (GUILayout.Button("Add New Tag"))
            {
                _settings.PlaceableItemTags.Tags.Add("New Tag");
                tagsModified = true;
            }

            if (tagsModified)
            {
                EditorUtility.SetDirty(_settings.PlaceableItemTags);
                AssetDatabase.SaveAssets();
            }
        }

        private void DrawPosingTab()
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Assign a prefab for the placeable item tags. " +
            "If populated the prefab will be shown when positioning the Item. If blank a defaut object will be shown instead ", MessageType.None, true);
            EditorGUILayout.Space();

            EditorGUILayout.Space();

            DrawGeneratePlaceableItemRegion();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Posing List", EditorStyles.boldLabel);

            for (int i = 0; i < _settings.PlaceableItemTags.Tags.Count; i++)
            {
                DrawPosingRow(_settings.PlaceableItemTags.Tags[i]);
            }
        }

        private void DrawPosingRow(string itemTag)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(itemTag, GUIStyles.SettingsWindowGrid);

            PlaceableItemPosingData placeableItemPosingData = _settings.PlaceableItemPosingDatas.Find(x => x.ItemTag.Equals(itemTag));

            GameObject oldPosingPrefab = null;

            if (placeableItemPosingData != null)
            {
                oldPosingPrefab = placeableItemPosingData.PosingPrefab;
            }

            GameObject objectFieldRes = (GameObject)EditorGUILayout.ObjectField(oldPosingPrefab, typeof(GameObject), true);


            if (oldPosingPrefab != objectFieldRes)
            {
                if (placeableItemPosingData == null)
                {
                    _settings.PlaceableItemPosingDatas.Add(new PlaceableItemPosingData()
                    {
                        ItemTag = itemTag,
                        PosingPrefab = objectFieldRes
                    });
                }
                else
                {
                    placeableItemPosingData.PosingPrefab = objectFieldRes;
                }
            }

            EditorGUILayout.EndHorizontal();
        }


        private void DrawGeneratePlaceableItemRegion()
        {
            EditorGUILayout.LabelField("Posing Object Creation Utility", EditorStyles.boldLabel);

            DrawOverrideExistingPosingPrefabOption();

            DrawPosingObjectNameField();

            DrawPosingObjectSavePathField();

            DrawPosingObjectField();

            DrawGeneratePosingObjectButton();
        }

        private void DrawOverrideExistingPosingPrefabOption()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Override Existing Prefab");
            _overrideExistingPosingPrefab = EditorGUILayout.Toggle(_overrideExistingPosingPrefab);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPosingObjectNameField()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Posing Object Name");
            _posingObjectPrefabName = EditorGUILayout.TextField(GetPosingObjectPrefabName());
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPosingObjectSavePathField()
        {
            EditorGUILayout.BeginHorizontal();
            string posingObjectSaveFilePath = EditorGUILayout.TextField("Save Path", _posingObjectSaveFilePath);

            if (GUILayout.Button("Browse", GUILayout.MaxWidth(80)))
            {
                posingObjectSaveFilePath = EditorUtility.OpenFolderPanel("Select Save Path", posingObjectSaveFilePath, "");

                // Only modify the path if it's within the Assets folder
                if (posingObjectSaveFilePath.Contains(Application.dataPath))
                {
                    posingObjectSaveFilePath = "Assets" + posingObjectSaveFilePath.Substring(Application.dataPath.Length);
                }
            }

            _posingObjectSaveFilePath = posingObjectSaveFilePath;
            EditorGUILayout.EndHorizontal();
        }


        private void DrawPosingObjectField()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Posing Object Source");
            _posingObjectSource = EditorGUILayout.ObjectField(_posingObjectSource, typeof(GameObject), true) as GameObject;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGeneratePosingObjectButton()
        {
            bool preivewObjectExistsInFileLocation = PosingObjectExists(GetPosingObjectPrefabFilePath());

            bool disableGeneratePosingObjectButton = preivewObjectExistsInFileLocation && !_overrideExistingPosingPrefab;

            EditorGUILayout.Space();

            if (disableGeneratePosingObjectButton)
            {
                EditorGUILayout.HelpBox("A posing object already exists with that name at the specified path. To generate a new posing object, delete the existing one first, change the posing object name, or tick the Override Exisiting Prefab tick box.", MessageType.Info);
            }

            if (preivewObjectExistsInFileLocation && _overrideExistingPosingPrefab)
            {
                EditorGUILayout.HelpBox("A posing object already exists with that name at the specified path. The existing posing object will be overwritten.", MessageType.Warning);
            }


            EditorGUI.BeginDisabledGroup(disableGeneratePosingObjectButton || _posingObjectSource == null || string.IsNullOrEmpty(_posingObjectSaveFilePath));

            if (GUILayout.Button("Generate Posing Object"))
            {
                GeneratePosingObject();
            }

            EditorGUI.EndDisabledGroup();
        }

        private void GeneratePosingObject()
        {
            GameObject posingObject = ComponentStripperManager.DuplicateAndStrip(_posingObjectSource, new List<Type>() { typeof(MeshFilter), typeof(MeshRenderer), typeof(SkinnedMeshRenderer) });

            posingObject.name = "NEW POSING OBJECT";

            PrefabUtility.SaveAsPrefabAsset(posingObject, GetPosingObjectPrefabFilePath());



            AssetDatabase.Refresh();

            GameObject posingObjectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GetPosingObjectPrefabFilePath());
            EditorGUIUtility.PingObject(posingObjectPrefab);
            if (posingObjectPrefab == null)
            {
                Debug.LogError("The posing prefab just created could not be loaded.");
                return;
            }

            DestroyImmediate(posingObject);
        }

        private string GetPosingObjectPrefabFilePath()
        {
            if (string.IsNullOrEmpty(_posingObjectSaveFilePath))
            {
                return string.Empty;
            }

            return Path.Combine(_posingObjectSaveFilePath, GetPosingObjectPrefabName() + ".prefab");
        }


        private bool PosingObjectExists(string prefabPath)
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            return existingPrefab != null;
        }

        private string GetPosingObjectPrefabName()
        {
            if (!string.IsNullOrEmpty(_posingObjectPrefabName))
            {
                return _posingObjectPrefabName;
            }

            return "Placeable Item Posing Object";
        }
    }
}