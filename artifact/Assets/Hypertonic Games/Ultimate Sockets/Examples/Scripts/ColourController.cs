using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Examples
{
    public class ColourController : MonoBehaviour
    {
        public PreviewColours PreviewItemTargetColourName => _previewItemTargetColourName;

        [SerializeField]
        private PreviewColours _previewItemTargetColourName;
    }


    public enum PreviewColours
    {
        RED,
        BLUE,
        GREEN,
    }
}
