using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using Hypertonic.Modules.UltimateSockets.Sockets;
using Hypertonic.Modules.UltimateSockets.XR;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// This is the intergration between the XR Interaction Toolkit and the Simple Sockets system.
/// </summary>
namespace Hypertonic.Modules.UltimateSockets
{
    public class XRITGrabbableItem : MonoBehaviour, IGrabbableItem
    {
        [SerializeField]
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grabbableInteractable;

        public event IGrabbableItemEvent OnGrabbed;
        public event IGrabbableItemEvent OnReleased;

        #region Unity Functions

        private void Awake()
        {
            if (_grabbableInteractable == null && !TryGetComponent(out _grabbableInteractable))
            {
                Debug.LogError("The XR Grab Interactable component could not be found on the game object", this);
                return;
            }
        }

        private void OnEnable()
        {
            _grabbableInteractable.selectEntered.AddListener(HandleGrabbed);
            _grabbableInteractable.selectExited.AddListener(HandleReleased);
        }

        private void OnDisable()
        {
            _grabbableInteractable.selectEntered.RemoveListener(HandleGrabbed);
            _grabbableInteractable.selectExited.RemoveListener(HandleReleased);
        }

        #endregion

        #region IGrabbableItem Functions

        public void Disable()
        {
            _grabbableInteractable.enabled = false;
        }

        public void Enable()
        {
            _grabbableInteractable.enabled = true;
        }

        public bool IsGrabbing()
        {
            return _grabbableInteractable.isSelected;
        }

        public void HandleRemovedFromSocket(Socket socket, PlaceableItem placeableItem) { }

        #endregion IGrabbableItem Functions

        #region Private Functions

        private void HandleGrabbed(SelectEnterEventArgs obj)
        {
            OnGrabbed?.Invoke();
        }

        private void HandleReleased(SelectExitEventArgs obj)
        {
            OnReleased?.Invoke();
        }

        #endregion Private Functions
    }
}
