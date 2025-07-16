using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using Hypertonic.Modules.UltimateSockets.Sockets;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.XR
{
    [DefaultExecutionOrder(-100)]
    public class PlaceableItemGrabbable : MonoBehaviour
    {
        public const string NOT_HOLDING_ITEM_PLACEMENT_CRITERIA_NAME = "Placement Criteria | Not Holding Item";

        public IGrabbableItem GrabbableItem => _grabbableItem;

        public GameObject GrabbableItemGameObject => _grabbableItemGameObject;

        [SerializeField]
        private GameObject _grabbableItemGameObject;

        private IGrabbableItem _grabbableItem;

        [SerializeField]
        private PlaceableItem _placeableItem;


        #region Unity Functions

        private void Awake()
        {
            if (!enabled) return;

            CheckReferences();
        }

        private void OnEnable()
        {
            _grabbableItem.OnGrabbed += HandleGrabbed;

            _placeableItem.OnPlaced += HandlePlaced;
            _placeableItem.OnRemovedFromSocket += HandleRemovedFromSocket;
        }

        private void OnDisable()
        {
            _grabbableItem.OnGrabbed -= HandleGrabbed;

            _placeableItem.OnPlaced -= HandlePlaced;
            _placeableItem.OnRemovedFromSocket -= HandleRemovedFromSocket;
        }

        #endregion Unity Functions

        #region  Public Functions



        #endregion Public Functions

        public void RemoveFromSocket()
        {
            if (!_placeableItem.Placed)
                return;

            if (_placeableItem.ClosestSocket == null)
            {
                Debug.Log("Cannot remove item from socket as it is not placed");
                return;
            }

            _placeableItem.RemoveFromSocket();
        }

        public void EnableGrabbable()
        {
            _grabbableItem.Enable();
        }

        public void DisableGrabbale()
        {
            _grabbableItem.Disable();
        }


        #region Private Functions

        private void HandleGrabbed()
        {
            RemoveFromSocket();
        }

        private void HandlePlaced(Socket socket, PlaceableItem placeableItem)
        {
            if (socket.PreventItemRemoval)
            {
                DisableGrabbale();
            }
        }

        private void HandleRemovedFromSocket(Socket socket, PlaceableItem placeableItem)
        {
            _grabbableItem.HandleRemovedFromSocket(socket, placeableItem);
        }

        private void CheckReferences()
        {
            if (_placeableItem == null)
            {
                Debug.LogError("The _placeableItem property has not been assigned", this);
                return;
            }

            if (_grabbableItemGameObject == null)
            {
                Debug.LogError("The _grabbableItemGameObject property has not been assigned", this);
                return;
            }

            if (!_grabbableItemGameObject.TryGetComponent(out _grabbableItem))
            {
                Debug.LogError("The _grabbableItemGameObject doesn't contain a component that implements the IGrabbableItem interface", this);
                return;
            }
        }

        #endregion Private Functions




        #region Editor Functions

#if UNITY_EDITOR

        public void ClearGrabbableGameObject()
        {
            _grabbableItemGameObject = null;
        }

        public void SetPlaceableItem(PlaceableItem placeableItem)
        {
            _placeableItem = placeableItem;
        }

        public void SetGrabbableItem(GameObject grabbableItemGameObject)
        {
            IGrabbableItem igrabbaleItem = grabbableItemGameObject.GetComponent<IGrabbableItem>();

            if (igrabbaleItem == null)
            {
                Debug.LogErrorFormat(this, "The grabbable item game object {0} doesn't contain a component that implements the IGrabbableItem interface", grabbableItemGameObject);
            }

            _grabbableItemGameObject = grabbableItemGameObject;
        }

#endif

        #endregion Editor Functions
    }
}
