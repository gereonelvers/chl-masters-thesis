using System.Collections;
using Hypertonic.Modules.UltimateSockets.Interfaces;
using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Sockets.PlacementCriterias
{
    public class ResocketCooldownPassed : MonoBehaviour, IPlaceableItemPlacementCriteria
    {
        [SerializeField]
        private float _coolDownDurationSeconds = 0.5f;

        [SerializeField]
        private bool _useCriteria = true;

        [SerializeField]
        [HideInInspector]
        private PlaceableItem _placeableItem;

        private bool _canResocket = true;

        private Coroutine _resocketCooldownCoroutine;


        #region Unity Functions

        private void Awake()
        {
            if (_placeableItem == null)
            {
                Debug.LogErrorFormat(this, "The PlaceableItem has not been assigned for the ResocketCooldownPassedCriteria component");
                return;
            }
        }
        private void OnEnable()
        {
            _placeableItem.OnRemovedFromSocket += HandleRemovedFromSocket;
            _placeableItem.OnPlaced += HandlePlaced;
        }

        private void OnDisable()
        {
            _placeableItem.OnRemovedFromSocket -= HandleRemovedFromSocket;
            _placeableItem.OnPlaced -= HandlePlaced;
        }

        #endregion Unity Functions

        #region  Public Functions
        public bool CanPlace()
        {
            return _canResocket;
        }

        public void Setup(PlaceableItem placeableItem)
        {
            _placeableItem = placeableItem;
        }

        public bool PreventHighlight() => true;

        public bool UseCriteria() => _useCriteria;


        #endregion  Public Functions

        #region  Private Functions
        private void HandlePlaced(Socket socket, PlaceableItem placeableItem)
        {
            StopCoroutineIfActive();

            _canResocket = false;
        }

        private void HandleRemovedFromSocket(Socket socket, PlaceableItem placeableItem)
        {
            StopCoroutineIfActive();

            _resocketCooldownCoroutine = StartCoroutine(RemovedFromSocketCooldownCoroutine());
        }

        private IEnumerator RemovedFromSocketCooldownCoroutine()
        {
            yield return new WaitForSeconds(_coolDownDurationSeconds);

            _canResocket = true;
        }

        private void StopCoroutineIfActive()
        {
            if (_resocketCooldownCoroutine != null)
            {
                StopCoroutine(_resocketCooldownCoroutine);
                _resocketCooldownCoroutine = null;
            }
        }

        #endregion  Private Functions

    }
}
