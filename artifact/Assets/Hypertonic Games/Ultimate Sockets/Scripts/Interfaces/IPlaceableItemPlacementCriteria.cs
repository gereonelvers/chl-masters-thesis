using Hypertonic.Modules.UltimateSockets.PlaceableItems;

namespace Hypertonic.Modules.UltimateSockets.Interfaces
{
    public interface IPlaceableItemPlacementCriteria
    {
        public void Setup(PlaceableItem placeableItem);
        public bool CanPlace();
        public bool UseCriteria();
        public bool PreventHighlight();
    }
}
