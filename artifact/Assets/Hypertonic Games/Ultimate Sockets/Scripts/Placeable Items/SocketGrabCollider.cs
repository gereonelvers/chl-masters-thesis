using Hypertonic.Modules.UltimateSockets.Sockets;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.PlaceableItems
{
    public class SocketGrabCollider : MonoBehaviour
    {
        public ColliderManager ColliderManager => _colliderManager;

        public Collider Collider => ColliderManager.Collider;

        [SerializeField]
        private PlaceableItem _placeableItem;

        [SerializeField]
        private ColliderManager _colliderManager;


        #region Unity Functions

        private void Awake()
        {
            if (_placeableItem == null)
            {
                Debug.LogError("The reference to _placeableItem has not been set", this);
                return;
            }

            DisableCollider();
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

        #endregion Unity Functions

        #region Public Functions

        public void EnableCollider()
        {
            ColliderManager.Collider.enabled = true;
        }

        public void DisableCollider()
        {
            ColliderManager.Collider.enabled = false;
        }

        #endregion Public Functions

        #region Private Functions

        private void HandlePlaced(Socket socket, PlaceableItem placeableItem)
        {
            EnableCollider();
        }

        private void HandleRemovedFromSocket(Socket socket, PlaceableItem placeableItem)
        {
            DisableCollider();
        }

        #endregion Private Functionss

        #region Editor Functions

#if UNITY_EDITOR

        public void SetPlaceableItem(PlaceableItem placeableItem)
        {
            _placeableItem = placeableItem;
        }

        public void AddColliderManager()
        {
            if (!TryGetComponent(out ColliderManager _))
            {
                _colliderManager = gameObject.AddComponent<ColliderManager>();
                _colliderManager.RootTransform = _placeableItem.RootTransform;
                _colliderManager.AddSphereCollider();
                _colliderManager.IsTrigger = false;

                _colliderManager.SetColliderRadius(_colliderManager.GetRadius() / 2f);
            }
        }

#endif

        #endregion Editor Functions
    }

}
