using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using Hypertonic.Modules.UltimateSockets.Sockets;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Interfaces
{
    /// <summary>
    /// Designed as an optional interface that allows components to apply specific functionality
    /// </summary>
    public interface IPreviewObjectSpawnHandler
    {
        public void HandlePreviewObjectSpawned(GameObject previewObject, PlaceableItem placeableItem, Socket socket);
    }
}
