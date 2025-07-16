using Hypertonic.Modules.UltimateSockets.Interfaces;
using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Examples
{
    public class GrabCountMoreThan : MonoBehaviour, IPlaceableItemPlacementCriteria
    {
        [SerializeField]
        private int _minGrabCount = 5;

        [SerializeField]
        private PlaceableItem _placeableItem;


        [SerializeField]
        private bool _useCriteria = true;

        public bool CanPlace()
        {
            GrabCounter grabCounter = _placeableItem.RootTransform.GetComponent<GrabCounter>();

            if (grabCounter == null)
            {
                Debug.LogWarning("No grab counter found on " + _placeableItem.RootTransform.name + ".");
                return false;
            }

            return grabCounter.GrabCount >= _minGrabCount;
        }

        public bool PreventHighlight() => true;
        public bool UseCriteria() => _useCriteria;

        public void Setup(PlaceableItem placeableItem)
        {
            _placeableItem = placeableItem;
        }
    }
}
