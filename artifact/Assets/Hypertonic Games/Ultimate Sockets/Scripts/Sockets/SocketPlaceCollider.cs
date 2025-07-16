using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Sockets
{
    public class SocketPlaceCollider : MonoBehaviour
    {
        public delegate void SocketPlaceColliderEvent(PlaceableItem placeableItem);
        public event SocketPlaceColliderEvent OnItemWithinPlaceableArea;
        public event SocketPlaceColliderEvent OnItemLeftPlaceableArea;


        public ColliderManager ColliderManager => _colliderManager;
        public Collider DetectionCollider => ColliderManager.Collider;

        public bool IsEnabledAndActiveInHierarchy => IsEnabled && DetectionCollider.gameObject.activeInHierarchy;
        public bool IsEnabled => DetectionCollider.enabled;

        [SerializeField]
        private ColliderManager _colliderManager;

        [SerializeField]
        private Socket _socket;

        #region Unity Functions

        private void OnEnable()
        {
            ColliderManager.OnTriggerEnterd += HandleTriggerEntered;
            ColliderManager.OnTriggerStaying += HandleTriggerStaying;
            ColliderManager.OnTriggerExited += HandleTriggerExited;
        }

        private void OnDisable()
        {
            ColliderManager.OnTriggerEnterd -= HandleTriggerEntered;
            ColliderManager.OnTriggerStaying -= HandleTriggerStaying;
            ColliderManager.OnTriggerExited -= HandleTriggerExited;
        }

        #endregion Unity Functions 

        #region Public Functions

        public void Enable()
        {
            DetectionCollider.enabled = true;
        }

        public void Disable()
        {
            if (DetectionCollider != null)
            {
                DetectionCollider.enabled = false;
            }
        }

        #endregion Public Functions

        #region Private Functions

        private void HandleTriggerEntered(Collider other)
        {
            HandleItemInTriggerZone(other);
        }

        private void HandleTriggerStaying(Collider other)
        {
            HandleItemInTriggerZone(other);
        }

        private void HandleItemInTriggerZone(Collider other)
        {
            if (!other.TryGetComponent(out PlaceableItemCollider placeableItemCollider))
                return;

            if (placeableItemCollider.PlaceableItem == null)
            {
                Debug.LogError("Could not access the placeable item on the placeable item collider component", this);
                return;
            }

            OnItemWithinPlaceableArea?.Invoke(placeableItemCollider.PlaceableItem);
        }

        private void HandleTriggerExited(Collider other)
        {
            if (!other.TryGetComponent(out PlaceableItemCollider placeableItemCollider))
                return;

            if (placeableItemCollider.PlaceableItem == null)
            {
                Debug.LogError("Could not access the placeable item on the placeable item collider component", this);
                return;
            }

            OnItemLeftPlaceableArea?.Invoke(placeableItemCollider.PlaceableItem);
        }

        #endregion Private Functions

        #region Editor Functions

        public void AddColliderManager()
        {
            if (!TryGetComponent(out ColliderManager _))
            {
                _colliderManager = gameObject.AddComponent<ColliderManager>();
                _colliderManager.RootTransform = _socket.transform.parent == null ? transform : _socket.transform.parent;
                _colliderManager.AddSphereCollider();
                _colliderManager.IsTrigger = true;
            }
        }

        public void SetSocket(Socket socket)
        {
            _socket = socket;
        }

        #endregion Editor Functions
    }
}
