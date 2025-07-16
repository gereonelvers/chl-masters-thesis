using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using Hypertonic.Modules.UltimateSockets.Sockets;

namespace Hypertonic.Modules.UltimateSockets.XR
{
    public delegate void IGrabbableItemEvent();

    public interface IGrabbableItem
    {
        public event IGrabbableItemEvent OnGrabbed;
        public event IGrabbableItemEvent OnReleased;

        public void Enable();
        public void Disable();
        public bool IsGrabbing();
        public void HandleRemovedFromSocket(Socket socket, PlaceableItem placeableItem);
    }

}
