using Hypertonic.Modules.UltimateSockets.Interfaces;
using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using Hypertonic.Modules.UltimateSockets.Sockets;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Examples
{
    public class FullyCharged : MonoBehaviour, ISocketPlacementCriteria
    {
        public bool CanPlace(PlaceableItem placeableItem)
        {
            ObjectCharger objectCharger = placeableItem.RootTransform.GetComponent<ObjectCharger>();

            if (objectCharger == null)
            {
                Debug.LogWarning("No object charger found on " + placeableItem.RootTransform.name + ".");
                return false;
            }

            return objectCharger.FullyCharged;
        }

        public bool PreventHighlight() => true;

        public void Setup(Socket socket) { }

        public bool UseCriteria() => true;
    }
}
