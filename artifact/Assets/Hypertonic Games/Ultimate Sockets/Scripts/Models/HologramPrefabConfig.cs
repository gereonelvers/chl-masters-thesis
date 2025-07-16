using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Models
{
    [System.Serializable]
    public class HologramPrefabConfig
    {
        [HideInInspector]
        public string ItemTag;
        public GameObject Prefab;
    }
}
