using Hypertonic.Modules.UltimateSockets.Enums;

namespace Hypertonic.Modules.UltimateSockets.Models
{
    [System.Serializable]
    public class SocketStackableItemControllerData
    {
        public bool Stackable = false;
        public int MaxStackSize = 10;
        public StackType StackType;
        public InstanceStackType InstanceStackType;
        public float StackReplacementDelay = 1f;
        public bool InfiniteReplacement = false;
    }
}
