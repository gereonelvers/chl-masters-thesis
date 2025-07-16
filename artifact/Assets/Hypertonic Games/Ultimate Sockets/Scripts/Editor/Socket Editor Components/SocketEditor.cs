using Hypertonic.Modules.UltimateSockets.Models;
using Hypertonic.Modules.UltimateSockets.Highlighters;
using Hypertonic.Modules.XR.PlacePoints.UltimateSockets.Models.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Hypertonic.Modules.UltimateSockets.Enums;
using Hypertonic.Modules.UltimateSockets.Sockets;
using Hypertonic.Modules.UltimateSockets.Sockets.Stacking;
using Hypertonic.Modules.UltimateSockets.PlaceableItems.Stacking;
using Hypertonic.Modules.UltimateSockets.Extensions;
using Hypertonic.Modules.UltimateSockets.Interfaces;
using Hypertonic.Modules.UltimateSockets.Editor.Utilities;
using System.Threading.Tasks;

namespace Hypertonic.Modules.UltimateSockets.Editor.Sockets
{
    [CustomEditor(typeof(Socket), true)]
    [CanEditMultipleObjects]
    public class SocketEditor : UnityEditor.Editor
    {

        #region General Variables

        private Socket _socket;
        private UltimateSocketSettings _ultimateSocketSettings;
        private readonly string[] _tabs = { "Settings", "Item Placements", "Highlighters", "Placement Criteria", "Stacking", "Audio" };

        private SerializedProperty _tabIndexProperty;
        private string[] _tags = new string[0];

        // Used to prevent redrawing error log when settings change from null.
        private bool _shouldDelayDrawingCustomInspectorGUI = false;

        private static DateTime _dateTimeLastPrefabSourceWindowShown;

        #endregion General Variables

        #region Settings Tab Variables

        private bool _showTags = true;

        private SerializedProperty _anyTagAllowed;

        private SerializedProperty _preventItemPlacement;
        private SerializedProperty _preventPhysicalItemPlacement;
        private SerializedProperty _preventItemRemoval;
        private SerializedProperty _placeOnStart;

        private int _currentAllowedTagsPage = 0;
        private const int _allowedTagsPerPage = 20;


        #region Placeable Item Detector Collider Variables

        private bool _adjustDetectionCollider = false;


        #endregion Placeable Item Detector Collider Variables

        #region Socket Highlighter Variables

        private SerializedObject _socketHighlighterSerializedObject;
        private SerializedProperty _useSocketHighlighter;

        private bool _adjustHighlightDetectionCollider = false;
        private Dictionary<MonoBehaviour, UnityEditor.Editor> _highlighterEditors = new Dictionary<MonoBehaviour, UnityEditor.Editor>();

        private SerializedObject _socketHighlightAreaColliderSerializedObject;
        private SerializedProperty _matchPlacementDetectionCollider;


        #endregion Socket Highlighter Variables

        #endregion Settings Tab Variables




        #region  Item Placement Tab Variables

        private SerializedProperty _placementProfileType;

        private SerializedProperty _placementProfile;
        private SerializedProperty _placementProfileScriptableObject;

        private ItemPlacementConfig _placementConfig = new ItemPlacementConfig();

        private GameObject _placeableItemPosingObject;

        private bool _isAddingNewPlacementConfig = false;
        private bool _isEditingDefultPlacementConfig = false;

        private UnityEditor.Editor _placementProfileScriptableObjectEditor;

        private SerializedProperty _defaultItemPlacementConfig;


        private SerializedObject _placeTransformSerializedObject;
        private SerializedProperty _tweenToSocket;
        private SerializedProperty _tweenToSocketCurve;
        private SerializedProperty _tweenToSocketDurationSeconds;
        private SerializedProperty _keepDefaultObjectScale;

        private SerializedProperty _preventPreviewItems;

        private Vector3 _originalPosingObjectScale;
        private bool _shouldDrawSaveConfigurationButton = false;
        private bool _cloneItemPlacement = false;
        private int _itemIndexToClone = -1;

        private bool _showNonConfiguredItemsOnly = false;


        #endregion Item Placement Tab Variables




        #region Placement Criteria Tab Variables

        private Dictionary<MonoBehaviour, UnityEditor.Editor> _placementCriteriaEditors = new Dictionary<MonoBehaviour, UnityEditor.Editor>();

        #endregion Placement Criteria Tab Variables


        #region Stacking Tab Variables
        private SerializedObject _stackableItemControllerSerializedObject;
        private SerializedProperty _stackableControllerSettings;

        private SerializedProperty _isStackableProperty;
        private SerializedProperty _stackTypeProperty;
        private SerializedProperty _stackInfiniteReplacement;
        private SerializedProperty _maxStackSizeProperty;
        private SerializedProperty _instanceStackType;
        private SerializedProperty _stackReplacementDelay;

        private readonly Dictionary<MonoBehaviour, UnityEditor.Editor> _stackingSpawnTransitionEditors = new Dictionary<MonoBehaviour, UnityEditor.Editor>();

        #endregion Stacking Tab Variables

        #region Audio Tab Variables

        private SerializedObject _socketAudioControllerSerializedObject;
        private SerializedProperty _useAudio;
        private SerializedProperty _placeAudioClip;
        private SerializedProperty _removeAudioClip;

        UnityEditor.Editor _audioSourceEditor = null;

        #endregion Audio Tab Variables


        #region Unity Functions

        private void OnEnable()
        {
            _socket = target as Socket;

            Selection.selectionChanged += HandleHierarchySelectionChanged;
            UltimateSocketsSettingsEditorWindow.OnUltimateSocketSettingsChanged += HandleUltimateSocketSettingsChanged;

            CheckAndAssignReferences();

            _placementProfileScriptableObjectEditor = CreateEditor(_socket.SocketPlacementProfileScriptableObject);

            if (_matchPlacementDetectionCollider.boolValue)
            {
                UpdateHighlightColliderToMatchPlacementDetectionCollider();
            }

            LoadSettings();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= HandleHierarchySelectionChanged;
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


            if (_tags == null || _tags.Length == 0)
            {
                _tags = _ultimateSocketSettings.PlaceableItemTags.Tags.ToArray();
            }


            serializedObject.Update();
            _socketHighlighterSerializedObject.Update();
            _socketHighlightAreaColliderSerializedObject.Update();
            _stackableItemControllerSerializedObject.Update();
            _placeTransformSerializedObject.Update();
            _socketAudioControllerSerializedObject.Update();

            if (IsInPrefabOverrideMenu())
            {
                DrawNonInspectorUI();

            }
            else
            {
                DrawInspectorUI();
            }


            serializedObject.ApplyModifiedProperties();
            _socketHighlighterSerializedObject.ApplyModifiedProperties();
            _socketHighlightAreaColliderSerializedObject.ApplyModifiedProperties();
            _stackableItemControllerSerializedObject.ApplyModifiedProperties();
            _placeTransformSerializedObject.ApplyModifiedProperties();
            _socketAudioControllerSerializedObject.ApplyModifiedProperties();
        }

        protected virtual void OnSceneGUI()
        {
            if (_adjustDetectionCollider)
            {
                ColliderConfigurationEditorUtilities.AdjustItemDetectionCollider(_socket.SocketPlaceCollider.DetectionCollider, () => Repaint());
                return;
            }

            if (_adjustHighlightDetectionCollider)
            {
                ColliderConfigurationEditorUtilities.AdjustItemDetectionCollider(_socket.SocketHighlightAreaCollider.DetectionCollider, () => Repaint());
            }
        }

        #endregion Unity Functions

        #region General Functions

        [MenuItem("GameObject/Hypertonic/Ultimate Sockets/Add Socket", false, 11)]
        public static void AddSocketGameObject()
        {
            GameObject currentSelection = Selection.activeGameObject;
            GameObject socketGameObject = new GameObject("Placeable Item Socket");
            socketGameObject.transform.SetParent(currentSelection.transform, false);

            Socket socket = socketGameObject.AddComponent<Socket>();

            SocketInstantiator.InstantiateSocketOptions(socket);

            EditorUtility.SetDirty(currentSelection);

            Selection.activeGameObject = socketGameObject;
        }

        private void HandleUltimateSocketSettingsChanged()
        {
            if (_ultimateSocketSettings != null)
            {
                _tags = _ultimateSocketSettings.PlaceableItemTags.Tags.ToArray();
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

        private void HandleHierarchySelectionChanged()
        {
            if (Selection.activeGameObject != _socket.gameObject)
            {
                _adjustDetectionCollider = false;
                _adjustHighlightDetectionCollider = false;
            }
        }

        private void LoadSettings()
        {
            _ultimateSocketSettings = UltimateSocketUtilities.LoadSettings();
        }

        private void CheckAndAssignReferences()
        {
            _tabIndexProperty = serializedObject.FindProperty("_tabIndex");

            if (_tabIndexProperty == null)
            {
                Debug.LogError("Failed to find the property _tabIndexProperty on the placeable item component");
                return;
            }

            _placementProfile = serializedObject.FindProperty("_placementProfile");

            if (_placementProfile == null)
            {
                Debug.LogError("Failed to find the property _placementProfile on the socket component");
                return;
            }

            _placementProfileType = serializedObject.FindProperty("_placementProfileType");

            if (_placementProfileType == null)
            {
                Debug.LogError("Failed to find the property _placementProfileType on the socket component");
                return;
            }

            _placementProfileScriptableObject = serializedObject.FindProperty("_placementProfileScriptableObject");

            if (_placementProfileScriptableObject == null)
            {
                Debug.LogError("Failed to find the property _placementProfileScriptableObject on the socket component");
                return;
            }

            _defaultItemPlacementConfig = serializedObject.FindProperty("_defaultItemPlacementConfig");

            if (_defaultItemPlacementConfig == null)
            {
                Debug.LogError("Failed to find the property _defaultItemPlacementConfig on the socket component");
                return;
            }

            _anyTagAllowed = serializedObject.FindProperty("_anyTagAllowed");

            if (_anyTagAllowed == null)
            {
                Debug.LogError("Failed to find the property _anyTagAllowed on the socket component");
                return;
            }

            _preventItemPlacement = serializedObject.FindProperty("_preventItemPlacement");

            if (_preventItemPlacement == null)
            {
                Debug.LogError("Failed to find the property _lockWhenItemPlaced on the socket component");
                return;
            }

            _preventPhysicalItemPlacement = serializedObject.FindProperty("_preventPhysicalItemPlacement");

            if (_preventPhysicalItemPlacement == null)
            {
                Debug.LogError("Failed to find the property _preventPhysicalItemPlacement on the socket component");
                return;
            }

            _preventItemRemoval = serializedObject.FindProperty("_preventItemRemoval");

            if (_preventItemRemoval == null)
            {
                Debug.LogError("Failed to find the property _preventItemRemoval on the socket component");
                return;
            }

            if (_socket.SocketHighlighter == null)
            {
                Debug.LogError("Failed to find the SocketHighlighter component on the socket component");
                return;
            }

            _placeOnStart = serializedObject.FindProperty("_placeOnStart");

            if (_placeOnStart == null)
            {
                Debug.LogError("Failed to find the property _placeOnStart on the socket component");
                return;
            }

            _preventPreviewItems = serializedObject.FindProperty("_preventPreviewItems");

            if (_preventPreviewItems == null)
            {
                Debug.LogError("Failed to find the property _preventPreviewItems on the socket component");
                return;
            }

            UpdateSocketHighligherSerializedReference(_socket.SocketHighlighter);
            UpdateSocketHighlightAreaCollider();
            UpdateSocketStackableItemControllerSerializedReference(_socket.StackableItemController);
            UpdatePlaceTransformSerializedObject();
            UpdateAudioControllerSerializedObject();
        }

        private void UpdateSocketHighligherSerializedReference(SocketHighlighter socketHighlighter)
        {
            _socketHighlighterSerializedObject = new SerializedObject(socketHighlighter);

            _useSocketHighlighter = _socketHighlighterSerializedObject.FindProperty("_useHighlighters");

            if (_useSocketHighlighter == null)
            {
                Debug.LogErrorFormat(this, "Failed to find the property _useHighlighters on the SocketHighlighter component");
                return;
            }
        }

        private void UpdateSocketHighlightAreaCollider()
        {
            SocketHighlightAreaCollider socketHighlightAreaCollider = _socket.SocketHighlightAreaCollider;

            _socketHighlightAreaColliderSerializedObject = new SerializedObject(socketHighlightAreaCollider);

            _matchPlacementDetectionCollider = _socketHighlightAreaColliderSerializedObject.FindProperty("_matchPlacementDetectionCollider");

            if (_matchPlacementDetectionCollider == null)
            {
                Debug.LogErrorFormat(this, "Failed to find the property _matchPlacementDetectionCollider on the SocketHighlighter component");
                return;
            }
        }

        private void UpdateSocketStackableItemControllerSerializedReference(SocketStackableItemController socketStableItemController)
        {
            _stackableItemControllerSerializedObject = new SerializedObject(socketStableItemController);

            _stackableControllerSettings = _stackableItemControllerSerializedObject.FindProperty("_settings");

            if (_stackableControllerSettings == null)
            {
                Debug.LogErrorFormat(this, "Failed to find the property _settings on the SocketStackableItemController component");
                return;
            }

            _isStackableProperty = _stackableControllerSettings.FindPropertyRelative("Stackable");

            if (_isStackableProperty == null)
            {
                Debug.LogErrorFormat(this, "Failed to find the property Stackable on the SocketStackableItemController component");
                return;
            }

            _isStackableProperty = _stackableControllerSettings.FindPropertyRelative("Stackable");

            if (_isStackableProperty == null)
            {
                Debug.LogErrorFormat(this, "Failed to find the property Stackable on the SocketStackableItemController component");
                return;
            }

            _stackTypeProperty = _stackableControllerSettings.FindPropertyRelative("StackType");

            if (_stackTypeProperty == null)
            {
                Debug.LogErrorFormat(this, "Failed to find the property StackType on the SocketStackableItemController component");
                return;
            }

            _stackInfiniteReplacement = _stackableControllerSettings.FindPropertyRelative("InfiniteReplacement");

            if (_stackInfiniteReplacement == null)
            {
                Debug.LogErrorFormat(this, "Failed to find the property InfiniteReplacement on the SocketStackableItemController component");
                return;
            }

            _maxStackSizeProperty = _stackableControllerSettings.FindPropertyRelative("MaxStackSize");

            if (_maxStackSizeProperty == null)
            {
                Debug.LogErrorFormat(this, "Failed to find the property MaxStackSize on the SocketStackableItemController component");
                return;
            }

            _instanceStackType = _stackableControllerSettings.FindPropertyRelative("InstanceStackType");

            if (_instanceStackType == null)
            {
                Debug.LogErrorFormat(this, "Failed to find the property InstanceStackType on the SocketStackableItemController component");
                return;
            }

            _stackReplacementDelay = _stackableControllerSettings.FindPropertyRelative("StackReplacementDelay");

            if (_stackReplacementDelay == null)
            {
                Debug.LogErrorFormat(this, "Failed to find the property StackReplacementDelay on the SocketStackableItemController component");
                return;
            }
        }

        private void UpdateAudioControllerSerializedObject()
        {
            if (_socket.SocketAudioController == null)
            {
                Debug.LogErrorFormat(this, "SocketAudioController is null");
                return;
            }

            _socketAudioControllerSerializedObject = new SerializedObject(_socket.SocketAudioController);

            _useAudio = _socketAudioControllerSerializedObject.FindProperty("_useAudio");

            if (_useAudio == null)
            {
                Debug.LogErrorFormat(this, "Failed to find the property _useAudio on the SocketAudioController component");
                return;
            }

            _placeAudioClip = _socketAudioControllerSerializedObject.FindProperty("_placeAudioClip");

            if (_placeAudioClip == null)
            {
                Debug.LogErrorFormat(this, "Failed to find the property _placeAudioClip on the SocketAudioController component");
                return;
            }

            _removeAudioClip = _socketAudioControllerSerializedObject.FindProperty("_removeAudioClip");

            if (_removeAudioClip == null)
            {
                Debug.LogErrorFormat(this, "Failed to find the property _removeAudioClip on the SocketAudioController component");
                return;
            }
        }

        private void UpdatePlaceTransformSerializedObject()
        {
            _placeTransformSerializedObject = new SerializedObject(_socket.SocketPlaceTransform);

            _tweenToSocket = _placeTransformSerializedObject.FindProperty("_tweenToSocket");

            if (_tweenToSocket == null)
            {
                Debug.LogErrorFormat(this, "Failed to find the property _tweenToSocket on the PlaceTransform component");
                return;
            }

            _tweenToSocketCurve = _placeTransformSerializedObject.FindProperty("_tweenToSocketCurve");

            if (_tweenToSocketCurve == null)
            {
                Debug.LogErrorFormat(this, "Failed to find the property _tweenToSocketCurve on the PlaceTransform component");
                return;
            }

            _tweenToSocketDurationSeconds = _placeTransformSerializedObject.FindProperty("_tweenToSocketDurationSeconds");

            if (_tweenToSocketDurationSeconds == null)
            {
                Debug.LogErrorFormat(this, "Failed to find the property _tweenToSocketDurationSeconds on the PlaceTransform component");
                return;
            }

            _keepDefaultObjectScale = _placeTransformSerializedObject.FindProperty("_keepDefaultObjectScale");

            if (_keepDefaultObjectScale == null)
            {
                Debug.LogErrorFormat(this, "Failed to find the property _keepDefautlObjectScale on the PlaceTransform component");
                return;
            }
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
            DrawItemPlacementTab();
            DrawHighlightingTab();
            DrawSocketPlacementCriteriaTab();
            DrawStackingTab();
            DrawAudioTab();
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginVertical();

            _tabIndexProperty.intValue = GUILayout.Toolbar(_tabIndexProperty.intValue, _tabs);

            EditorGUILayout.EndVertical();
        }

        private void DetermineTabToDraw()
        {
            switch (_socket.TabIndex)
            {
                case 0: DrawSettingsTab(); break;
                case 1: DrawItemPlacementTab(); break;
                case 2: DrawHighlightingTab(); break;
                case 3: DrawSocketPlacementCriteriaTab(); break;
                case 4: DrawStackingTab(); break;
                case 5: DrawAudioTab(); break;
            }
        }





        #endregion General Functions

        #region Settings Tab

        private void DrawSettingsTab()
        {
            DrawAllowedTagsRegion();

            DrawLockSettings();

            DrawPlaceOnStartSettingsRegion();

            DrawPlaceableAreaColliderSettingsRegion();
        }

        private void DrawAllowedTagsRegion()
        {
            EditorLayoutUtilities.DrawTopOfSection("Allowed Items");

            EditorGUILayout.PropertyField(_anyTagAllowed);
            DisplayPlaceableItemTags(_socket);

            EditorLayoutUtilities.DrawBottomOfSection();
        }

        private void DisplayPlaceableItemTags(Socket socket)
        {
            if (socket.AnyTagAllowed) return;

            _showTags = EditorGUILayout.Foldout(_showTags, "Allowed Placeable Item Tags");

            bool itemChanged = false;

            if (_showTags)
            {
                int totalTags = _tags.Length;
                int totalPages = Mathf.CeilToInt((float)totalTags / _allowedTagsPerPage);

                int startIndex = _currentAllowedTagsPage * _allowedTagsPerPage;
                int endIndex = Mathf.Min(startIndex + _allowedTagsPerPage, totalTags);

                for (int i = startIndex; i < endIndex; i++)
                {
                    bool tagIsAllowed = socket.AllowedTags.Contains(_tags[i]);
                    bool newState = EditorGUILayout.Toggle(_tags[i], tagIsAllowed);

                    if (newState != tagIsAllowed)
                    {
                        itemChanged = true;

                        if (newState)
                        {
                            socket.SetAllowedTag(_tags[i]);
                        }
                        else
                        {
                            socket.SetNotAllowedTag(_tags[i]);
                        }
                    }
                }

                EditorGUILayout.Space();

                // Pagination controls
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Previous", GUILayout.Width(100)) && _currentAllowedTagsPage > 0)
                {
                    _currentAllowedTagsPage--;
                }

                EditorGUILayout.LabelField($"Page {_currentAllowedTagsPage + 1} of {totalPages}", EditorStyles.centeredGreyMiniLabel);

                if (GUILayout.Button("Next", GUILayout.Width(100)) && _currentAllowedTagsPage < totalPages - 1)
                {
                    _currentAllowedTagsPage++;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (itemChanged)
            {
                EditorUtility.SetDirty(target);
            }
        }


        private void DrawLockSettings()
        {
            EditorLayoutUtilities.DrawTopOfSection("Lock Settings");

            EditorGUILayout.PropertyField(_preventItemPlacement);
            EditorGUILayout.PropertyField(_preventPhysicalItemPlacement);
            EditorGUILayout.PropertyField(_preventItemRemoval);
            EditorGUILayout.PropertyField(_preventPreviewItems);


            EditorLayoutUtilities.DrawBottomOfSection();
        }

        private void DrawPlaceOnStartSettingsRegion()
        {
            EditorLayoutUtilities.DrawTopOfSection("Place On Start");

            EditorGUILayout.PropertyField(_placeOnStart);

            EditorLayoutUtilities.DrawBottomOfSection();
        }

        private void DrawPlaceableAreaColliderSettingsRegion()
        {
            int oldDetectorColliderType = _socket.SocketPlaceCollider.ColliderManager.GetColliderTypeIndex();

            ColliderConfigurationEditorUtilities.DrawColliderSettingsRegion("Placeable Area Collider Settings", _socket.SocketPlaceCollider.ColliderManager, ref _adjustDetectionCollider);

            if (_adjustDetectionCollider && _adjustHighlightDetectionCollider)
            {
                _adjustHighlightDetectionCollider = false;
            }

            if (_socket.SocketPlaceCollider.ColliderManager.GetColliderTypeIndex() != oldDetectorColliderType)
            {
                UpdateHighlightColliderToMatchPlacementDetectionCollider();
            }

        }

        #endregion Settings Tab

        #region Item Placement Tab

        private void DrawItemPlacementTab()
        {
            DrawTranisitonToSocketOptions();

            DrawProfileTypeRegion();

            DrawDefaultItemPlacementSection();

            DrawItemPlacementConfigsSection();

            DrawConfigureItemPlacementRegion();
        }

        private void DrawTranisitonToSocketOptions()
        {
            EditorLayoutUtilities.DrawTopOfSection("Placement Transition Settings");

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_tweenToSocket);

            if (_tweenToSocket.boolValue)
            {
                EditorGUILayout.PropertyField(_tweenToSocketDurationSeconds);
                EditorGUILayout.PropertyField(_tweenToSocketCurve);
            }

            EditorLayoutUtilities.DrawBottomOfSection();
        }

        private void DrawProfileTypeRegion()
        {
            EditorLayoutUtilities.DrawTopOfSection("Profile Type");

            EditorGUILayout.PropertyField(_placementProfileType);
            EditorGUILayout.HelpBox("Choose if the placement config should be saved to the gameobject or to a scriptable object. Saving it to a scriptable object is useful if you want to use the same settings on multiple sockets", MessageType.Info);

            EditorLayoutUtilities.DrawBottomOfSection();
        }

        private void DrawDefaultItemPlacementSection()
        {
            EditorLayoutUtilities.DrawTopOfSection("Default Placement Config");

            EditorGUILayout.PropertyField(_keepDefaultObjectScale);

            EditorGUILayout.PropertyField(_defaultItemPlacementConfig);

            EditorGUILayout.Space();

            if (!_isAddingNewPlacementConfig)
            {
                if (GUILayout.Button("Configure Default Item Placement"))
                {
                    _isEditingDefultPlacementConfig = true;
                    _isAddingNewPlacementConfig = true;
                }
            }

            EditorLayoutUtilities.DrawBottomOfSection();
        }

        private void DrawItemPlacementConfigsSection()
        {
            bool shouldCreateNewPlacementConfig = false;

            EditorLayoutUtilities.DrawTopOfSection("Item Placement Configs");

            _shouldDrawSaveConfigurationButton = false;

            if (_placementProfileType.enumValueIndex == (int)PlacementProfileType.SCRIPTABLE_OBJECT)
            {
                DrawPlacementProfileScriptableObjectFields(ref shouldCreateNewPlacementConfig);
            }
            else if (_placementProfileType.enumValueIndex == (int)PlacementProfileType.LOCAL)
            {
                DrawLocalPlacementProfileFields();
            }

            EditorLayoutUtilities.DrawBottomOfSection();


            // The Create New Placement Profile functionality needs to happen outside of the GUILayout.EndVertical(); sections

            if (shouldCreateNewPlacementConfig)
            {
                CreateNewPlacementProfile();
            }

        }

        private void DrawConfigureItemPlacementRegion()
        {
            EditorLayoutUtilities.DrawTopOfSection(_isEditingDefultPlacementConfig ? "Configure Default Item Placement" : "Configure Item Placement");

            if (!_isAddingNewPlacementConfig)
            {
                if (_placementProfileType.enumValueIndex != (int)PlacementProfileType.SCRIPTABLE_OBJECT || _socket.SocketPlacementProfileScriptableObject != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Configure Item Placement", GUILayout.Height(EditorGUIUtility.singleLineHeight * 1.5f), GUILayout.Width(220f)))
                    {
                        _isEditingDefultPlacementConfig = false;
                        _isAddingNewPlacementConfig = true;
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (_isEditingDefultPlacementConfig || _isAddingNewPlacementConfig)
            {
                DisplayConfigureItemPlacementOption();
            }

            EditorLayoutUtilities.DrawBottomOfSection();
        }

        private void DrawPlacementProfileScriptableObjectFields(ref bool shouldCreateNewPlacementConfig)
        {
            EditorGUILayout.PropertyField(_placementProfileScriptableObject);
            serializedObject.ApplyModifiedProperties();

            if (_socket.SocketPlacementProfileScriptableObject == null)
            {
                DrawAddPlacementProfileButton(ref shouldCreateNewPlacementConfig);

                if (_placementProfileScriptableObjectEditor != null)
                {
                    DestroyImmediate(_placementProfileScriptableObjectEditor);
                    _placementProfileScriptableObjectEditor = null;
                }
            }
            else
            {
                if (_placementProfileScriptableObjectEditor == null ||
                    _placementProfileScriptableObjectEditor.target != _socket.SocketPlacementProfileScriptableObject)
                {
                    if (_placementProfileScriptableObjectEditor != null)
                    {
                        DestroyImmediate(_placementProfileScriptableObjectEditor);
                    }
                    _placementProfileScriptableObjectEditor = CreateEditor(_socket.SocketPlacementProfileScriptableObject);
                }

                _placementProfileScriptableObjectEditor?.OnInspectorGUI();
            }
        }


        private void DrawLocalPlacementProfileFields()
        {
            EditorGUILayout.Space();

            // Find the _placementProfile property
            SerializedProperty placementProfileProperty = serializedObject.FindProperty("_placementProfile");

            if (placementProfileProperty == null)
            {
                Debug.LogError("_placementProfile property not found");
                return;
            }
            // Apply the Tab Index Change
            serializedObject.ApplyModifiedProperties();

            // Update the _placementProfile property
            serializedObject.Update();

            // Draw the ItemPlacementConfigs
            EditorGUILayout.PropertyField(placementProfileProperty.FindPropertyRelative("ItemPlacementConfigs"), true);

            // Apply the changes to the _placementProfile property
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
        }

        private void DrawAddPlacementProfileButton(ref bool shouldCreateNewPlacementConfig)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayoutOption narrowOption = GUILayout.Width(250);

            if (GUILayout.Button("Create New Placement Profile", narrowOption))
            {
                shouldCreateNewPlacementConfig = true;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void CreateNewPlacementProfile()
        {
            string defaultFileName = "NewSocketPlacementProfile.asset";
            string path = EditorUtility.SaveFilePanel("Save Socket Placement Profile", "Assets", defaultFileName, "asset");

            if (!string.IsNullOrEmpty(path))
            {
                path = FileUtil.GetProjectRelativePath(path);

                SocketPlacementProfileScriptableObject newProfile = ScriptableObject.CreateInstance<SocketPlacementProfileScriptableObject>();
                AssetDatabase.CreateAsset(newProfile, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorGUIUtility.PingObject(newProfile);

                _socket.SetPlacementProfile(newProfile);
            }
        }

        private void DisplayConfigureItemPlacementOption()
        {
            EditorGUILayout.Space();

            if (!_isAddingNewPlacementConfig)
            {
                if (GUILayout.Button("Configure Item Placement", GUIStyles.LargeButtonStyle))
                {
                    _isEditingDefultPlacementConfig = false;
                    _isAddingNewPlacementConfig = true;
                }
            }

            if (_isAddingNewPlacementConfig)
            {
                DrawConfigureItemPlacement();

                DrawClonePlacementRegionOptions();

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                DrawActionPlacementButtons();
            }
        }

        private void DrawConfigureItemPlacement()
        {
            bool _showNonConfiguredItemsOnlyChanged = false;
            string currentTag = _placementConfig.Name;

            string[] tagsToDisplay = _tags;

            if (_showNonConfiguredItemsOnly)
            {
                tagsToDisplay = _tags.Where(tag => !_socket.SocketPlacementProfile.ItemPlacementConfigs.Exists(config => config.Name == tag)).ToArray();
            }

            int oldTagIndex = System.Array.IndexOf(tagsToDisplay, currentTag);
            int newTagIndex = oldTagIndex;


            if (!_isEditingDefultPlacementConfig)
            {
                newTagIndex = EditorGUILayout.Popup("Placeable Item Tag", oldTagIndex, tagsToDisplay);

                bool _oldShowNonConfiguredItemsOnly = _showNonConfiguredItemsOnly;
                _showNonConfiguredItemsOnly = EditorGUILayout.Toggle("Show Non-Configured Items Only", _showNonConfiguredItemsOnly);

                if (_oldShowNonConfiguredItemsOnly != _showNonConfiguredItemsOnly)
                {
                    _showNonConfiguredItemsOnlyChanged = true;
                }
            }

            // If we've changed the item tag
            if (newTagIndex != -1 && (newTagIndex != oldTagIndex || _showNonConfiguredItemsOnlyChanged))
            {
                string newItemTag = tagsToDisplay[newTagIndex];

                _placementConfig.Name = newItemTag;

                StopPosing();

                PoseItem(newItemTag);
            }

            if (newTagIndex != -1)
            {
                DrawConfigureItemPlacementOptions(_placementConfig);
                _shouldDrawSaveConfigurationButton = true;
            }
            else if (_isEditingDefultPlacementConfig) // If setting default config 
            {
                if (string.IsNullOrEmpty(_placementConfig.Name))
                {
                    _placementConfig.Name = "DEFAULT";
                    _placementConfig.PlacedPosition = _socket.DefaultItemPlacementConfig.PlacedPosition;
                    _placementConfig.PlacedRotation = _socket.DefaultItemPlacementConfig.PlacedRotation;
                }

                DrawConfigureItemPlacementOptions(_placementConfig);
                _shouldDrawSaveConfigurationButton = true;
            }
        }

        private void DrawConfigureItemPlacementOptions(ItemPlacementConfig placementConfig)
        {
            DrawPosingObjectFields(placementConfig);
        }

        private void DrawPositionItemButton(ItemPlacementConfig placementConfig)
        {
            EditorGUILayout.Space();

            if (GUILayout.Button("Pose Item"))
            {
                PoseItem(placementConfig.Name);
            }

            EditorGUILayout.Space();
        }

        private void DrawnCancelConfigurationButton()
        {
            if (GUILayout.Button("Cancel Configuration", GUILayout.Height(EditorGUIUtility.singleLineHeight * 1.5f), GUILayout.Width(220f)))
            {
                StopPosing();
                _isAddingNewPlacementConfig = false;
                _isEditingDefultPlacementConfig = false;
                _placementConfig = new ItemPlacementConfig();
            }
        }

        private void DrawCloneItemPlacementButton()
        {
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Clone Item Config", GUILayout.Height(EditorGUIUtility.singleLineHeight * 1.5f), GUILayout.Width(220f)))
            {
                _itemIndexToClone = -1;
                _cloneItemPlacement = true;

                if (_placeableItemPosingObject != null)
                {
                    StopPosing();
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPosingObjectFields(ItemPlacementConfig placementConfig)
        {
            if (_placeableItemPosingObject == null && !_cloneItemPlacement)
            {
                DrawPositionItemButton(placementConfig);
            }

            if (_placeableItemPosingObject != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                DrawTransformFields();
                DrawCancelPosingButton();
            }
        }

        private void DrawClonePlacementRegionOptions()
        {
            if (_cloneItemPlacement)
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                DrawCloneItemPlacementRegion(_placementConfig.Name);

                if (GUILayout.Button("Close"))
                {
                    _itemIndexToClone = -1;
                    _cloneItemPlacement = false;
                }

                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
        }

        private void DrawCloneItemPlacementRegion(string itemTag)
        {
            _itemIndexToClone = EditorGUILayout.Popup("Item To Clone", _itemIndexToClone, _tags);

            if (_itemIndexToClone == -1)
            {
                return;
            }

            string itemTagToClone = _tags[_itemIndexToClone];


            ItemPlacementConfig configToClone = _socket.SocketPlacementProfile.ItemPlacementConfigs.Find(x => x.Name.Equals(itemTagToClone));

            if (configToClone == null)
            {
                _cloneItemPlacement = false;
                return;
            }

            _itemIndexToClone = -1;
            _cloneItemPlacement = false;


            PoseItem(itemTag);

            _placeableItemPosingObject.transform.localPosition = configToClone.PlacedPosition;
            _placeableItemPosingObject.transform.localRotation = configToClone.PlacedRotation;
            _placeableItemPosingObject.transform.localScale = configToClone.PlacedScale;
        }

        private void DrawTransformFields()
        {
            EditorGUILayout.LabelField("Item placement", EditorStyles.boldLabel);

            Vector3 currentPosition = _placeableItemPosingObject.transform.localPosition;
            Vector3 currentRotation = _placeableItemPosingObject.transform.localEulerAngles;
            Vector3 currentScale = _placeableItemPosingObject.transform.localScale;

            DrawPositionField(currentPosition);
            DrawRotationField(currentRotation);
            DrawScaleField(currentScale);
        }

        private void DrawPositionField(Vector3 currentPosition)
        {
            EditorGUILayout.BeginHorizontal();
            Vector3 newPosition = EditorGUILayout.Vector3Field("Position", currentPosition);

            if (GUILayout.Button("Reset", GUILayout.Width(60)))
            {
                ResetPosition();
            }
            EditorGUILayout.EndHorizontal();

            if (newPosition != currentPosition)
            {
                _placeableItemPosingObject.transform.localPosition = newPosition;
            }
        }

        private void DrawRotationField(Vector3 currentRotation)
        {
            EditorGUILayout.BeginHorizontal();
            Vector3 newRotation = EditorGUILayout.Vector3Field("Rotation", currentRotation);

            if (GUILayout.Button("Reset", GUILayout.Width(60)))
            {
                ResetRotation();
            }
            EditorGUILayout.EndHorizontal();

            if (newRotation != currentRotation)
            {
                _placeableItemPosingObject.transform.localEulerAngles = newRotation;
            }
        }

        private void DrawScaleField(Vector3 currentScale)
        {
            EditorGUILayout.BeginHorizontal();
            Vector3 newScale = EditorGUILayout.Vector3Field("Scale", currentScale);

            if (GUILayout.Button("Reset", GUILayout.Width(60)))
            {
                _placeableItemPosingObject.transform.localScale = _originalPosingObjectScale;
            }
            EditorGUILayout.EndHorizontal();

            if (newScale != currentScale)
            {
                _placeableItemPosingObject.transform.localScale = newScale;
            }
        }

        private void DrawCancelPosingButton()
        {
            if (GUILayout.Button("Cancel Posing"))
            {
                StopPosing();
            }

            EditorGUILayout.Space();
        }

        private void DrawActionPlacementButtons()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (!_cloneItemPlacement && _shouldDrawSaveConfigurationButton && !_isEditingDefultPlacementConfig)
            {
                DrawCloneItemPlacementButton();
            }

            if (_shouldDrawSaveConfigurationButton)
            {
                DrawSaveConfigurationButton();
            }

            DrawnCancelConfigurationButton();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }


        private void DrawSaveConfigurationButton()
        {
            if (GUILayout.Button("Save Configuration", GUILayout.Height(EditorGUIUtility.singleLineHeight * 1.5f), GUILayout.Width(220f)))
            {
                SavePlacementConfiguration();
            }
        }

        private void StopPosing()
        {
            if (_placeableItemPosingObject != null)
            {
                DestroyImmediate(_placeableItemPosingObject);
            }

            Selection.activeGameObject = _socket.gameObject;
            EditorUtility.SetDirty(_socket.gameObject);
            ActiveEditorTracker.sharedTracker.isLocked = false;
        }

        private void ResetPosition()
        {
            _placeableItemPosingObject.transform.localPosition = Vector3.zero;
        }

        private void ResetRotation()
        {
            _placeableItemPosingObject.transform.localRotation = Quaternion.identity;
        }

        private void PoseItem(string itemTag)
        {
            GameObject posingPrefab = GetPosingPrefab(itemTag);

            if (_placeableItemPosingObject != null)
            {
                DestroyImmediate(_placeableItemPosingObject);
            }

            if (posingPrefab == null)
            {
                _placeableItemPosingObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _placeableItemPosingObject.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                _placeableItemPosingObject.transform.parent = _socket.SocketPlaceTransform.transform;
                _placeableItemPosingObject.transform.localPosition = Vector3.zero;

                _originalPosingObjectScale = _placeableItemPosingObject.transform.localScale;
            }
            else
            {
                _placeableItemPosingObject = Instantiate(posingPrefab,
                _socket.SocketPlaceTransform.transform.position,
                _socket.SocketPlaceTransform.transform.rotation);

                _originalPosingObjectScale = _placeableItemPosingObject.transform.localScale;
                _placeableItemPosingObject.transform.SetParent(_socket.SocketPlaceTransform.transform, true);
            }

            ItemPlacementConfig existingConfig = _socket.SocketPlacementProfile.ItemPlacementConfigs.Find(x => x.Name.Equals(itemTag));

            if (existingConfig != null)
            {
                _placeableItemPosingObject.transform.localPosition = existingConfig.PlacedPosition;
                _placeableItemPosingObject.transform.localRotation = existingConfig.PlacedRotation;
                _placeableItemPosingObject.transform.localScale = existingConfig.PlacedScale;
            }

            if (_isEditingDefultPlacementConfig)
            {
                _placeableItemPosingObject.transform.localPosition = _placementConfig.PlacedPosition;
                _placeableItemPosingObject.transform.localRotation = _placementConfig.PlacedRotation;
            }

            ActiveEditorTracker.sharedTracker.isLocked = true;
            Selection.activeGameObject = _placeableItemPosingObject;

            _placeableItemPosingObject.name = "Placeable Item Posing Object (Delete Me)";
        }

        private GameObject GetPosingPrefab(string itemTag)
        {
            PlaceableItemPosingData posingData = _ultimateSocketSettings.PlaceableItemPosingDatas.Find(x => x.ItemTag.Equals(itemTag));

            if (posingData == null)
            {
                return null;
            }

            if (posingData.PosingPrefab == null)
            {
                return null;
            }

            return posingData.PosingPrefab;
        }


        private void SavePlacementConfiguration()
        {
            if (_isEditingDefultPlacementConfig)
            {
                StoreDefaultItemPlacementConfig();
            }
            else
            {
                StoreItemPlacementConfig();
            }

            StopPosing();

            _isAddingNewPlacementConfig = false;
            _isEditingDefultPlacementConfig = false;
            _placementConfig = new ItemPlacementConfig();
        }

        private void StoreItemPlacementConfig()
        {
            ItemPlacementConfig existingConfig = _socket.SocketPlacementProfile.ItemPlacementConfigs.Find(x => x.Name.Equals(_placementConfig.Name));

            Vector3 placedPosition;
            Quaternion placedRotation;
            Vector3 placedScale;

            if (_placeableItemPosingObject == null)
            {
                placedPosition = existingConfig == null ? Vector3.zero : existingConfig.PlacedPosition;
                placedRotation = existingConfig == null ? Quaternion.identity : existingConfig.PlacedRotation;
                placedScale = existingConfig == null ? Vector3.one : existingConfig.PlacedScale;
            }
            else
            {
                placedPosition = _placeableItemPosingObject.transform.localPosition;
                placedRotation = _placeableItemPosingObject.transform.localRotation;

                bool hasPosingPrefab = GetPosingPrefab(_placementConfig.Name) != null;

                // Because we used a scaled down default object if there isn't a posing prefab we need to ignore the scale of the posing prefab
                if (hasPosingPrefab)
                {
                    placedScale = _placeableItemPosingObject.transform.localScale;
                }
                else
                {
                    placedScale = Vector3.one;
                }

            }

            ItemPlacementConfig itemPlacementConfig = new ItemPlacementConfig()
            {
                Name = _placementConfig.Name,
                PlacedPosition = placedPosition,
                PlacedRotation = placedRotation,
                PlacedScale = placedScale,
            };


            if (existingConfig == null)
            {
                _socket.SocketPlacementProfile.ItemPlacementConfigs.Add(itemPlacementConfig);
            }
            else
            {
                existingConfig.PlacedPosition = itemPlacementConfig.PlacedPosition;
                existingConfig.PlacedRotation = itemPlacementConfig.PlacedRotation;
                existingConfig.PlacedScale = itemPlacementConfig.PlacedScale;
            }

            if (_socket.PlacementProfileType == PlacementProfileType.SCRIPTABLE_OBJECT)
            {
                EditorUtility.SetDirty(_socket.SocketPlacementProfileScriptableObject);

                AssetDatabase.SaveAssets();
            }
            else
            {
                EditorUtility.SetDirty(_socket);
            }
        }

        private void StoreDefaultItemPlacementConfig()
        {
            ItemPlacementConfig itemPlacementConfig = new ItemPlacementConfig()
            {
                Name = _placementConfig.Name,
                PlacedPosition = _placeableItemPosingObject == null ? Vector3.zero : _placeableItemPosingObject.transform.localPosition,
                PlacedRotation = _placeableItemPosingObject == null ? Quaternion.identity : _placeableItemPosingObject.transform.localRotation,
            };

            _socket.SetDefaultItemPlacementConfig(itemPlacementConfig);
        }

        #endregion Item Placement Tab

        #region Highlighting Tab

        private void DrawHighlightingTab()
        {
            DrawCoreHighlightingRegion();

            if (!_matchPlacementDetectionCollider.boolValue)
            {
                DrawHighlighterColliderSettingsRegion();
            }

            DrawHighlightingListRegion();
            DrawSocketHighlighterCustomEditorsRegion();
        }

        private void DrawCoreHighlightingRegion()
        {
            EditorLayoutUtilities.DrawTopOfSection("Socket Highlighting Settings");

            EditorGUILayout.PropertyField(_useSocketHighlighter);

            bool _oldMatchPlacementDetectionCollider = _matchPlacementDetectionCollider.boolValue;

            EditorGUILayout.PropertyField(_matchPlacementDetectionCollider);

            if (_matchPlacementDetectionCollider.boolValue != _oldMatchPlacementDetectionCollider && _matchPlacementDetectionCollider.boolValue)
            {
                UpdateHighlightColliderToMatchPlacementDetectionCollider();
            }

            EditorLayoutUtilities.DrawBottomOfSection();
        }

        private void UpdateHighlightColliderToMatchPlacementDetectionCollider()
        {
            Collider placementDetectionCollider = _socket.SocketPlaceCollider.ColliderManager.Collider;

            if (placementDetectionCollider == null)
            {
                Debug.LogError("The Placement Detection Collider is null");
                return;
            }

            if (!_socket.SocketHighlightAreaCollider.ColliderManager.TypeMatches(placementDetectionCollider))
            {
                // Match the type
                if (placementDetectionCollider is SphereCollider)
                {
                    _socket.SocketHighlightAreaCollider.ColliderManager.RemoveColliders();
                    _socket.SocketHighlightAreaCollider.ColliderManager.AddSphereCollider();
                }
                else if (placementDetectionCollider is BoxCollider)
                {
                    _socket.SocketHighlightAreaCollider.ColliderManager.RemoveColliders();
                    _socket.SocketHighlightAreaCollider.ColliderManager.AddBoxCollider();
                }
            }

            // Match Size
            if (placementDetectionCollider is SphereCollider sphereCollider)
            {
                _socket.SocketHighlightAreaCollider.ColliderManager.SetColliderRadius(sphereCollider.radius);
                ((SphereCollider)_socket.SocketHighlightAreaCollider.ColliderManager.Collider).center = sphereCollider.center;
            }
            else if (placementDetectionCollider is BoxCollider boxCollider)
            {
                BoxCollider highlightBoxCollider = _socket.SocketHighlightAreaCollider.ColliderManager.Collider as BoxCollider;

                if (highlightBoxCollider != null)
                {
                    highlightBoxCollider.center = boxCollider.center;
                    highlightBoxCollider.size = boxCollider.size;
                }
            }

            _socket.SocketHighlightAreaCollider.ColliderManager.transform.localPosition = placementDetectionCollider.transform.localPosition;
        }

        private void DrawHighlighterColliderSettingsRegion()
        {
            ColliderConfigurationEditorUtilities.DrawColliderSettingsRegion("Highlight Area Collider Settings", _socket.SocketHighlightAreaCollider.ColliderManager, ref _adjustHighlightDetectionCollider);

            if (_adjustHighlightDetectionCollider && _adjustDetectionCollider)
            {
                _adjustDetectionCollider = false;
            }
        }

        private void DrawHighlightingListRegion()
        {
            EditorLayoutUtilities.DrawTopOfSection("Socket Highligher Components");

            List<string> socketHighlighterNames = GetSocketHighlighterNames();

            for (int i = 0; i < socketHighlighterNames.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                bool hasHighlighter = _socket.SocketHighlighter.HasHighlighter(socketHighlighterNames[i]);

                GUI.enabled = !hasHighlighter;

                EditorGUILayout.LabelField(socketHighlighterNames[i].SplitCamelCase());

                if (GUILayout.Button("Add", GUILayout.Width(40)))
                {
                    AddSocketHighlighter(socketHighlighterNames[i]);
                }

                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }

            EditorLayoutUtilities.DrawBottomOfSection();
        }

        private void AddSocketHighlighter(string highlighterName)
        {
            Type highlighterType = TypeCache.GetTypesDerivedFrom<ISocketHighlighter>()
                .FirstOrDefault(type => type.Name == highlighterName);

            if (highlighterType == null)
            {
                Debug.LogErrorFormat(this, "Could not find highlighter type: {0}", highlighterName);
                return;
            }

            _socket.SocketHighlighter.AddHighlighter(highlighterName);

            EditorUtility.SetDirty(_socket.gameObject);
            EditorUtility.SetDirty(_socket.SocketHighlighter.gameObject);
        }

        private void RemoveSocketHighlighter(string highlighterName)
        {
            Type highlighterType = TypeCache.GetTypesDerivedFrom<ISocketHighlighter>()
                .FirstOrDefault(type => type.Name == highlighterName);

            if (highlighterType == null)
            {
                Debug.LogErrorFormat(this, "Could not find highlighter type: {0}", highlighterName);
                return;
            }

            _socket.SocketHighlighter.RemoveHighlighter(highlighterName);

            EditorUtility.SetDirty(_socket.gameObject);
            EditorUtility.SetDirty(_socket.SocketHighlighter.gameObject);
        }

        private List<string> GetSocketHighlighterNames()
        {
            Type socketHighlighterType = typeof(ISocketHighlighter);
            TypeCache.TypeCollection derivedTypes = TypeCache.GetTypesDerivedFrom(socketHighlighterType);

            return derivedTypes
                .Where(type => type.IsClass && !type.IsAbstract)
                .Select(type => type.Name)
                .OrderBy(x => x)
                .ToList();
        }

        private void DrawSocketHighlighterCustomEditorsRegion()
        {
            if (_socket.SocketHighlighter == null)
            {
                Debug.LogErrorFormat(this, "Could not find Socket Highlighter on Socket: {0}", _socket.name);
                return;
            }

            EditorLayoutUtilities.DrawTopOfSection("Socket Highlighter Custom Editors");

            List<HighlighterEntry> socketHighlighters = _socket.SocketHighlighter.SocketHighlighters;

            for (int i = socketHighlighters.Count - 1; i >= 0; i--)
            {
                HighlighterEntry highlighterEntry = socketHighlighters[i];
                string highlighterName = highlighterEntry.HighlighterName;
                MonoBehaviour highlighter = highlighterEntry.HighlighterComponent;

                if (string.IsNullOrEmpty(highlighterName))
                {
                    Debug.LogErrorFormat(this, "Socket Highlighter Name is null or empty. Skipping highlighter: {0}", highlighter.GetType().Name);
                    continue;
                }

                if (highlighter == null)
                {
                    Debug.LogErrorFormat(this, "Socket Highlighter is null. Skipping highlighter: {0}", highlighterName);
                    continue;
                }

                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(highlighterName.SplitCamelCase(), GUIStyles.RegionNameStyle);

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    RemoveSocketHighlighter(highlighterName);
                    EditorGUILayout.EndHorizontal();
                    continue;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                if (!_highlighterEditors.TryGetValue(highlighter, out UnityEditor.Editor highlighterEditor))
                {
                    highlighterEditor = UnityEditor.Editor.CreateEditor(highlighter as UnityEngine.Object);
                    _highlighterEditors[highlighter] = highlighterEditor;
                }

                highlighterEditor.OnInspectorGUI();
            }


            EditorLayoutUtilities.DrawBottomOfSection();
        }

        #endregion Highlighting Tab

        #region Socket Placement Criteria Tab

        private void DrawSocketPlacementCriteriaTab()
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

                string criteriaLabel = SplitCamelCase(placementCriteriaNames[i]);

                bool hasCriteria = _socket.SocketPlacementCriteriaController.HasCriteria(placementCriteriaNames[i]);

                GUI.enabled = !hasCriteria;
                EditorGUILayout.LabelField(criteriaLabel);

                if (GUILayout.Button("Add", GUILayout.Width(40)))
                {
                    AddPlacementCriteria(placementCriteriaNames[i]);
                }

                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();
            }

            EditorLayoutUtilities.DrawBottomOfSection();
        }

        private string SplitCamelCase(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, "(?<!^)([A-Z][a-z]|(?<=[a-z])([A-Z]))", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
        }

        private List<string> GetPlacementCriteriaNames()
        {
            var placementCriteriaType = typeof(ISocketPlacementCriteria);
            var derivedTypes = TypeCache.GetTypesDerivedFrom(placementCriteriaType);

            return derivedTypes
                .Where(type => type.IsClass && !type.IsAbstract)
                .Select(type => type.Name)
                .OrderBy(x => x)
                .ToList();
        }

        private void AddPlacementCriteria(string criteriaName)
        {
            Type criteriaType = TypeCache.GetTypesDerivedFrom<ISocketPlacementCriteria>()
                .FirstOrDefault(type => type.Name == criteriaName);

            if (criteriaType == null)
            {
                Debug.LogErrorFormat(this, "Could not find criteria type: {0}", criteriaName);
                return;
            }

            _socket.SocketPlacementCriteriaController.AddPlacementCriteria(criteriaName);

            EditorUtility.SetDirty(_socket.gameObject);
            EditorUtility.SetDirty(_socket.SocketPlacementCriteriaController.gameObject);
        }

        private void RemovePlacementCriteria(string criteriaName)
        {
            Type criteriaType = TypeCache.GetTypesDerivedFrom<ISocketPlacementCriteria>()
             .FirstOrDefault(type => type.Name == criteriaName);

            if (criteriaType == null)
            {
                Debug.LogErrorFormat(this, "Could not find criteria type: {0}", criteriaName);
                return;
            }

            _socket.SocketPlacementCriteriaController.RemovePlacementCriteria(criteriaName);

            EditorUtility.SetDirty(_socket.gameObject);
            EditorUtility.SetDirty(_socket.SocketPlacementCriteriaController.gameObject);
        }

        private void DrawPlacementCriteriaCustomEditors()
        {
            if (_socket.SocketPlacementCriteriaController == null)
            {
                Debug.LogErrorFormat(this, "No SocketPlacementCriteriaContainer found on PlaceableItem: {0}", _socket.name);
                return;
            }

            EditorLayoutUtilities.DrawTopOfSection("Placement Criteria Custom Editors");

            List<CriteriaEntry> placementCriterias = _socket.SocketPlacementCriteriaController.Criterias;

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
                EditorGUILayout.LabelField(SplitCamelCase(criteraName), GUIStyles.RegionNameStyle);

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

        #endregion Socket Placement Criteria Tab

        #region Stacking Tab

        private void DrawStackingTab()
        {
            DrawCoreStackingSettings();

            if (!_isStackableProperty.boolValue)
                return;

            DrawStackingOptionsRegion();

            CreateStackingUIRegion();

            DrawStackSpawnTransitionRegion();
        }

        private void DrawCoreStackingSettings()
        {
            EditorLayoutUtilities.DrawTopOfSection("Stacking Settings");

            EditorGUILayout.PropertyField(_isStackableProperty, new GUIContent("Make Socket Stackable"));

            EditorLayoutUtilities.DrawBottomOfSection();
        }

        private void DrawStackingOptionsRegion()
        {
            EditorLayoutUtilities.DrawTopOfSection("Stacking Options");

            EditorGUILayout.PropertyField(_stackTypeProperty);

            if (_stackTypeProperty.enumValueIndex == (int)StackType.CLONE)
            {
                EditorGUILayout.PropertyField(_stackInfiniteReplacement);
            }


            EditorGUILayout.PropertyField(_maxStackSizeProperty);
            EditorGUILayout.HelpBox("CLONE - Destroys items added to the stack. When an item is removed a clone is spawned.\nINSTANCE - Persists items added to the stack. When an item is removed the original item is spawned", MessageType.Info);

            EditorGUILayout.PropertyField(_stackReplacementDelay);

            if (_stackTypeProperty.enumValueIndex == (int)StackType.INSTANCE)
            {
                EditorGUILayout.PropertyField(_instanceStackType);
                EditorGUILayout.HelpBox("FIFO - First In First Out.\nFILO - First In Last Out", MessageType.Info);
            }

            EditorLayoutUtilities.DrawBottomOfSection();
        }

        private void DrawStackSpawnTransitionRegion()
        {
            DrawStackingSpawnTransitionRegion();
            DrawSpawnTransitionCustomEditors();
        }

        private void DrawStackingSpawnTransitionRegion()
        {
            EditorLayoutUtilities.DrawTopOfSection("Spawn Transition Options");

            List<string> spawnTransitionNames = GetStackSpawnTransitionNames();

            for (int i = 0; i < spawnTransitionNames.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                bool hasSpawnTransition = _socket.StackableItemController.HasSpawnTransition(spawnTransitionNames[i]);
                GUI.enabled = !hasSpawnTransition;

                EditorGUILayout.LabelField(SplitCamelCase(spawnTransitionNames[i]));

                if (GUILayout.Button("Add", GUILayout.Width(40)))
                {
                    AddSpawnTransition(spawnTransitionNames[i]);
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

        private void AddSpawnTransition(string transitionType)
        {
            Type spawnType = TypeCache.GetTypesDerivedFrom<IStackSpawnTransition>()
                .FirstOrDefault(type => type.Name == transitionType);

            if (spawnType == null)
            {
                Debug.LogErrorFormat(this, "Could not find spawn transition type: {0}", transitionType);
                return;
            }

            _socket.StackableItemController.AddSpawnTransition(transitionType);

            EditorUtility.SetDirty(_socket.gameObject);
            EditorUtility.SetDirty(_socket.StackableItemController.gameObject);
        }

        private void RemoveSpawnTransition(string transitionType)
        {
            Type transitionName = TypeCache.GetTypesDerivedFrom<IStackSpawnTransition>()
             .FirstOrDefault(type => type.Name == transitionType);

            if (transitionName == null)
            {
                Debug.LogErrorFormat(this, "Could not find criteria type: {0}", transitionName);
                return;
            }

            _socket.StackableItemController.RemoveSpawnTransition(transitionType);

            EditorUtility.SetDirty(_socket.gameObject);
            EditorUtility.SetDirty(_socket.SocketPlacementCriteriaController.gameObject);
        }

        private void DrawSpawnTransitionCustomEditors()
        {
            if (_socket.StackableItemController == null)
            {
                Debug.LogErrorFormat(this, "No StackableItemController found on Socket: {0}", _socket.name);
                return;
            }

            EditorLayoutUtilities.DrawTopOfSection("Spawn Transition Custom Editors");

            List<SpawnTransitionEntry> spawnTransitions = _socket.StackableItemController.SpawnTransitionEntries;

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
                EditorGUILayout.LabelField(SplitCamelCase(spawnTransitionName), GUIStyles.RegionNameStyle);

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    RemoveSpawnTransition(spawnTransitionName);
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


        private void CreateStackingUIRegion()
        {
            EditorLayoutUtilities.DrawTopOfSection("Stacking UI");

            if (_socket.StackableItemController.StackCounterUI == null)
            {
                EditorGUILayout.HelpBox("This button will create an example of some UI for stacked items. It's recommended to create your own. See the documentation for more details", MessageType.Info);

                if (GUILayout.Button("Create Stacking UI"))
                {
                    CreateStackingUI();
                }
            }
            else
            {
                if (GUILayout.Button("Delete Stacking UI"))
                {
                    DestroyImmediate(_socket.StackableItemController.StackCounterUI);
                }
            }

            EditorLayoutUtilities.DrawBottomOfSection();
        }

        private void CreateStackingUI()
        {
            _socket.StackableItemController.StackCounterUI = UltimateSocketUtilities.CreateStackUIAsync(_socket);
        }

        #endregion Stacking Tab

        #region Audio

        public void DrawAudioTab()
        {
            DrawCoreAudioSettingsRegion();

            if (!_useAudio.boolValue)
                return;

            DrawAudioClipsRegion();

            // There seems to be a bug with Unity's AudioSource custom editor when displayed on another gameobject to the one that holds the AudioSource component. 
            // For now a redirction button is used to prevent this.
            // DrawAudioSourceRegion(); 

            DrawGoToAudioSourceButton();
        }

        private void DrawCoreAudioSettingsRegion()
        {
            EditorLayoutUtilities.DrawTopOfSection("Audio Settings");

            bool oldUseAudio = _useAudio.boolValue;

            EditorGUILayout.PropertyField(_useAudio, new GUIContent("Use Socket Audio"));

            _socketAudioControllerSerializedObject?.ApplyModifiedProperties();

            if (oldUseAudio != _useAudio.boolValue)
            {
                _socket.SocketAudioController.HandleUseAudioChanged();
            }

            _socketAudioControllerSerializedObject?.Update();

            EditorLayoutUtilities.DrawBottomOfSection();
        }

        private void DrawAudioClipsRegion()
        {
            EditorLayoutUtilities.DrawTopOfSection("Audio Clips");

            EditorGUILayout.PropertyField(_placeAudioClip);
            EditorGUILayout.PropertyField(_removeAudioClip);

            EditorLayoutUtilities.DrawBottomOfSection();
        }

        public void DrawGoToAudioSourceButton()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Go To Audio Source", GUILayout.Height(EditorGUIUtility.singleLineHeight * 1.5f), GUILayout.Width(220f)))
            {

                Selection.activeGameObject = _socket.SocketAudioController.gameObject;
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAudioSourceRegion()
        {
            EditorLayoutUtilities.DrawTopOfSection("Audio Source");

            if (_socket.SocketAudioController == null || _socket.SocketAudioController.AudioSource == null)
            {
                EditorLayoutUtilities.DrawBottomOfSection();
                return;
            }

            if (_audioSourceEditor == null)
            {
                _audioSourceEditor = UnityEditor.Editor.CreateEditor(_socket.SocketAudioController.AudioSource);
            }

            if (_audioSourceEditor != null)
            {
                _audioSourceEditor.OnInspectorGUI();
            }

            EditorLayoutUtilities.DrawBottomOfSection();
        }

        #endregion Audio
    }
}
