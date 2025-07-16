using Hypertonic.Modules.UltimateSockets.Models;
using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using Hypertonic.Modules.UltimateSockets.Sockets;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets
{
    public class PlaceableItemRigidbody : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody _rigidbody;

        [SerializeField]
        private PlaceableItem _placeableItem;

        [SerializeField]
        private PlaceableItemRigidbodySettings _settings;

        #region Unity Functions

        private void Start()
        {
            if (_rigidbody == null)
            {
                Debug.LogError("The rigidbody reference is null");
                return;
            }

            if (_placeableItem == null)
            {
                Debug.LogError("The _placeableItem reference is null");
                return;
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

        #endregion Unity Functions

        #region Public Functions

        public void SetKinematic(bool state)
        {
            _rigidbody.isKinematic = state;
        }

        #endregion Public Functions

        #region Private Functions

        private void HandlePlaced(Socket socket, PlaceableItem placeableItem)
        {
            if (_settings.KinematicOnSocket)
            {
                _rigidbody.isKinematic = true;
            }
        }

        private void HandleRemovedFromSocket(Socket socket, PlaceableItem placeableItem)
        {
            if (_settings.NonKinematicOnUnsocket)
            {
                _rigidbody.isKinematic = false;
            }
        }

        #endregion Private Functions

        #region Editor Functions

#if UNITY_EDITOR

        public void SetPlaceableItem(PlaceableItem placeableItem)
        {
            _placeableItem = placeableItem;
        }

        public void SetRigidBody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

#endif

        #endregion Editor Functions
    }
}
