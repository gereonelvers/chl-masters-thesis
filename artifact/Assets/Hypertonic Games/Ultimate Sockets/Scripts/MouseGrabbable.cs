using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using Hypertonic.Modules.UltimateSockets.Sockets;
using Hypertonic.Modules.UltimateSockets.XR;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hypertonic.Modules.UltimateSockets
{
    public class MouseGrabbable : MonoBehaviour, IGrabbableItem, IPointerClickHandler
    {
        public event IGrabbableItemEvent OnGrabbed;
        public event IGrabbableItemEvent OnReleased;

        private bool _isGrabbing = false;

        private bool _interactable = true;
        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (IsGrabbing() && _interactable)
            {
                FollowMouseZ();
            }
        }

        public void Disable()
        {
            _interactable = false;
        }

        public void Enable()
        {
            _interactable = true;
        }

        public void HandleRemovedFromSocket(Socket socket, PlaceableItem placeableItem) { }

        public bool IsGrabbing() => _isGrabbing;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && _interactable)
            {
                HandleGrabbed();
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                HandleReleased();
            }
        }

        private void HandleGrabbed()
        {
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;

            OnGrabbed?.Invoke();
            _isGrabbing = true;
        }

        private void HandleReleased()
        {
            _rigidbody.isKinematic = false;
            OnReleased?.Invoke();
            _isGrabbing = false;
        }

        private void FollowMouseZ()
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Camera.main.WorldToScreenPoint(transform.position).z;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

            transform.position = new Vector3(transform.position.x, worldPos.y, worldPos.z);
        }
    }
}
