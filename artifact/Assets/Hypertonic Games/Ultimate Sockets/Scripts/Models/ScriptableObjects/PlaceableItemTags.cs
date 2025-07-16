using System.Collections.Generic;
using UnityEngine;

namespace Hypertonic.Modules.XR.PlacePoints.UltimateSockets.Models.ScriptableObjects
{
    [CreateAssetMenu(fileName = "Placeable Item Tags", menuName = "Hypertonic/Ultimate Sockets/Placeable Item Tags")]
    public class PlaceableItemTags : ScriptableObject
    {
        public List<string> Tags = new List<string>();
    }
}

