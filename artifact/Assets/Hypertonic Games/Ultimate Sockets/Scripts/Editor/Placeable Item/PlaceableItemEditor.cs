using Hypertonic.Modules.UltimateSockets.Models;
using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using Hypertonic.Modules.UltimateSockets.PlaceableItems.PlacementCriterias;
using Hypertonic.Modules.UltimateSockets.PlaceableItems.Stacking;
using Hypertonic.Modules.UltimateSockets.XR;
using Hypertonic.Modules.Utilities;
using Hypertonic.Modules.XR.PlacePoints.UltimateSockets.Models.ScriptableObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Hypertonic.Modules.UltimateSockets.Extensions;
using Hypertonic.Modules.UltimateSockets.Enums;
using Hypertonic.Modules.UltimateSockets.Interfaces;
using Hypertonic.Modules.UltimateSockets.Editor.Utilities;
using Hypertonic.Modules.UltimateSockets.Sockets;
using System.Threading.Tasks;

namespace Hypertonic.Modules.UltimateSockets.Editor.PlaceableItems
{
    [CustomEditor(typeof(PlaceableItem))]
    [CanEditMultipleObjects]
    public class PlaceableItemEditor : UnityEditor.Editor
    {
        #region General Variables

        private PlaceableItem _placeableItem;
        private UltimateSocketSettings _ultimateSocketSettings;

        private readonly string[] _tabs = { "Settings", "Preview Object", "Placement Criteria", "Stacking", "Integrations" };

        private SerializedProperty _tabIndexProperty;

        private string[] _tags = new string[0];

        private Color _defaultGUIBackgroundColour = Color.black;
        private Color _regionBackgroundColour = new Color(0.8f, 0.8f, 0.8f);

        // Used to prevent redrawing error log when settings change from null.
        bool _shouldDelayDrawingCustomInspectorGUI = false;

        private static DateTime _dateTimeLastPrefabSourceWindowShown;

        #endregion General Variables




        #region Settings Tab Variables

        private SerializedProperty _rootTransform;

        private SerializedProperty _exitPlaceZoneThreshold;
        private SerializedProperty _exitPlaceZoneMethod;
        private SerializedProperty _destroyOnPlace;
        private SerializedProperty _clearParentWhenRemoved;
        private SerializedProperty _keepScaleForDefaultPlacements;

        private SerializedObject _rigidBodySerializedObject;
        private SerializedProperty _rigidbody;
        private SerializedProperty _placeableRigidbodySettings;

        private SerializedObject _placeableItemGrabbableSerializedObject;
        private SerializedProperty _grabbableItem;

        private PlaceableItemGrabbable _placeableItemGrabbable;
        private PlaceableItemRigidbody _placeableItemRigidbody;

        private bool _adjustPlaceableItemCollider = false;

        private SerializedObject _placeableItemColliderSerializedObject;
        private bool _adjustXRGrabCollider = false;


        #endregion Settings Tab Variables

        #region Preview Object Tab Variables

        private SerializedObject _placeableItemPreviewControllerSerializedObject;
        private PlaceableItemPreviewController _previewObjectController;
        private SerializedProperty _usePreviewController;
        private SerializedProperty _useRuntimePreviewObject;
        private SerializedProperty _previewObject;
        private SerializedProperty _placeSpeed;
        private SerializedProperty _unplaceSpeed;
        private SerializedProperty _placeSpeedAnimationCurve;
        private SerializedProperty _unplaceSpeedAnimationCurve;

        private bool _overrideExistingPreviewPrefab;

        private string _previewObjectPrefabName = string.Empty;

        private SerializedProperty _socketReferenceProperty;

        bool _previewItemSaveLocationWindowOpen = false;

        #endregion Preview Object Tab Variables

        #region Placement Criteria Tab Variables

        private Dictionary<MonoBehaviour, UnityEditor.Editor> _placementCriteriaEditors = new Dictionary<MonoBehaviour, UnityEditor.Editor>();

        #endregion Placement Criteria Tab Variables

        #region Stacking Tab Variables
        private SerializedObject _stackableControllerSerializedObject;
        private SerializedProperty _stackableSerializedProperty;

        private StackableItemController _stackableController;

        private Dictionary<MonoBehaviour, UnityEditor.Editor> _stackingSpawnTransitionEditors = new Dictionary<MonoBehaviour, UnityEditor.Editor>();

        #endregion Stacking Tab Variables

        #region References Tab Variables

        private SerializedProperty _socketGrabCollider;
        private SerializedProperty _socketDetectorCollider;
        private SerializedProperty _placementCriteriaContainer;

        #endregion References Tab Variables    




        #region Unity Functions

        private void OnEnable()
        {
            _placeableItem = target as PlaceableItem;

            _defaultGUIBackgroundColour = GUI.backgroundColor;

            CheckAndAssignReferences();

            UltimateSocketsSettingsEditorWindow.OnUltimateSocketSettingsChanged += HandleUltimateSocketSettingsChanged;

            LoadSettings();
        }

        private void OnDisable()
        {
            UltimateSocketsSettingsEditorWindow.OnUltimateSocketSettingsChanged -= HandleUltimateSocketSettingsChanged;
        }

        public override void OnInspectorGUI()
        {
            if (_ultimateSocketSettings == null || _ultimateSocketSettings.PlaceableItemTags == null)
            {
                DrawNoSettingsSetRegion();
                LoadSettings();
                Repaint();
                _shouldDelayDrawingCustomInspectorGUI = true;
                return;
            }

            if (_shouldDelayDrawingCustomInspectorGUI)
            {
                _shouldDelayDrawingCustomInspectorGUI = false;
                return;
            }

            // Focus check - this indicates the save dialog closed
            if (Event.current.type == EventType.Repaint && _previewItemSaveLocationWindowOpen)
            {
                Repaint();
                _previewItemSaveLocationWindowOpen = false;
                return;
            }

            if (_tags == null || _tags.Length == 0)
            {
                _tags = _ultimateSocketSettings.PlaceableItemTags.Tags.ToArray();
            }

            if (string.IsNullOrEmpty(_placeableItem.ItemTag))
            {
                DrawNoItemTagSet();
                return;
            }

            serializedObject.Update();
            _rigidBodySerializedObject?.Update();
            _placeableItemGrabbableSerializedObject?.Update();
            _placeableItemPreviewControllerSerializedObject?.Update();
            _stackableControllerSerializedObject?.Update();

            if (IsInPrefabOverrideMenu())
            {
                DrawNonInspectorUI();
            }
            else
            {
                DrawInspectorUI();
            }

            serializedObject.ApplyModifiedProperties();
            _rigidBodySerializedObject?.ApplyModifiedProperties();
            _placeableItemGrabbableSerializedObject?.ApplyModifiedProperties();
            _placeableItemPreviewControllerSerializedObject?.ApplyModifiedProperties();
            _stackableControllerSerializedObject?.ApplyModifiedProperties();
            DisplayUnsocketOption();
        }

        protected virtual void OnSceneGUI()
        {
            if (_adjustPlaceableItemCollider)
            {
                ColliderConfigurationEditorUtilities.AdjustItemDetectionCollider(_placeableItem.PlaceableItemCollider.Collider, () => Repaint());
            }

            if (_adjustXRGrabCollider)
            {
                ColliderConfigurationEditorUtilities.AdjustItemDetectionCollider(_placeableItem.SocketGrabCollider.Collider, () => Repaint());
            }
        }

        #endregion Unity Functions

        #region Context Menu Functions

        [MenuItem("GameObject/Hypertonic/Ultimate Sockets/Add Placeable Item", false, 10)]
        public static void AddPlaceableItemGameObject()
        {
            GameObject currentSelection = Selection.activeGameObject;
            GameObject placeableItemGameObject = new GameObject("Placeable Item");
            placeableItemGameObject.transform.SetParent(currentSelection.transform, false);

            PlaceableItem placeableItem = placeableItemGameObject.AddComponent<PlaceableItem>();

            placeableItem.SetRootTransform(currentSelection.transform);

            InstantiatePlaceableItemComponents(placeableItem);

            Selection.activeGameObject = placeableItemGameObject;
        }

        #endregion Context Menu Functions

        #region General Functions

        public static void InstantiatePlaceableItemComponents(PlaceableItem placeableItem)
        {
            PlaceableItemInstantiator.InstantiatePlaceableItemComponents(placeableItem);
        }

        private void HandleUltimateSocketSettingsChanged()
        {
            if (_ultimateSocketSettings != null)
            {
                _tags = UltimateSocketUtilities.GetTags(_ultimateSocketSettings).ToArray();
            }
            else
            {
                _tags = new string[0];
            }

            _ = RepaintAsync();
        }

        private async Task RepaintAsync()
        {
            // Inspector requires a second repaint to update the UI
            Repaint();
            await Task.Delay(100);
            Repaint();
        }

        private void LoadSettings()
        {
            _ultimateSocketSettings = UltimateSocketUtilities.LoadSettings();
        }

        private void DrawNoSettingsSetRegion()
        {
            if (_ultimateSocketSettings == null)
            {
                EditorGUILayout.HelpBox("No Ultimate Socket settings set. Please set the settings in the Ultimate Socket Settings window.", MessageType.Warning);
            }
            else if (_ultimateSocketSettings.PlaceableItemTags == null)
            {
                EditorGUILayout.HelpBox("No Ultimate Socket Tag settings have been set. Please set the settings in the Ultimate Socket Settings window.", MessageType.Warning);
            }

            if (GUILayout.Button("Open Ultimate Socket Settings"))
            {
                UltimateSocketsSettingsEditorWindow.ShowWindow();
            }
        }

        private void DrawNoItemTagSet()
        {
            EditorLayoutUtilities.DrawTopOfSection("Core Settings");

            DisplayTagDropDown(_placeableItem);

            EditorLayoutUtilities.DrawBottomOfSection();
        }

        private void CheckAndAssignReferences()
        {
            _tabIndexProperty = serializedObject.FindProperty("_tabIndex");

            if (_tabIndexProperty == null)
            {
                Debug.LogError("Failed to find the property _tabIndexProperty on the placeable item component");
                return;
            }

            _rootTransform = serializedObject.FindProperty("_rootTransform");

            if (_rootTransform == null)
            {
                Debug.LogError("Failed to find the property _rootTransform on the placeable item component");
                return;
            }

            _socketGrabCollider = serializedObject.FindProperty("_socketGrabCollider");

            if (_socketGrabCollider == null)
            {
                Debug.LogError("Failed to find the property _socketGrabCollider on the placeable item component");
                return;
            }

            _socketDetectorCollider = serializedObject.FindProperty("_socketDetectorCollider");

            if (_socketDetectorCollider == null)
            {
                Debug.LogError("Failed to find the property _socketDetectorCollider on the placeable item component");
                return;
            }

            _placementCriteriaContainer = serializedObject.FindProperty("_placementCriteriaContainer");

            if (_placementCriteriaContainer == null)
            {
                Debug.LogError("Failed to find the property _placementCriteriaContainer on the placeable item component");
                return;
            }

            _exitPlaceZoneThreshold = serializedObject.FindProperty("_exitPlaceZoneThreshold");

            if (_exitPlaceZoneThreshold == null)
            {
                Debug.LogError("Failed to find the property _exitPlaceZoneThreshold on the placeable item component");
                return;
            }

            _exitPlaceZoneMethod = serializedObject.FindProperty("_exitPlaceZoneMethod");

            if (_exitPlaceZoneMethod == null)
            {
                Debug.LogError("Failed to find the property _exitPlaceZoneMethod on the placeable item component");
                return;
            }

            _destroyOnPlace = serializedObject.FindProperty("_destroyOnPlace");

            if (_destroyOnPlace == null)
            {
                Debug.LogError("Failed to find the property _destroyOnPlace on the placeable item component");
                return;
            }

            _clearParentWhenRemoved = serializedObject.FindProperty("_clearParentWhenRemoved");

            if (_clearParentWhenRemoved == null)
            {
                Debug.LogError("Failed to find the property _clearParentWhenRemoved on the placeable item component");
                return;
            }

            _keepScaleForDefaultPlacements = serializedObject.FindProperty("_keepScaleForDefaultPlacements");

            if (_keepScaleForDefaultPlacements == null)
            {
                Debug.LogError("Failed to find the property _keepScaleForDefaultPlacements on the placeable item component");
                return;
            }

            if (_placeableItem.UtilityComponentContainer == null)
            {
                Debug.LogError("Failed to find the property UtilityComponentContainer on the placeable item component");
                return;
            }

            if (!_placeableItem.UtilityComponentContainer.TryGetComponent(out _placeableItemRigidbody))
            {
                Debug.LogError("Could not find the _placeableItemRigidbody component on the placeable item");
                return;
            }

            UpdateRigidBodySerializedReference(_placeableItemRigidbody);

            if (!_placeableItem.UtilityComponentContainer.TryGetComponent(out _placeableItemGrabbable))
            {
                Debug.LogError("Could not find the _placeableItemGrabbable component on the placeable item");
                return;
            }

            UpdateGrabbableItemSerializedReference(_placeableItemGrabbable);

            if (!_placeableItem.UtilityComponentContainer.TryGetComponent(out _previewObjectController))
            {
                Debug.LogError("Could not find the preview object controller component on the placeable item");
                return;
            }

            UpdatePreviewControllerSerializedReference(_previewObjectController);


            _usePreviewController = serializedObject.FindProperty("_usePreviewController");

            if (_usePreviewController == null)
            {
                Debug.LogErrorFormat(this, "Failed to find the property _usePreviewController on the placeable item component");
                return;
            }

            _socketReferenceProperty = serializedObject.FindProperty("_previewTransitionSocket");

            if (_socketReferenceProperty == null)
            {
                Debug.LogErrorFormat(this, "Failed to find the property _socketReferenceProperty on the placeable item component");
                return;
            }

            if (!_placeableItem.UtilityComponentContainer.TryGetComponent(out _stackableController))
            {
                Debug.LogErrorFormat(this, "Could not find the _stackableController component on the placeable item");
                return;
            }

            UpdateStackableControllerSerializedReference(_stackableController);

            UpdatePlaceableItemColliderSerializedReference(_placeableItem.PlaceableItemCollider);
        }

        private void UpdateRigidBodySerializedReference(PlaceableItemRigidbody placeableItemRigidbody)
        {
            _rigidBodySerializedObject = new SerializedObject(placeableItemRigidbody);

            _rigidbody = _rigidBodySerializedObject.FindProperty("_rigidbody");

            if (_rigidbody == null)
            {
                Debug.LogError("Failed to find the property _rigidbody on the placeable item rigid body component");
                return;
            }

            _placeableRigidbodySettings = _rigidBodySerializedObject.FindProperty("_settings");

            if (_placeableRigidbodySettings == null)
            {
                Debug.LogError("Failed to find the property _settings on the placeable item rigid body component");
                return;
            }
        }

        private void UpdateGrabbableItemSerializedReference(PlaceableItemGrabbable placeableItemGrabbable)
        {
            _placeableItemGrabbableSerializedObject = new SerializedObject(placeableItemGrabbable);

            _grabbableItem = _placeableItemGrabbableSerializedObject.FindProperty("_grabbableItemGameObject");

            if (_grabbableItem == null)
            {
                Debug.LogError("Failed to find the property _grabbableItem on the grabbable placeable item component");
                return;
            }
        }

        private void UpdatePreviewControllerSerializedReference(PlaceableItemPreviewController placeableItemPreviewController)
        {
            _placeableItemPreviewControllerSerializedObject = new SerializedObject(placeableItemPreviewController);

            _previewObject = _placeableItemPreviewControllerSerializedObject.FindProperty("_previewObjectPrefab");

            if (_previewObject == null)
            {
                Debug.LogError("Failed to find the property _previewObjectPrefab on the placeable item preview controller component");
                return;
            }

            _placeSpeedAnimationCurve = _placeableItemPreviewControllerSerializedObject.FindProperty("_placeSpeedAnimationCurve");

            if (_placeSpeedAnimationCurve == null)
            {
                Debug.LogError("Failed to find the property _placeSpeedAnimationCurve on the placeable item preview controller component");
                return;
            }

            _unplaceSpeedAnimationCurve = _placeableItemPreviewControllerSerializedObject.FindProperty("_unplaceSpeedAnimationCurve");

            if (_unplaceSpeedAnimationCurve == null)
            {
                Debug.LogError("Failed to find the property _unplaceSpeedAnimationCurve on the placeable item preview controller component");
                return;
            }

            _placeSpeed = _placeableItemPreviewControllerSerializedObject.FindProperty("_placeSpeed");

            if (_placeSpeed == null)
            {
                Debug.LogError("Failed to find the property _placeSpeed on the placeable item preview controller component");
                return;
            }

            _unplaceSpeed = _placeableItemPreviewControllerSerializedObject.FindProperty("_unplaceSpeed");

            if (_unplaceSpeed == null)
            {
                Debug.LogError("Failed to find the property _unplaceSpeed on the placeable item preview controller component");
                return;
            }

            _useRuntimePreviewObject = _placeableItemPreviewControllerSerializedObject.FindProperty("_useRuntimePreviewObject");

            if (_useRuntimePreviewObject == null)
            {
                Debug.LogError("Failed to find the property _useRuntimePreviewObject on the placeable item preview controller component");
                return;
            }
        }

        private void UpdateStackableControllerSerializedReference(StackableItemController stackableItemController)
        {
            _stackableControllerSerializedObject = new SerializedObject(stackableItemController);

            _stackableSerializedProperty = _stackableControllerSerializedObject.FindProperty("_stackable");
        }

        private void UpdatePlaceableItemColliderSerializedReference(PlaceableItemCollider placeableItemCollider)
        {
            _placeableItemColliderSerializedObject = new SerializedObject(placeableItemCollider);
        }

        private void DrawInspectorUI()
        {
            DrawTabs();
            DetermineTabToDraw();
        }

        /// <summary>
        /// This function is designed to draw the UI in prefab comparison mode. Tabs become disabled when comparing source to new prefabs,
        /// so this function draws all the tabs vertically to help with a side by side comparison.
        /// </summary>
        private void DrawNonInspectorUI()
        {
            DrawSettingsTab();
            DrawPreviewObjectTab();
            DrawPlacementCriteriaTab();
            DrawStackingTab();
            DrawXRTab();
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginVertical();

            _tabIndexProperty.intValue = GUILayout.Toolbar(_tabIndexProperty.intValue, _tabs);

            EditorGUILayout.EndVertical();
        }

        private void DetermineTabToDraw()
        {
            switch (_placeableItem.TabIndex)
            {
                case 0: DrawSettingsTab(); break;
                case 1: DrawPreviewObjectTab(); break;
                case 2: DrawPlacementCriteriaTab(); break;
                case 3: DrawStackingTab(); break;
                case 4: DrawXRTab(); break;
            }
        }


        /// <summary>
        /// This function will determine if the user is in the prefab comparison mode. We need to detect this so that we can display all
        /// of the tabs at once in the comparison mode as the tabs are disabled in the prefab source menu.
        /// </summary>
        /// <returns></returns>
        private bool IsInPrefabOverrideMenu()
        {
            Type inspectorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            EditorWindow currentWindow = EditorWindow.GetWindow(inspectorType, false, "Inspector", false);

            if (currentWindow == null) return false;

            bool isPrefabSourceWindowShowing = !GUI.enabled;

            if (isPrefabSourceWindowShowing)
            {
                _dateTimeLastPrefabSourceWindowShown = DateTime.Now;
            }

            bool isViewingPrefabSourceWindow = DateTime.Now < _dateTimeLastPrefabSourceWindowShown.AddSeconds(0.1f);

            return isViewingPrefabSourceWindow;
        }

        #endregion General Functions

        #region Settings Tab

        private void DrawSettingsTab()
        {
            DrawCoreSettings();
            DrawRigidBodyRegion();

            DrawPlaceableAreaColliderSettings();
        }

        private void DrawCoreSettings()
        {
            GUI.backgroundColor = _regionBackgroundColour;

            DrawRegionHeader("Core Settings");
            DisplayTagDropDown(_placeableItem);
            EditorGUILayout.PropertyField(_rootTransform);

            EditorGUILayout.PropertyField(_exitPlaceZoneMethod);

            if (_exitPlaceZoneMethod.intValue != (int)ExitPlaceZoneMethod.COLLIDER)
            {
                EditorGUILayout.PropertyField(_exitPlaceZoneThreshold);
            }

            EditorGUILayout.PropertyField(_destroyOnPlace);
            EditorGUILayout.PropertyField(_clearParentWhenRemoved);
            EditorGUILayout.PropertyField(_keepScaleForDefaultPlacements);

            GUILayout.EndVertical();

            GUI.backgroundColor = _defaultGUIBackgroundColour;
        }

        private void DrawRigidBodyRegion()
        {
            GUI.backgroundColor = _regionBackgroundColour;

            DrawRegionHeader("Rigidbody Settings");

            _placeableItemRigidbody.enabled = EditorGUILayout.Toggle("Use Rigidbody Settings", _placeableItemRigidbody.enabled);

            if (_placeableItemRigidbody.enabled)
            {
                EditorGUILayout.PropertyField(_rigidbody);
                EditorGUILayout.PropertyField(_placeableRigidbodySettings);
            }

            GUILayout.EndVertical();

            GUI.backgroundColor = _defaultGUIBackgroundColour;
        }



        #endregion Settings Tabs

        #region Preview Object

        private void DrawPreviewObjectTab()
        {
            DrawPlaceableItemToggleRegion();

            if (!_usePreviewController.boolValue)
            {
                return;
            }
            DrawPreviewObjectType();

            if (!_useRuntimePreviewObject.boolValue)
            {
                DrawPlaceableItemReferenceRegion();
                DrawGeneratePlaceableItemRegion();
            }

            DrawTransitionToSocketRegion();
            DrawTransitionPreviewRegion();
        }

        private void DrawPlaceableItemToggleRegion()
        {
            GUI.backgroundColor = _regionBackgroundColour;

            EditorGUILayout.Space();
            GUILayout.BeginVertical(GUIStyles.RegionStyle);
            EditorGUILayout.LabelField("Use Preview Object", GUIStyles.RegionNameStyle);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_usePreviewController);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            GUILayout.EndVertical();

            GUI.backgroundColor = _defaultGUIBackgroundColour;
        }

        private void DrawPlaceableItemReferenceRegion()
        {
            GUI.backgroundColor = _regionBackgroundColour;

            EditorGUILayout.Space();
            GUILayout.BeginVertical(GUIStyles.RegionStyle);
            EditorGUILayout.LabelField("Preview Object Reference", GUIStyles.RegionNameStyle);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_previewObject);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            GUILayout.EndVertical();

            GUI.backgroundColor = _defaultGUIBackgroundColour;
        }

        private void DrawPreviewObjectType()
        {
            GUI.backgroundColor = _regionBackgroundColour;

            EditorGUILayout.Space();
            GUILayout.BeginVertical(GUIStyles.RegionStyle);
            EditorGUILayout.LabelField("Preview Object Type", GUIStyles.RegionNameStyle);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_useRuntimePreviewObject);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            GUILayout.EndVertical();

            GUI.backgroundColor = _defaultGUIBackgroundColour;
        }

        private void DrawGeneratePlaceableItemRegion()
        {
            GUI.backgroundColor = _regionBackgroundColour;

            EditorGUILayout.Space();
            GUILayout.BeginVertical(GUIStyles.RegionStyle);
            EditorGUILayout.LabelField("Generate Preview Object", GUIStyles.RegionNameStyle);
            EditorGUILayout.Space();

            DrawOverrideExistingPreviewPrefabOption();

            DrawPreviewObjectNameField();

            DrawPreviewObjectSavePathField();

            DrawGeneratePreviewObjectButton();

            if (!_previewItemSaveLocationWindowOpen)
            {
                GUILayout.EndVertical();
            }

            GUI.backgroundColor = _defaultGUIBackgroundColour;
        }

        private void DrawOverrideExistingPreviewPrefabOption()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Override Existing Prefab");
            _overrideExistingPreviewPrefab = EditorGUILayout.Toggle(_overrideExistingPreviewPrefab);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreviewObjectNameField()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Preview Object Name");
            _previewObjectPrefabName = EditorGUILayout.TextField(GetPreviewObjectPrefabName());
            EditorGUILayout.EndHorizontal();
        }


        private void DrawPreviewObjectSavePathField()
        {
            EditorGUILayout.BeginHorizontal();
            string previewObjectSaveFilePath = EditorGUILayout.TextField("Preview Object Save Path", _previewObjectController.PreviewObjectSaveFilePath);

            if (GUILayout.Button("Browse", GUILayout.MaxWidth(80)))
            {
                _previewItemSaveLocationWindowOpen = true;

                previewObjectSaveFilePath = EditorUtility.OpenFolderPanel("Select Save Path", previewObjectSaveFilePath, "");

                if (!previewObjectSaveFilePath.StartsWith("Assets/"))
                {
                    previewObjectSaveFilePath = "Assets/" + previewObjectSaveFilePath.Replace(Application.dataPath, "");
                }
                previewObjectSaveFilePath = previewObjectSaveFilePath.TrimEnd('/');
            }

            _previewObjectController.SetPreviewObjectSaveFilePath(previewObjectSaveFilePath);

            if (_previewItemSaveLocationWindowOpen)
                return;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGeneratePreviewObjectButton()
        {
            bool preivewObjectExistsInFileLocation = PreviewObjectExists(GetPreviewObjectPrefabFilePath());

            bool disableGeneratePreviewObjectButton = preivewObjectExistsInFileLocation && !_overrideExistingPreviewPrefab;

            EditorGUILayout.Space();

            if (disableGeneratePreviewObjectButton)
            {
                EditorGUILayout.HelpBox("A preview object already exists with that name at the specified path. To generate a new preview object, delete the existing one first, change the preview object name, or tick the Override Exisiting Prefab tick box.", MessageType.Info);
            }

            if (preivewObjectExistsInFileLocation && _overrideExistingPreviewPrefab)
            {
                EditorGUILayout.HelpBox("A preview object already exists with that name at the specified path. The existing preview object will be overwritten.", MessageType.Warning);
            }

            EditorGUI.BeginDisabledGroup(disableGeneratePreviewObjectButton);

            if (GUILayout.Button("Generate Preview Object"))
            {
                GeneratePreviewObject();
            }

            EditorGUI.EndDisabledGroup();
        }

        private void GeneratePreviewObject()
        {
            GameObject previewObject = ComponentStripperManager.DuplicateAndStrip(_placeableItem.RootTransform.gameObject, new List<Type>() { typeof(MeshFilter), typeof(MeshRenderer), typeof(SkinnedMeshRenderer) });

            previewObject.name = "NEW PREVIEW OBJECT";

            PrefabUtility.SaveAsPrefabAsset(previewObject, GetPreviewObjectPrefabFilePath());

            AssetDatabase.Refresh();

            GameObject previewObjectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GetPreviewObjectPrefabFilePath());

            if (previewObjectPrefab == null)
            {
                Debug.LogError("The preview prefab just created could not be loaded.");
                return;
            }

            _previewObjectController.SetPreviewObjectPrefab(previewObjectPrefab);

            DestroyImmediate(previewObject);
        }

        private bool PreviewObjectExists(string prefabPath)
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            return existingPrefab != null;
        }

        private string GetPreviewObjectPrefabName()
        {
            if (!string.IsNullOrEmpty(_previewObjectPrefabName))
            {
                return _previewObjectPrefabName;
            }

            return _placeableItem.RootTransform.gameObject.name + " Placeable Item Preview Object";
        }

        private string GetPreviewObjectPrefabFilePath()
        {
            return Path.Combine(_previewObjectController.PreviewObjectSaveFilePath, GetPreviewObjectPrefabName() + ".prefab");
        }

        private void DrawTransitionToSocketRegion()
        {
            GUI.backgroundColor = _regionBackgroundColour;

            EditorGUILayout.Space();
            GUILayout.BeginVertical(GUIStyles.RegionStyle);
            EditorGUILayout.LabelField("Transition Settings", GUIStyles.RegionNameStyle);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_placeSpeed);
            EditorGUILayout.PropertyField(_unplaceSpeed);
            EditorGUILayout.PropertyField(_placeSpeedAnimationCurve);
            EditorGUILayout.PropertyField(_unplaceSpeedAnimationCurve);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            GUILayout.EndVertical();

            GUI.backgroundColor = _defaultGUIBackgroundColour;
        }

        private void DrawTransitionPreviewRegion()
        {
            GUI.backgroundColor = _regionBackgroundColour;

            EditorGUILayout.Space();
            GUILayout.BeginVertical(GUIStyles.RegionStyle);


            EditorGUILayout.LabelField("Transition preview", GUIStyles.RegionNameStyle);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_socketReferenceProperty);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Playmode is required in order to test the transitions of the preview object.", MessageType.Info);
            }

            GUI.enabled = _socketReferenceProperty.objectReferenceValue != null && Application.isPlaying && !_placeableItem.Placed && !_placeableItem.WithinPlaceableZone;

            if (GUILayout.Button("Transition preview object to Socket"))
            {
                TransitionToSocket();
            }

            GUI.enabled = _socketReferenceProperty.objectReferenceValue != null && Application.isPlaying && !_placeableItem.Placed && _placeableItem.WithinPlaceableZone;

            if (GUILayout.Button("Transition preview object from Socket"))
            {
                TransitionFromSocket();
            }

            GUI.enabled = _socketReferenceProperty.objectReferenceValue != null && Application.isPlaying && !_placeableItem.Placed;

            if (GUILayout.Button("Place in Socket"))
            {
                PlaceInSocket();
            }

            GUI.enabled = _socketReferenceProperty.objectReferenceValue != null && Application.isPlaying && _placeableItem.Placed;

            if (GUILayout.Button("Remove from Socket"))
            {
                RemoveFromSocketAtOriginalPosition();
            }

            GUI.enabled = true;

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            GUILayout.EndVertical();

            GUI.backgroundColor = _defaultGUIBackgroundColour;
        }

        private Vector3 _preSocketPosition;
        private Quaternion _preSocketRotation;
        private float _originalPlaceColliderRadius;

        private void TransitionToSocket()
        {
            Socket socket = _socketReferenceProperty.objectReferenceValue as Socket;

            if (socket == null)
            {
                Debug.LogError("No socket reference assigned. Please assign a valid socket reference.");
                return;
            }

            if (socket.SocketPlaceCollider.ColliderManager.GetRadius() != 100f)
            {
                _originalPlaceColliderRadius = socket.SocketPlaceCollider.ColliderManager.GetRadius();
                socket.SocketPlaceCollider.ColliderManager.SetColliderRadius(100f);
            }

            _preSocketPosition = _placeableItem.RootTransform.position;
            _preSocketRotation = _placeableItem.RootTransform.rotation;

            _placeableItem.SetPreventPlacement(true);
            _placeableItem.HandleEnteredPlaceableZone(socket);
        }

        private void TransitionFromSocket()
        {
            _placeableItem.HandleMovedOutOfPlaceRadiusEditor();
        }

        private void RemoveFromSocketAtOriginalPosition()
        {
            _placeableItem.RemoveFromSocket();
            _placeableItem.RootTransform.SetPositionAndRotation(_preSocketPosition, _preSocketRotation);
        }

        private void PlaceInSocket()
        {
            Socket socket = _socketReferenceProperty.objectReferenceValue as Socket;

            if (socket == null)
            {
                Debug.LogError("No socket reference assigned. Please assign a valid socket reference.");
                return;
            }

            socket.SocketPlaceCollider.ColliderManager.SetColliderRadius(_originalPlaceColliderRadius);

            _placeableItem.SetPreventPlacement(false);
            _placeableItem.PlaceInSocket(socket);
        }

        #endregion Preview Object

        #region  Placement Criteria Tab

        private void DrawPlacementCriteriaTab()
        {
            DrawPlacementCriteriaOptionsRegion();

            DrawPlacementCriteriaCustomEditors();
        }

        private void DrawPlacementCriteriaOptionsRegion()
        {
            EditorLayoutUtilities.DrawTopOfSection("Placement Criteria Components");

            List<string> placementCriteriaNames = GetPlacementCriteriaNames();

            for (int i = 0; i < placementCriteriaNames.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                bool hasCriteria = _placeableItem.PlaceableItemPlacementCriteriaController.HasCriteria(placementCriteriaNames[i]);
                GUI.enabled = !hasCriteria;

                EditorGUILayout.LabelField(placementCriteriaNames[i].SplitCamelCase());

                if (GUILayout.Button("Add", GUILayout.Width(40)))
                {
                    AddPlacementCriteria(placementCriteriaNames[i]);
                }

                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }

            EditorLayoutUtilities.DrawBottomOfSection();
        }

        private List<string> GetPlacementCriteriaNames()
        {
            var placementCriteriaType = typeof(IPlaceableItemPlacementCriteria);
            var derivedTypes = TypeCache.GetTypesDerivedFrom(placementCriteriaType);

            return derivedTypes
                .Where(type => type.IsClass && !type.IsAbstract)
                .Select(type => type.Name)
                .ToList();
        }


        private void DrawPlacementCriteriaCustomEditors()
        {
            if (_placeableItem.PlaceableItemPlacementCriteriaController == null)
            {
                Debug.LogErrorFormat(this, "No PlaceableItemPlacementCriteriaController found on PlaceableItem: {0}", _placeableItem.name);
                return;
            }

            EditorLayoutUtilities.DrawTopOfSection("Placement Criteria Custom Editors");

            List<CriteriaEntry> placementCriterias = _placeableItem.PlaceableItemPlacementCriteriaController.Criterias;

            for (int i = placementCriterias.Count - 1; i >= 0; i--)
            {
                CriteriaEntry criteriaEntry = placementCriterias[i];
                string criteraName = criteriaEntry.CriteriaName;
                MonoBehaviour criteria = criteriaEntry.CriteriaComponent;

                if (string.IsNullOrEmpty(criteraName))
                {
                    Debug.LogErrorFormat(this, "Placement Criteria Name is null or empty. Skipping criteria: {0}", criteria.GetType().Name);
                    continue;
                }

                if (criteria == null)
                {
                    Debug.LogErrorFormat(this, "Placement Criteria component is null. Skipping criteria: {0}", criteraName);
                    continue;
                }

                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(criteraName.SplitCamelCase(), GUIStyles.RegionNameStyle);

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    RemovePlacementCriteria(criteraName);
                    continue;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                if (!_placementCriteriaEditors.TryGetValue(criteria, out UnityEditor.Editor criteriaEditor))
                {
                    criteriaEditor = UnityEditor.Editor.CreateEditor(criteria as UnityEngine.Object);
                    _placementCriteriaEditors[criteria] = criteriaEditor;
                }

                criteriaEditor.OnInspectorGUI();
            }

            EditorLayoutUtilities.DrawBottomOfSection();
        }


        private void AddPlacementCriteria(string criteriaName)
        {
            Type criteriaType = TypeCache.GetTypesDerivedFrom<IPlaceableItemPlacementCriteria>()
                .FirstOrDefault(type => type.Name == criteriaName);

            if (criteriaType == null)
            {
                Debug.LogErrorFormat(this, "Could not find criteria type: {0}", criteriaName);
                return;
            }

            _placeableItem.PlaceableItemPlacementCriteriaController.AddPlacementCriteria(criteriaName);

            EditorUtility.SetDirty(_placeableItem.gameObject);
            EditorUtility.SetDirty(_placeableItem.PlaceableItemPlacementCriteriaController.gameObject);
        }

        private void RemovePlacementCriteria(string criteriaName)
        {
            Type criteriaType = TypeCache.GetTypesDerivedFrom<IPlaceableItemPlacementCriteria>()
             .FirstOrDefault(type => type.Name == criteriaName);

            if (criteriaType == null)
            {
                Debug.LogErrorFormat(this, "Could not find criteria type: {0}", criteriaName);
                return;
            }

            _placeableItem.PlaceableItemPlacementCriteriaController.RemovePlacementCriteria(criteriaName);

            EditorUtility.SetDirty(_placeableItem.gameObject);
            EditorUtility.SetDirty(_placeableItem.PlaceableItemPlacementCriteriaController.gameObject);
        }

        #endregion Placement Criteria Tab

        #region Stacking Tab

        private void DrawStackingTab()
        {
            DrawStackableItemRegion();

            if (_stackableSerializedProperty.boolValue)
            {
                DrawStackingSpawnTransitionRegion();
                DrawSpawnTransitionCustomEditors();
            }
        }

        private void DrawStackableItemRegion()
        {
            EditorLayoutUtilities.DrawTopOfSection("Stackable Item Settings");

            EditorGUILayout.PropertyField(_stackableSerializedProperty, new GUIContent("Stackable"));

            EditorLayoutUtilities.DrawBottomOfSection();
        }

        private void DrawStackingSpawnTransitionRegion()
        {
            EditorLayoutUtilities.DrawTopOfSection("Spawn Transition Options");

            List<string> spawnTransitionNames = GetStackSpawnTransitionNames();

            for (int i = 0; i < spawnTransitionNames.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                bool hasSpawnTransition = _placeableItem.StackableItemController.HasSpawnTransition(spawnTransitionNames[i]);
                GUI.enabled = !hasSpawnTransition;

                EditorGUILayout.LabelField(spawnTransitionNames[i].SplitCamelCase());

                if (GUILayout.Button("Add", GUILayout.Width(40)))
                {
                    AdSpawnTransition(spawnTransitionNames[i]);
                }

                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }

            EditorLayoutUtilities.DrawBottomOfSection();
        }

        private List<string> GetStackSpawnTransitionNames()
        {
            Type transitionType = typeof(IStackSpawnTransition);
            var derivedTypes = TypeCache.GetTypesDerivedFrom(transitionType);

            return derivedTypes
                .Where(type => type.IsClass && !type.IsAbstract)
                .Select(type => type.Name)
                .ToList();
        }

        private void AdSpawnTransition(string transitionType)
        {
            Type spawnType = TypeCache.GetTypesDerivedFrom<IStackSpawnTransition>()
                .FirstOrDefault(type => type.Name == transitionType);

            if (spawnType == null)
            {
                Debug.LogErrorFormat(this, "Could not find spawn transition type: {0}", transitionType);
                return;
            }

            _placeableItem.StackableItemController.AddSpawnTransition(transitionType);

            EditorUtility.SetDirty(_placeableItem.gameObject);
            EditorUtility.SetDirty(_placeableItem.StackableItemController.gameObject);
        }

        private void DrawSpawnTransitionCustomEditors()
        {
            if (_placeableItem.StackableItemController == null)
            {
                Debug.LogErrorFormat(this, "No StackableItemController found on PlaceableItem: {0}", _placeableItem.name);
                return;
            }

            EditorLayoutUtilities.DrawTopOfSection("Spawn Transition Custom Editors");

            List<SpawnTransitionEntry> spawnTransitions = _placeableItem.StackableItemController.SpawnTransitionEntries;

            for (int i = spawnTransitions.Count - 1; i >= 0; i--)
            {
                SpawnTransitionEntry transitionEntry = spawnTransitions[i];
                string spawnTransitionName = transitionEntry.TransitionName;
                MonoBehaviour SpawnTransition = transitionEntry.TransitionComponent;

                if (string.IsNullOrEmpty(spawnTransitionName))
                {
                    Debug.LogErrorFormat(this, "Spawn Transition Name is null or empty. Skipping transition: {0}", SpawnTransition.GetType().Name);
                    continue;
                }

                if (SpawnTransition == null)
                {
                    Debug.LogErrorFormat(this, "Spawn Transition component is null. Skipping transition: {0}", spawnTransitionName);
                    continue;
                }

                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(spawnTransitionName.SplitCamelCase(), GUIStyles.RegionNameStyle);

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    RemoveSpawnTransition(spawnTransitionName);
                    EditorGUILayout.EndHorizontal();
                    continue;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                if (!_stackingSpawnTransitionEditors.TryGetValue(SpawnTransition, out UnityEditor.Editor spawnTransitionEditor))
                {
                    spawnTransitionEditor = UnityEditor.Editor.CreateEditor(SpawnTransition as UnityEngine.Object);
                    _stackingSpawnTransitionEditors[SpawnTransition] = spawnTransitionEditor;
                }

                spawnTransitionEditor.OnInspectorGUI();
            }

            EditorLayoutUtilities.DrawBottomOfSection();
        }

        private void RemoveSpawnTransition(string spawnTransitionName)
        {
            Type transitionType = TypeCache.GetTypesDerivedFrom<IStackSpawnTransition>()
             .FirstOrDefault(type => type.Name == spawnTransitionName);

            if (transitionType == null)
            {
                Debug.LogErrorFormat(this, "Could not find spawn transition type: {0}", spawnTransitionName);
                return;
            }

            _placeableItem.StackableItemController.RemoveSpawnTransition(spawnTransitionName);

            EditorUtility.SetDirty(_placeableItem.gameObject);
            EditorUtility.SetDirty(_placeableItem.StackableItemController.gameObject);
        }

        #endregion Stacking Tab

        #region XR Tab
        private void DrawXRTab()
        {
            DrawGrabbaleItemRegion();

            DrawXRGrabColliderSettings();
        }

        private void DrawGrabbaleItemRegion()
        {
            EditorLayoutUtilities.DrawTopOfSection("Grabbable Settings");

            DrawIGrabbableInputRegion();
            DrawPreventPlacementWhileHoldingRegion();

            EditorLayoutUtilities.DrawBottomOfSection();
        }

        private void DrawIGrabbableInputRegion()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(_grabbableItem, new GUIContent("IGrabbable"));

            _placeableItemGrabbableSerializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                ValidateGrabbableItem();
            }
        }

        private void DrawPreventPlacementWhileHoldingRegion()
        {
            bool preventPlacementWhileHolding = _placeableItem.PlaceableItemPlacementCriteriaController.HasCriteria(typeof(NotHoldingItem).Name);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Prevent Placement While Holding");
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Toggle(preventPlacementWhileHolding);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("The prevent placement while holding setting can be updated in the Placement Criteria Tab", MessageType.Info);
        }

        private void DrawXRGrabColliderSettings()
        {
            ColliderConfigurationEditorUtilities.DrawColliderSettingsRegion("Grab Collider Settings", _placeableItem.SocketGrabCollider.ColliderManager, ref _adjustXRGrabCollider);
        }

        private void ValidateGrabbableItem()
        {
            if (!_placeableItem.UtilityComponentContainer.TryGetComponent(out PlaceableItemGrabbable placeableItemGrabbale))
            {
                Debug.LogError("Could not obtain a reference to the PlaceableItemGrabbable on the _placeableItem");
                return;
            }

            if (placeableItemGrabbale.GrabbableItemGameObject == null)
            {
                Debug.LogError("Object is null");
                return;
            }

            if (!placeableItemGrabbale.GrabbableItemGameObject.TryGetComponent(out IGrabbableItem iGrabbable))
            {
                Debug.LogError("An object that does not contain an IGrabbable implementation has been assinged to the grabbable item reference", this);
                placeableItemGrabbale.ClearGrabbableGameObject();
                return;
            }
        }

        private void DrawPlaceableAreaColliderSettings()
        {
            ColliderConfigurationEditorUtilities.DrawColliderSettingsRegion("Placeable Area Collider Settings", _placeableItem.PlaceableItemCollider.ColliderManager, ref _adjustPlaceableItemCollider);
        }

        #endregion Integrations Tab

        private void DisplayTagDropDown(PlaceableItem placeableItem)
        {
            string currentTag = placeableItem.ItemTag;
            int oldTagIndex = Array.IndexOf(_tags, currentTag);

            int newTagIndex = EditorGUILayout.Popup("Placeable Item Tag", oldTagIndex, _tags);


            if (newTagIndex == -1)
                return;

            placeableItem.SetTag(_tags[newTagIndex]);

            if (newTagIndex != oldTagIndex)
            {
                EditorUtility.SetDirty(target);
            }
        }

        private void DisplayUnsocketOption()
        {
            GUI.enabled = _placeableItem.Placed;

            if (GUILayout.Button("Unsocket"))
            {
                _placeableItem.RemoveFromSocket();
            }

            GUI.enabled = true;
        }

        private void DrawRegionHeader(string headerLabel)
        {
            EditorGUILayout.Space();
            GUILayout.BeginVertical(GUIStyles.RegionStyle);
            EditorGUILayout.LabelField(headerLabel, GUIStyles.RegionNameStyle);
            EditorGUILayout.Space();
        }
    }
}
