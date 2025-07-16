using System.Collections;
using Hypertonic.Modules.UltimateSockets.Sockets;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.PlaceableItems
{
    public class PlaceableItemCollider : MonoBehaviour
    {
        public PlaceableItem PlaceableItem => _placeableItem;

        public Collider Collider => ColliderManager.Collider;

        public bool Enabled { get; private set; } = true;

        public ColliderManager ColliderManager => _colliderManager;

        [SerializeField]
        private PlaceableItem _placeableItem;


        [SerializeField]
        private float _enableColliderDelaySeconds = 0.06f;


        [SerializeField]
        private ColliderManager _colliderManager;

        private Coroutine _enableColliderCoroutine;

        #region Unity Functions

        private void Awake()
        {
            if (_placeableItem == null)
            {
                Debug.LogError("The _placeableItem item has not beed assigned", this);
                return;
            }

            if (Collider == null)
            {
                Debug.LogError("The Collider has not been assigned", this);
                return;
            }
        }

        protected virtual void OnEnable()
        {
            _placeableItem.OnPlaced += HandlePlaced;
            _placeableItem.OnRemovedFromSocket += HandleRemovedFromSocket;
        }

        protected virtual void OnDisable()
        {
            _placeableItem.OnPlaced -= HandlePlaced;
            _placeableItem.OnRemovedFromSocket -= HandleRemovedFromSocket;
        }

        #endregion Unity Functions

        #region Public Functions

        public void Enable()
        {
            Enabled = true;
        }

        public void Disable()
        {
            Enabled = false;
        }

        public void EnableCollider()
        {
            Collider.enabled = true;
        }

        public void DisableCollider()
        {
            Collider.enabled = false;
        }

        #endregion Public Functions

        #region Private Functions

        protected virtual void HandleGrabbed(GameObject hand, GameObject gameObject)
        {
            HandleRemovedFromSocket();
        }

        protected virtual void HandleReleased(GameObject hand, GameObject gameObject)
        {
            StopEnableCoroutineIfActive();

            DisableCollider();
        }

        protected virtual void HandlePlaced(Socket socket, PlaceableItem placeableItem)
        {
            StopEnableCoroutineIfActive();

            DisableCollider();
        }

        protected virtual void HandleRemovedFromSocket(Socket socket, PlaceableItem placeableItem)
        {
            HandleRemovedFromSocket();
        }

        protected virtual void HandleRemovedFromSocket()
        {
            if (!Enabled) return;

            StopEnableCoroutineIfActive();

            _enableColliderCoroutine = StartCoroutine(EnablePlaceableColliderCoroutine());
        }

        protected virtual IEnumerator EnablePlaceableColliderCoroutine()
        {
            yield return new WaitForSeconds(_enableColliderDelaySeconds);

            EnableCollider();
        }

        protected virtual void StopEnableCoroutineIfActive()
        {
            if (_enableColliderCoroutine != null)
            {
                StopCoroutine(_enableColliderCoroutine);
                _enableColliderCoroutine = null;
            }
        }

        #endregion Private Functions

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
                _colliderManager.AddMeshCollider();
                _colliderManager.IsTrigger = true;
            }
        }

#endif

        #endregion Editor Functions
    }
}
