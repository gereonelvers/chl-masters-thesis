using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Models
{
    [System.Serializable]
    public class ItemPlacementConfig
    {
        [HideInInspector]
        public string Name;
        public Vector3 PlacedPosition;
        public Quaternion PlacedRotation;
        public Vector3 PlacedScale = Vector3.one;

        public ItemPlacementConfig()
        {
            PlacedRotation = Quaternion.identity;
        }
    }
}
