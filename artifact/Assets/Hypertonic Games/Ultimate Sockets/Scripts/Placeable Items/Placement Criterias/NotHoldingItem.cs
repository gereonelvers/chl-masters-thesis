using Hypertonic.Modules.UltimateSockets.Interfaces;
using Hypertonic.Modules.UltimateSockets.XR;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.PlaceableItems.PlacementCriterias
{
    public class NotHoldingItem : MonoBehaviour, IPlaceableItemPlacementCriteria
    {
        [SerializeField]
        [HideInInspector]
        private PlaceableItemGrabbable _placeableItemGrabbable;

        [SerializeField]
        private bool _preventHighlight = false;

        [SerializeField]
        private bool _useCriteria = true;

        #region Unity Functions

        private void Awake()
        {
            if (_placeableItemGrabbable == null)
            {
                Debug.LogError("The reference to the _placeableItemGrabbable has not been set");
                return;
            }
        }

        #endregion Unity Functions


        #region Public Functions

        public bool CanPlace() => !_placeableItemGrabbable.GrabbableItem.IsGrabbing();

        public bool PreventHighlight() => _preventHighlight;

        public bool UseCriteria() => _useCriteria;

        public void Setup(PlaceableItem placeableItem)
        {
            if (placeableItem == null)
            {
                Debug.LogErrorFormat(this, "The placeable item is null");
                return;
            }

            if (!placeableItem.UtilityComponentContainer.TryGetComponent(out _placeableItemGrabbable))
            {
                Debug.LogErrorFormat(this, "The placeable item [{0}] does not have a PlaceableItemGrabbable component", placeableItem.name);
                return;
            }
        }

        #endregion Public Functions

        #region  Editor Functions

#if UNITY_EDITOR

        public void SetPlaceableItemGrabbable(PlaceableItemGrabbable placeableItemGrabbable)
        {
            _placeableItemGrabbable = placeableItemGrabbable;
        }
#endif

        #endregion Editor Functions
    }
}
