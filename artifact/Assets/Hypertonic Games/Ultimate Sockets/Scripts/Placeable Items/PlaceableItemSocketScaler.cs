using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using Hypertonic.Modules.UltimateSockets.Sockets;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets
{
    public class PlaceableItemSocketScaler : MonoBehaviour
    {
        public delegate void PlaceableItemSocketScalerEvent();
        public event PlaceableItemSocketScalerEvent OnReturnedToOriginalScale;

        public Vector3? OriginalLocalScale { get; private set; }

        private PlaceableItem _placeableItem;

        private void Awake()
        {
            if (!transform.parent.TryGetComponent(out _placeableItem))
            {
                Debug.LogError("Cannot get reference to PlaceableItem", this);
            }
        }

        private void OnEnable()
        {
            _placeableItem.OnPlaced += HandlePlaced;
            _placeableItem.OnRemovedFromSocket += HandleRemovedFromSocket;
        }

        private void OnDisable()
        {
            _placeableItem.OnPlaced -= HandlePlaced;
            _placeableItem.OnRemovedFromSocket -= HandleRemovedFromSocket;
        }

        private void HandleRemovedFromSocket(Socket socket, PlaceableItem placeableItem)
        {
            ReturnToOriginalScale(placeableItem);
            OriginalLocalScale = null;
        }

        private void HandlePlaced(Socket socket, PlaceableItem placeableItem)
        {
            OriginalLocalScale = placeableItem.RootTransform.localScale;
        }

        private void ReturnToOriginalScale(PlaceableItem placeableItem)
        {
            placeableItem.RootTransform.localScale = (Vector3)OriginalLocalScale;
            OnReturnedToOriginalScale?.Invoke();
        }
    }
}
