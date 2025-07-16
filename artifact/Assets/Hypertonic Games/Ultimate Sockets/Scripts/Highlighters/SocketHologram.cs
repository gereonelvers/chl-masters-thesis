using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hypertonic.Modules.UltimateSockets.Enums;
using Hypertonic.Modules.UltimateSockets.Models;
using Hypertonic.Modules.UltimateSockets.Models.ScriptableObjects;
using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using Hypertonic.Modules.UltimateSockets.PlaceableItems.PlacementCriterias;
using Hypertonic.Modules.UltimateSockets.Sockets;
using Hypertonic.Modules.UltimateSockets.Sockets.PlacementCriterias;
using Hypertonic.Modules.Utilities;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Highlighters
{
    public class SocketHologram : MonoBehaviour, ISocketHighlighter
    {
        private const string _DEFAULT_PLACEABLE_MATERIAL_ASSET_PATH = "Assets/Hypertonic Games/Ultimate Sockets/Materials/[UltimateSockets] PlaceableHolgram.mat";
        private const string _DEFAULT_NON_PLACEABLE_MATERIAL_ASSET_PATH = "Assets/Hypertonic Games/Ultimate Sockets/Materials/[UltimateSockets] NonPlaceableHolgram.mat";

        public HologramMode HologramMode => _hologramMode;

        [SerializeField]
        private HologramMode _hologramMode = HologramMode.Dynamic;


        [SerializeField]
        private GameObject _hologramGameObject;

        [SerializeField]
        private GameObject _hologramPrefab;

        [SerializeField]
        private GameObject _itemSpecificDefaultPrefab;

        [SerializeField]
        private ItemSpecificDefaultItemType _itemSpecificDefaultItemType = ItemSpecificDefaultItemType.Dynamic;

        [SerializeField]
        private Material _placeableHologramMaterial;

        [SerializeField]
        private Material _nonPlaceableHologramMaterial;

        [SerializeField]
        private SocketHighlighter _socketHighlighter;

        [SerializeField]
        private HologramPrefabSettings _itemSpecificHologramPrefabSettings;

        private Socket _socket => _socketHighlighter.Socket;

        private GameObject _activeHologramGameObject;

        private PlaceableItem _placeableItemHighlighting;
        private Coroutine _checkPlaceableStateCoroutine;

        #region Unity Functions

        private void Awake()
        {
            ValidateReferences();
        }

        #endregion Unity Functions

        #region Public Functions

        #region ISocketHighlighter Implementations

        public void StartHighlight(PlaceableItem placeableItem)
        {
            _placeableItemHighlighting = placeableItem;

            CreateHighlightObject(placeableItem);

            if (_hologramMode == HologramMode.Dynamic)
            {
                StopCheckPlaceableStateCoroutineIfActive();

                _checkPlaceableStateCoroutine = StartCoroutine(CheckPlaceableStateCoroutine());
            }
        }

        public void StopHighlight()
        {
            StopCheckPlaceableStateCoroutineIfActive();

            if (_activeHologramGameObject != null)
            {
                Destroy(_activeHologramGameObject);
            }

            _placeableItemHighlighting = null;
        }

        public void Setup(SocketHighlighter socketHighlighter)
        {
            _socketHighlighter = socketHighlighter;

#if UNITY_EDITOR
            LoadDefaultMaterials();
#endif
        }

        #endregion ISocketHighlighter Implementations

        #endregion Public Functions

        #region Private Functions

        private void CreateHighlightObject(PlaceableItem placeableItem)
        {
            switch (_hologramMode)
            {
                case HologramMode.Dynamic:
                    CreateGameObjectForDynamicHologram(placeableItem); break;
                case HologramMode.SinglePrefab:
                    CreateGameObjectForSinglePrefabHologram(placeableItem); break;
                case HologramMode.ItemSpecific:
                    CreateGameObjectForItemSpecificHologram(placeableItem); break;
                case HologramMode.GameObject:
                    CreateGameObjectForGameObjectHologram(placeableItem); break;
            }
        }

        private void CreateGameObjectForDynamicHologram(PlaceableItem placeableItem)
        {
            _activeHologramGameObject = ComponentStripperManager.DuplicateAndStrip(placeableItem.RootTransform.gameObject, new List<Type> { typeof(MeshRenderer), typeof(MeshFilter) });

            _activeHologramGameObject.name += "Hologram";

            SetHologramPosition(_activeHologramGameObject, placeableItem.ItemTag);

            SetHologramMaterial(_activeHologramGameObject, _placeableHologramMaterial);
        }

        private void CreateGameObjectForItemSpecificHologram(PlaceableItem placeableItem)
        {
            GameObject itemSpecificHologramPrefab = GetGameObjectForItemSpecificHologram(placeableItem);

            if (itemSpecificHologramPrefab == null)
            {
                Debug.LogErrorFormat(this, "No hologram prefab found for item {0}", placeableItem.ItemTag);
                return;
            }

            _activeHologramGameObject = Instantiate(itemSpecificHologramPrefab);
            SetHologramPosition(_activeHologramGameObject, placeableItem.ItemTag);
        }

        private GameObject GetGameObjectForItemSpecificHologram(PlaceableItem placeableItem)
        {
            if (_itemSpecificHologramPrefabSettings == null)
            {
                Debug.LogErrorFormat(this, "The _itemSpecificHologramPrefabSettings is null, please set it in the inspector.");
                return null;
            }

            HologramPrefabConfig hologramPrefabConfig = _itemSpecificHologramPrefabSettings.HologramPrefabConfigs
            .Where(config => config.ItemTag.Equals(placeableItem.ItemTag))
            .FirstOrDefault();

            if (hologramPrefabConfig == null)
            {
                return _itemSpecificDefaultPrefab;
            }

            return hologramPrefabConfig.Prefab;
        }

        private void CreateGameObjectForSinglePrefabHologram(PlaceableItem placeableItem)
        {
            _activeHologramGameObject = Instantiate(_hologramPrefab);

            SetHologramPosition(_activeHologramGameObject, placeableItem.ItemTag);
        }


        private void CreateGameObjectForGameObjectHologram(PlaceableItem placeableItem)
        {
            _activeHologramGameObject = Instantiate(_hologramGameObject);
            _activeHologramGameObject.SetActive(true);

            SetHologramPosition(_activeHologramGameObject, placeableItem.ItemTag);
        }

        private void ValidateReferences()
        {
            if (_socket == null)
            {
                Debug.LogError("The socket is null", this);
            }

            if (_socketHighlighter == null)
            {
                Debug.LogError("Could not obtain a reference to the SocketHighlighter component on gameobject: [" + gameObject.name + "]", this);
            }

            CheckSinglePrefabMode();
            CheckGameObjectMode();
            CheckDynamicMode();
            CheckItemSpecificMode();
        }

        private void CheckSinglePrefabMode()
        {
            if (_hologramMode.Equals(HologramMode.SinglePrefab) && _hologramPrefab == null)
            {
                Debug.LogError("The hologram prefab is null", this);
            }
        }

        private void CheckGameObjectMode()
        {
            if (_hologramMode.Equals(HologramMode.GameObject) && _hologramGameObject == null)
            {
                Debug.LogError("The highlight game object is null", this);
            }
        }

        private void CheckDynamicMode()
        {
            if (_hologramMode.Equals(HologramMode.Dynamic) && _placeableHologramMaterial == null)
            {
                Debug.LogError("The hologram material is null", this);
            }
        }

        private void CheckItemSpecificMode()
        {
            if (_hologramMode.Equals(HologramMode.ItemSpecific) && _itemSpecificDefaultItemType.Equals(ItemSpecificDefaultItemType.DefaultPrefab) && _itemSpecificDefaultPrefab == null)
            {
                Debug.LogError("The item specific default prefab is null", this);
            }
        }

        private void SetHologramPosition(GameObject hologramGameObject, string placeableItemTag)
        {
            hologramGameObject.transform.SetParent(_socket.SocketPlaceTransform.transform, false);

            TransformData socketTransformData = _socket.SocketPlaceTransform.GetPlacementPositionForItem(placeableItemTag);

            hologramGameObject.transform.localPosition = socketTransformData.Position;
            hologramGameObject.transform.localRotation = socketTransformData.Rotation;

            if (_socket.SocketPlaceTransform.HasPlacementPositionForItem(placeableItemTag))
            {
                hologramGameObject.transform.localScale = socketTransformData.Scale;
            }
        }

        private void SetHologramMaterial(GameObject hologramGameObject, Material hologramMaterial)
        {
            if (_placeableHologramMaterial == null)
            {
                Debug.LogError("The hologram material is null", this);
                return;
            }

            // Get all the mesh renderers in the hologram object and its children
            MeshRenderer[] meshRenderers = hologramGameObject.GetComponentsInChildren<MeshRenderer>();

            // Loop through each mesh renderer
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                // Get the number of material slots in the mesh renderer
                int materialCount = meshRenderer.sharedMaterials.Length;

                // Create a new array to store the hologram materials
                Material[] hologramMaterials = new Material[materialCount];

                // Fill the array with the hologram material
                for (int i = 0; i < materialCount; i++)
                {
                    hologramMaterials[i] = hologramMaterial;
                }

                // Set the materials on the mesh renderer
                meshRenderer.sharedMaterials = hologramMaterials;
            }
        }

        private IEnumerator CheckPlaceableStateCoroutine()
        {
            List<string> placementCriteriaNamesToIgnore = new List<string> { typeof(PlaceableItems.PlacementCriterias.NotHoldingItem).Name, typeof(Sockets.PlacementCriterias.NotHoldingItem).Name };

            if (!_hologramMode.Equals(HologramMode.Dynamic))
                yield break;

            bool itemIsPlaceable = _socketHighlighter.Socket.CanPlace(_placeableItemHighlighting, placementCriteriaNamesToIgnore);

            UpdateHologramMaterial(itemIsPlaceable);

            while (_placeableItemHighlighting != null)
            {
                bool newItemIsPlaceable = _socketHighlighter.Socket.CanPlace(_placeableItemHighlighting, placementCriteriaNamesToIgnore);

                if (itemIsPlaceable != newItemIsPlaceable)
                {
                    itemIsPlaceable = newItemIsPlaceable;

                    UpdateHologramMaterial(itemIsPlaceable);
                }

                yield return null;
            }
        }

        private void StopCheckPlaceableStateCoroutineIfActive()
        {
            if (_checkPlaceableStateCoroutine != null)
            {
                StopCoroutine(_checkPlaceableStateCoroutine);
                _checkPlaceableStateCoroutine = null;
            }
        }

        private void UpdateHologramMaterial(bool isPlaceable)
        {
            if (isPlaceable)
            {
                SetHologramMaterial(_activeHologramGameObject, _placeableHologramMaterial);
            }
            else
            {
                SetHologramMaterial(_activeHologramGameObject, _nonPlaceableHologramMaterial);
            }
        }


        #endregion Private Functions

        #region Editor Functions

#if UNITY_EDITOR

        private void LoadDefaultMaterials()
        {
            if (_placeableHologramMaterial == null)
            {
                LoadDefaultPlaceableHologramMaterial();
            }

            if (_nonPlaceableHologramMaterial == null)
            {
                LoadDefaultNonPlaceableHologramMaterial();
            }
        }

        private void LoadDefaultPlaceableHologramMaterial()
        {
            _placeableHologramMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(_DEFAULT_PLACEABLE_MATERIAL_ASSET_PATH);

            if (_placeableHologramMaterial == null)
            {
                Debug.LogWarning("Failed to load the default placeable hologram material at path: " + _DEFAULT_PLACEABLE_MATERIAL_ASSET_PATH);
            }
        }

        private void LoadDefaultNonPlaceableHologramMaterial()
        {
            _nonPlaceableHologramMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(_DEFAULT_NON_PLACEABLE_MATERIAL_ASSET_PATH);

            if (_nonPlaceableHologramMaterial == null)
            {
                Debug.LogWarning("Failed to load the default non placeable hologram material at path: " + _DEFAULT_NON_PLACEABLE_MATERIAL_ASSET_PATH);
            }
        }
#endif

        #endregion Editor Functions
    }
}
