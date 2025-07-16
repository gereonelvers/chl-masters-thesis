using System.Collections.Generic;
using Hypertonic.Modules.UltimateSockets.Models;
using UnityEngine;

namespace Hypertonic.Modules.XR.PlacePoints.UltimateSockets.Models.ScriptableObjects
{
    [CreateAssetMenu(fileName = "Ultimate Socket Settings", menuName = "Hypertonic/Ultimate Sockets/Ultimate Socket Settings")]
    public class UltimateSocketSettings : ScriptableObject
    {
        public PlaceableItemTags PlaceableItemTags;
        public List<PlaceableItemPosingData> PlaceableItemPosingDatas = new List<PlaceableItemPosingData>();
    }
}
