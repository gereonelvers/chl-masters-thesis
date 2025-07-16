using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Sockets
{
    public class SocketHighlightAreaCollider : MonoBehaviour
    {
        public delegate void SocketHighlightAreaColliderEvent(PlaceableItem placeableItem);
        public event SocketHighlightAreaColliderEvent OnPlaceableItemNear;
        public event SocketHighlightAreaColliderEvent OnPlaceableItemStay;
        public event SocketHighlightAreaColliderEvent OnPlaceableItemLeftArea;

        public ColliderManager ColliderManager => _colliderManager;
        public Collider DetectionCollider => ColliderManager.Collider;

        [SerializeField]
        private ColliderManager _colliderManager;

        [SerializeField]
        private Socket _socket;

#pragma warning disable CS0414

        // Used by editor script
        [SerializeField]
        private bool _matchPlacementDetectionCollider = true;

#pragma warning restore CS0414

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
            ColliderManager.SetColliderActive(true);
        }

        public void Disable()
        {
            ColliderManager.SetColliderActive(false);
        }

        #endregion Public Functions

        #region Private Functions

        private void HandleTriggerEntered(Collider other)
        {
            if (!other.TryGetComponent(out PlaceableItemCollider placeableItemCollider))
                return;

            if (placeableItemCollider.PlaceableItem == null)
            {
                Debug.LogError("Could not access the placeable item on the placeable item collider component", this);
                return;
            }

            OnPlaceableItemNear?.Invoke(placeableItemCollider.PlaceableItem);
        }

        private void HandleTriggerStaying(Collider other)
        {
            if (!other.TryGetComponent(out PlaceableItemCollider placeableItemCollider))
                return;

            if (placeableItemCollider.PlaceableItem == null)
            {
                Debug.LogError("Could not access the placeable item on the placeable item collider component", this);
                return;
            }

            OnPlaceableItemStay?.Invoke(placeableItemCollider.PlaceableItem);
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

            OnPlaceableItemLeftArea?.Invoke(placeableItemCollider.PlaceableItem);
        }


        #endregion Private Functions

        #region Editor Functions

        public void AddColliderManager()
        {
            if (!TryGetComponent(out ColliderManager _))
            {
                _colliderManager = gameObject.AddComponent<ColliderManager>();
                _colliderManager.RootTransform = _socket.transform.parent == null ? transform : _socket.transform.parent;
                _colliderManager.AddMeshCollider();
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
