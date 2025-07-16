using Hypertonic.Modules.UltimateSockets.Interfaces;
using Hypertonic.Modules.UltimateSockets.Models;
using Hypertonic.Modules.UltimateSockets.PlaceableItems.PlacementCriterias;
using Hypertonic.Modules.UltimateSockets.Sockets;
using Hypertonic.Modules.UltimateSockets.Sockets.PlacementCriterias;
using Hypertonic.Modules.UltimateSockets.Tweening;
using Hypertonic.Modules.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.PlaceableItems
{
    public class PlaceableItemPreviewController : MonoBehaviour
    {
        public GameObject PreviewObjectPrefab => _previewObjectPrefab;

        public string PreviewObjectSaveFilePath => _previewObjectSaveFilePath;

        public bool UseRuntimePreviewObject => _useRuntimePreviewObject;

        [SerializeField]
        private GameObject _previewObjectPrefab;

        [SerializeField]
        private float _placeSpeed = 0.1f;

        [SerializeField]
        private float _unplaceSpeed = 0.1f;

        [SerializeField]
        private AnimationCurve _placeSpeedAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [SerializeField]
        private AnimationCurve _unplaceSpeedAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [SerializeField]
        [Tooltip("The path to the file where the preview object will be saved when generated from the editor tool")]
        private string _previewObjectSaveFilePath = "Assets/";

        [SerializeField]
        private bool _useRuntimePreviewObject = true;

        private Socket _closestSocket;

        private GameObject _previewObject;

        private PlaceableItemSocketScaler _placeableItemSocketScaler;

        private PlaceableItem _placeableItem;

        private Tweener _movementTweener;
        private Tweener _rotationTweener;
        private Tweener _scaleTweener;

        private Coroutine _tweenPreviewObjectToHandCoroutine;

        private Coroutine _handleEnteredPlaceableZoneCoroutine;

        #region Unity Functions

        private void Awake()
        {
            if (!transform.parent.TryGetComponent(out _placeableItem))
            {
                Debug.LogError("Could not obtain a reference to the PlaceableItem componment", this);
                return;
            }

            if (!TryGetComponent(out _placeableItemSocketScaler))
            {
                Debug.LogError("Could not obtain a reference to the PlaceableItemSocketScaler componment", this);
                return;
            }

            if (!_placeableItem.UsePreviewController)
                return;

            if (_previewObjectPrefab == null && !_useRuntimePreviewObject)
            {
                Debug.LogError("_previewObjectPrefab has not been assigned", this);
                return;
            }
        }

        private void OnEnable()
        {
            _placeableItem.OnRemovedFromSocket += HandleRemovedFromSocket;
            _placeableItem.OnPlaced += HandlePlaced;
        }

        private void OnDisable()
        {
            _placeableItem.OnRemovedFromSocket -= HandleRemovedFromSocket;
            _placeableItem.OnPlaced -= HandlePlaced;
        }

        private void OnDestroy()
        {
            DestroyPreviewObjectIfActive();
        }

        #endregion Unity Functions

        #region Public Functions

        public void HandleEnteredPlaceableZone(Socket socket)
        {
            if (!_placeableItem.UsePreviewController)
                return;

            if (socket.PreventPreviewItems)
                return;

            if (_previewObjectPrefab == null && !_useRuntimePreviewObject)
            {
                Debug.LogError("_previewObjectPrefab has not been assigned", this);
                return;
            }

            List<string> placementCriteriaNamesToIgnore = new List<string> { typeof(PlacementCriterias.NotHoldingItem).Name, typeof(Sockets.PlacementCriterias.NotHoldingItem).Name };

            if (!socket.CanPlace(_placeableItem, placementCriteriaNamesToIgnore))
                return;

            StopHandleEnteredPlaceableZoneCoroutineIfActive();

            _handleEnteredPlaceableZoneCoroutine = StartCoroutine(HandleEnteredPlaceableZoneCoroutine(socket));
        }

        private IEnumerator HandleEnteredPlaceableZoneCoroutine(Socket socket)
        {
            if (_closestSocket != null)
            {
                ClearClosestSocket();
            }

            if (_previewObject != null)
            {
                _placeableItem.PlaceableItemMeshController.EnableMeshRenderers();
                StopCurrentTweensIfActive();
                Destroy(_previewObject);
            }

            StopTweenPreviewObjectToHandCoroutineIfActive();

            SetClosestSocket(socket);

            yield return new WaitForFixedUpdate();

            _previewObject = SpawnPreviewObject(_placeableItem.transform.position, _placeableItem.transform.rotation, socket.SocketPlaceTransform.transform, socket);

            TransformData socketTransformData = socket.SocketPlaceTransform.GetPlacementPositionForItem(_placeableItem.ItemTag);

            bool socketHasConfiguredPlacementPositionForItem = socket.SocketPlaceTransform.HasPlacementPositionForItem(_placeableItem.ItemTag);

            if (!socketHasConfiguredPlacementPositionForItem && (_placeableItem.KeepScaleForDefaultPlacements || socket.SocketPlaceTransform.KeepDefaultScale()))
            {
                socketTransformData.Scale = _previewObject.transform.localScale;
            }

            if (IsZeroQuaternion(socketTransformData.Rotation))
            {
                socketTransformData.Rotation = Quaternion.identity;
            }

            TweenPreviewObjectToSocket(socketTransformData.Position, socketTransformData.Rotation, socketTransformData.Scale, _previewObject.transform.localScale);

            _placeableItem.PlaceableItemMeshController.DisableMeshRenderers();
        }

        public void HandleMovedAwayFromSocket(Socket socket)
        {
            ClearClosestSocket();

            StopHandleEnteredPlaceableZoneCoroutineIfActive();
            StopCurrentTweensIfActive();
            StopTweenPreviewObjectToHandCoroutineIfActive();

            if (socket == null || socket.PreventPreviewItems || !_placeableItem.UsePreviewController)
            {
                return;
            }

            _tweenPreviewObjectToHandCoroutine = StartCoroutine(TweenPreviewObjectFromSocketCoroutine());
        }

        #endregion Public Functions

        #region Private Functions

        private void HandleRemovedFromSocket(Socket socket, PlaceableItem placeableItem)
        {
            if (!_placeableItem.UsePreviewController)
                return;

            StopTweenPreviewObjectToHandCoroutineIfActive();

            _tweenPreviewObjectToHandCoroutine = StartCoroutine(TweenPreviewObjectFromSocketCoroutine());
        }

        private void HandlePlaced(Socket socket, PlaceableItem placeableItem)
        {
            if (!_placeableItem.UsePreviewController)
                return;

            DestroyPreviewObjectIfActive();
            _placeableItem.PlaceableItemMeshController.EnableMeshRenderers();
        }

        private void ClearClosestSocket()
        {
            _closestSocket = null;
        }

        private void SetClosestSocket(Socket socket)
        {
            StopCurrentTweensIfActive();
            DestroyPreviewObjectIfActive();

            _closestSocket = socket;
        }

        private GameObject SpawnPreviewObject(Vector3 spawnPosition, Quaternion spawnRotation, Transform placePointTransform, Socket socket)
        {
            GameObject previewObject;

            if (UseRuntimePreviewObject)
            {
                previewObject = ComponentStripperManager.DuplicateAndStrip(_placeableItem.RootTransform.gameObject, new List<Type>() { typeof(MeshFilter), typeof(MeshRenderer), typeof(SkinnedMeshRenderer) });
                previewObject.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            }
            else
            {
                previewObject = Instantiate(_previewObjectPrefab, spawnPosition, spawnRotation);
            }

            HandlePreviewObjectSpawned(previewObject, socket);

            previewObject.SetActive(true);

            previewObject.transform.SetParent(placePointTransform, true);
            previewObject.name = "Preview Object";

            return previewObject;
        }

        private void HandlePreviewObjectSpawned(GameObject previewObject, Socket socket)
        {
            if (!previewObject.TryGetComponent(out IPreviewObjectSpawnHandler previewObjectSpawnHandler))
                return;

            previewObjectSpawnHandler.HandlePreviewObjectSpawned(previewObject, _placeableItem, socket);
        }


        private void TweenPreviewObjectToSocket(Vector3 targetPosition, Quaternion targetRotation, Vector3 targetScale, Vector3 localScale)
        {
            _previewObject.transform.localScale = localScale;

            TweenPreviewObjectToPosition(targetPosition, targetRotation, targetScale, _placeSpeedAnimationCurve);
        }

        private void TweenPreviewObjectToPosition(Vector3 targetPosition, Quaternion targetRotation, Vector3 targetScale, AnimationCurve animationCurve)
        {
            _movementTweener = Tweener.TweenVector3(_previewObject.transform.localPosition, targetPosition, _placeSpeed, (value) =>
            {
                if (_previewObject != null)
                    _previewObject.transform.localPosition = value;
            },
            animationCurve);

            _rotationTweener = Tweener.TweenQuaternion(_previewObject.transform.localRotation, targetRotation, _placeSpeed, (value) =>
            {
                if (_previewObject != null)
                    _previewObject.transform.localRotation = value;
            },
            animationCurve);

            _scaleTweener = Tweener.TweenVector3(_previewObject.transform.localScale, targetScale, _placeSpeed, (value) =>
            {
                if (_previewObject != null)
                    _previewObject.transform.localScale = value;
            },
            animationCurve);
        }


        private IEnumerator TweenPreviewObjectFromSocketCoroutine()
        {
            Vector3 targetScale = _placeableItem.RootTransform.localScale;

            if (!_useRuntimePreviewObject)
            {
                targetScale = _previewObjectPrefab.transform.localScale;
            }

            if (_placeableItemSocketScaler.OriginalLocalScale.HasValue)
            {
                targetScale = _placeableItemSocketScaler.OriginalLocalScale.Value;
            }

            yield return TweenPreviewObjectFromSocketCoroutine(targetScale, _unplaceSpeedAnimationCurve);
        }

        private IEnumerator TweenPreviewObjectFromSocketCoroutine(Vector3 targetScale, AnimationCurve animationCurve)
        {
            if (_previewObject == null)
            {
                Debug.LogWarning("Preview object is null. Cannot tween preview object from socket");

                if (_placeableItem.PlaceableItemMeshController.HasActiveRenderers)
                {
                    _placeableItem.PlaceableItemMeshController.EnableMeshRenderers();
                }

                _closestSocket = null;

                yield break;
            }

            Vector3 startPosition = _previewObject.transform.position;
            Quaternion startRotation = _previewObject.transform.rotation;
            Vector3 startScale = _previewObject.transform.localScale;

            _movementTweener = Tweener.TweenFloat(0f, 1f, _unplaceSpeed, (value) => UpdatePreviewObjectTransform(value, startPosition, transform.position, startRotation, transform.rotation, startScale, targetScale), animationCurve);

            while (_movementTweener.IsTweening)
            {
                yield return null;
            }

            _placeableItem.PlaceableItemMeshController.EnableMeshRenderers();
            _closestSocket = null;

            DestroyPreviewObjectIfActive();
        }

        private void UpdatePreviewObjectTransform(float progress, Vector3 startPosition, Vector3 targetPosition, Quaternion startRotation, Quaternion targetRotation, Vector3 startScale, Vector3 targetScale)
        {
            if (_previewObject == null)
                return;

            _previewObject.transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            _previewObject.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, progress);
            _previewObject.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
        }

        private void StopCurrentTweensIfActive()
        {
            _movementTweener?.Cancel();
            _rotationTweener?.Cancel();
            _scaleTweener?.Cancel();
        }

        private void StopTweenPreviewObjectToHandCoroutineIfActive()
        {
            if (_tweenPreviewObjectToHandCoroutine != null)
            {
                StopCoroutine(_tweenPreviewObjectToHandCoroutine);
                _tweenPreviewObjectToHandCoroutine = null;
            }
        }

        private void DestroyPreviewObjectIfActive()
        {
            if (_previewObject != null)
            {
                Destroy(_previewObject);
                _previewObject = null;
            }
        }

        private void StopHandleEnteredPlaceableZoneCoroutineIfActive()
        {
            if (_handleEnteredPlaceableZoneCoroutine != null)
            {
                StopCoroutine(_handleEnteredPlaceableZoneCoroutine);
                _handleEnteredPlaceableZoneCoroutine = null;
            }
        }

        private bool IsZeroQuaternion(Quaternion q)
        {
            return q.x == 0f && q.y == 0f && q.z == 0f && q.w == 0f;
        }

        #endregion Private Functions

        #region Editor Functions

#if UNITY_EDITOR


        public void SetPreviewObjectPrefab(GameObject prefab)
        {
            _previewObjectPrefab = prefab;
        }

        public void SetPreviewObjectSaveFilePath(string filePath)
        {
            _previewObjectSaveFilePath = filePath;
        }

#endif

        #endregion Editor Functions
    }
}
