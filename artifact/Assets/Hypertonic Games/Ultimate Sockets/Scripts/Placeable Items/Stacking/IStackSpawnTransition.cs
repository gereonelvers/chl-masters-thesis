
using Hypertonic.Modules.UltimateSockets.Sockets;

namespace Hypertonic.Modules.UltimateSockets.PlaceableItems.Stacking
{
    public interface IStackSpawnTransition
    {
        public void Spawn(Socket socket, PlaceableItem placeableItem);
        public bool IsSpawning();
    }
}
