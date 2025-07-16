using Hypertonic.Modules.UltimateSockets.PlaceableItems;

namespace Hypertonic.Modules.UltimateSockets.Highlighters
{
    public interface ISocketHighlighter
    {
        public void Setup(SocketHighlighter socketHighlighter);
        public void StartHighlight(PlaceableItem placeableItem);
        public void StopHighlight();
    }
}
