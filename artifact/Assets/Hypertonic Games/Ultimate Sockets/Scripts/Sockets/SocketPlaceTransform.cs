using Hypertonic.Modules.UltimateSockets.Models;
using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using Hypertonic.Modules.UltimateSockets.Tweening;
using System.Linq;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Sockets
{
    public class SocketPlaceTransform : MonoBehaviour
    {
        [SerializeField]
        private Socket _socket;

        [SerializeField]
        private bool _tweenToSocket;

        [SerializeField]
        private AnimationCurve _tweenToSocketCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [SerializeField]
        private float _tweenToSocketDurationSeconds = 0.5f;

        [SerializeField]
        private bool _keepDefaultObjectScale = false;

        private Tweener _tweener;

        #region Unity Functions

        private void Awake()
        {
            if (_socket == null)
            {
                if (!transform.parent.TryGetComponent(out _socket))
                {
                    Debug.LogError("Cannot obtain a reference to the socket component", this);
                    return;
                }
            }
        }

        private void OnEnable()
        {
            _socket.OnItemRemoved += HandleRemovedFromSocket;
        }

        private void OnDisable()
        {
            _socket.OnItemRemoved -= HandleRemovedFromSocket;
        }

        #endregion Unity Functions

        public void PlaceItem(PlaceableItem placeableItem)
        {
            TransformData transformData = GetPlacementPositionForItem(placeableItem.ItemTag);

            placeableItem.RootTransform.SetParent(transform, true);

            if (_tweenToSocket)
            {
                TweenToSocket(placeableItem, transformData);
            }
            else
            {
                placeableItem.RootTransform.localPosition = transformData.Position;
                placeableItem.RootTransform.localRotation = transformData.Rotation;


                if (HasPlacementPositionForItem(placeableItem.ItemTag))
                {
                    placeableItem.RootTransform.localScale = transformData.Scale;
                    return;
                }

                if (KeepDefaultScale())
                {
                    return;
                }

                if (placeableItem.KeepScaleForDefaultPlacements)
                {
                    return;
                }

                placeableItem.RootTransform.localScale = transformData.Scale;
            }
        }

        public TransformData GetPlacementPositionForItem(string itemTag)
        {
            // Should get config for item selected OR have default settings if any allowed
            ItemPlacementConfig config = _socket.SocketPlacementProfile.ItemPlacementConfigs.Where(x => x.Name.Equals(itemTag)).FirstOrDefault();

            if (config == null)
            {
                config = _socket.DefaultItemPlacementConfig;
            }

            return new TransformData
            {
                Position = config.PlacedPosition,
                Rotation = config.PlacedRotation,
                Scale = config.PlacedScale
            };
        }

        public bool HasPlacementPositionForItem(string itemTag)
        {
            return _socket.SocketPlacementProfile.ItemPlacementConfigs.Find(x => x.Name.Equals(itemTag)) != null;
        }

        public bool KeepDefaultScale()
        {
            return _keepDefaultObjectScale;
        }

        private void TweenToSocket(PlaceableItem placeableItem, TransformData transformData)
        {
            Vector3 startPosition = placeableItem.RootTransform.localPosition;
            Quaternion startRotation = placeableItem.RootTransform.localRotation;
            Vector3 startScale = placeableItem.RootTransform.localScale;

            Vector3 targetScale = HasPlacementPositionForItem(placeableItem.ItemTag) ? transformData.Scale : placeableItem.RootTransform.localScale;

            CancelTween();
            _tweener = Tweener.TweenFloat(0, 1, _tweenToSocketDurationSeconds, (float p) => UpdateTweenPosition(p, placeableItem, startPosition, startRotation, startScale, transformData, targetScale), _tweenToSocketCurve);
        }

        private void UpdateTweenPosition(float progress, PlaceableItem placeableItem, Vector3 startPosition, Quaternion startRotation, Vector3 startScale, TransformData targetTransformData, Vector3 targetScale)
        {
            if (_socket.PlacedItem != placeableItem)
                return;

            float curveValue = _tweenToSocketCurve.Evaluate(progress);

            placeableItem.RootTransform.localPosition = Vector3.LerpUnclamped(startPosition, targetTransformData.Position, curveValue);

            // Converting back to a quaternion here because using targetTransformData.Rotation directly can cause issues in the LerpUnclamped function.
            Quaternion targetRotation = Quaternion.Euler(targetTransformData.Rotation.eulerAngles);

            placeableItem.RootTransform.localRotation = Quaternion.LerpUnclamped(startRotation, targetRotation, curveValue);

            if (startScale != targetScale)
            {
                placeableItem.RootTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, curveValue);
            }
        }

        private void HandleRemovedFromSocket(Socket socket, PlaceableItem placeableItem)
        {
            CancelTween();
        }

        private void CancelTween()
        {
            _tweener?.Cancel();
            _tweener = null;
        }


        #region Editor
#if UNITY_EDITOR

        public void SetSocket(Socket socket)
        {
            _socket = socket;
        }
#endif
        #endregion Editor
    }
}
