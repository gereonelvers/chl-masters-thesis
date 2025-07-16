using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using Hypertonic.Modules.UltimateSockets.Sockets;

namespace Hypertonic.Modules.UltimateSockets.Interfaces
{
    public interface ISocketPlacementCriteria
    {
        public void Setup(Socket socket);
        public bool CanPlace(PlaceableItem placeableItem);
        public bool UseCriteria();
        public bool PreventHighlight();
    }
}
