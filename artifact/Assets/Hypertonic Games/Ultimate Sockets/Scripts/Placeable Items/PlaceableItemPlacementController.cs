using System.Collections;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.PlaceableItems
{
    /// <summary>
    /// Responsible for trying to place the object into a socket
    /// </summary>
    public class PlaceableItemPlacementController : MonoBehaviour
    {
        private PlaceableItem _placeableItem;

        private PlaceableItemPlacementCriteriaController _criteriaController => _placeableItem.PlaceableItemPlacementCriteriaController;

        private Coroutine _checkPlacementCriteriaCoroutine = null;

        private void Awake()
        {
            if (_placeableItem == null && !transform.parent.TryGetComponent(out _placeableItem))
            {
                Debug.LogError("The PlaceableItem component has not been set");
                return;
            }
        }

        private void OnEnable()
        {
            _placeableItem.OnEnteredPlaceableZone += HandleEnteredPlaceableZone;
            _placeableItem.OnExitedPlaceableZone += HandleExitedPlaceableZone;
        }

        private void OnDisable()
        {
            _placeableItem.OnEnteredPlaceableZone -= HandleEnteredPlaceableZone;
            _placeableItem.OnExitedPlaceableZone -= HandleExitedPlaceableZone;
        }

        private void HandleEnteredPlaceableZone(PlaceableItem placeableItem)
        {
            StopCheckCriteriaCoroutineIfActive();

            if (_criteriaController == null)
            {
                Debug.LogError("The criteria controller is null", this);
                return;
            }

            _checkPlacementCriteriaCoroutine = StartCoroutine(CheckPlacementCriteriaCoroutine());
        }

        private void HandleExitedPlaceableZone(PlaceableItem placeableItem)
        {
            StopCheckCriteriaCoroutineIfActive();
        }

        private IEnumerator CheckPlacementCriteriaCoroutine()
        {
            while (_placeableItem.WithinPlaceableZone)
            {
                yield return null;

                if (_criteriaController.CanPlace() && !_placeableItem.PreventPlacement && CanPlaceInSocket())
                {
                    _placeableItem.PlaceInSocket();
                    break;
                }
            }
        }

        private bool CanPlaceInSocket()
        {
            if (_placeableItem.ClosestSocket == null)
            {
                return false;
            }

            return _placeableItem.ClosestSocket.CanPlace(_placeableItem);
        }

        private void StopCheckCriteriaCoroutineIfActive()
        {
            if (_checkPlacementCriteriaCoroutine != null)
            {
                StopCoroutine(_checkPlacementCriteriaCoroutine);
                _checkPlacementCriteriaCoroutine = null;
            }
        }
    }
}
