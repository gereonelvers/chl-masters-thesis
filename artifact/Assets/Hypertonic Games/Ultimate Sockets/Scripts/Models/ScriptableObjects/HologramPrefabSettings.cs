using System.Collections.Generic;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Models.ScriptableObjects
{

    [CreateAssetMenu(fileName = "Hologram Prefab Settings", menuName = "Hypertonic/Ultimate Sockets/Hologram Prefab Settings", order = 3)]
    public class HologramPrefabSettings : ScriptableObject
    {
        public List<HologramPrefabConfig> HologramPrefabConfigs = new List<HologramPrefabConfig>();
    }
}
