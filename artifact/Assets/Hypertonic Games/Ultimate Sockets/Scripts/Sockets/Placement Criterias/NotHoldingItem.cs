using Hypertonic.Modules.UltimateSockets.Interfaces;
using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using Hypertonic.Modules.UltimateSockets.XR;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Sockets.PlacementCriterias
{
    public class NotHoldingItem : MonoBehaviour, ISocketPlacementCriteria
    {
        [SerializeField]
        private bool _shouldUseCriteria = true;

        [SerializeField]
        private bool _preventHighlight = false;


        public bool CanPlace(PlaceableItem placeableItem)
        {
            if (!placeableItem.UtilityComponentContainer.TryGetComponent(out PlaceableItemGrabbable placeableItemGrabbable))
            {
                Debug.LogErrorFormat(this, "Could not get a reference to the PlaceableItemGrabbable component on the placeable item: [{0}]", placeableItem.name);

                return false;
            }

            return !placeableItemGrabbable.GrabbableItem.IsGrabbing();
        }

        public bool PreventHighlight() => _preventHighlight;

        public void Setup(Socket socket) { }

        public bool UseCriteria() => _shouldUseCriteria;
    }
}
