using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Examples
{
    public class GrabCounter : MonoBehaviour
    {
        public int GrabCount = 0;

        [SerializeField]
        private TMPro.TMP_Text _counterText;

        private PlaceableItem _placeableItem;

        private void Awake()
        {
            _placeableItem = GetComponentInChildren<PlaceableItem>();

            if (_placeableItem == null)
            {
                Debug.LogError("The placeable item component could not be found on the game object");
            }
        }

        private void OnEnable()
        {
            _placeableItem.PlaceableItemGrabbable.GrabbableItem.OnGrabbed += HandleGrabbed;
        }

        private void OnDisable()
        {
            _placeableItem.PlaceableItemGrabbable.GrabbableItem.OnGrabbed -= HandleGrabbed;
        }

        private void HandleGrabbed()
        {
            GrabCount++;
            UpdateCounter();
        }

        private void UpdateCounter()
        {
            _counterText.text = GrabCount.ToString();
        }
    }
}
