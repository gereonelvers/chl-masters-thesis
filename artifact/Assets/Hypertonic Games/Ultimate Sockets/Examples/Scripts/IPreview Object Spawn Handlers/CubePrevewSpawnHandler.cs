using Hypertonic.Modules.UltimateSockets.Interfaces;
using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using Hypertonic.Modules.UltimateSockets.Sockets;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Examples.IPreviewSpawnHandlers
{
    /// <summary>
    /// This is an example class of how you can hook into the preview object spawn event and preform bespoke behaviour based based on the socket and item.
    /// E.G Play a sound, update a texture based on game state, etc.
    /// This is helpful for passing the context of the placeable item and the socket to the preview object which otherwise wouldn't have that data available.
    /// </summary>
    public class CubePrevewSpawnHandler : MonoBehaviour, IPreviewObjectSpawnHandler
    {
        public void HandlePreviewObjectSpawned(GameObject previewObject, PlaceableItem placeableItem, Socket socket)
        {
            ColourController colourController = placeableItem.transform.parent.GetComponent<ColourController>();

            if (colourController == null)
            {
                Debug.LogError("This preview object does not have a colour controller attached");
                return;
            }


            Color color = Color.magenta;

            switch (colourController.PreviewItemTargetColourName)
            {
                case PreviewColours.RED: color = Color.red; break;
                case PreviewColours.BLUE: color = Color.blue; break;
                case PreviewColours.GREEN: color = Color.green; break;
            }

            previewObject.GetComponent<MeshRenderer>().material.color = color;
        }
    }
}
