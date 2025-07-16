using System.Collections;
using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using UnityEngine;
using UnityEngine.UI;

namespace Hypertonic.Modules.UltimateSockets.Examples
{
    public class ObjectCharger : MonoBehaviour
    {
        public bool FullyCharged => _charge >= 1f;

        [SerializeField]
        private float _chargeSpeed = 1f;

        [SerializeField]
        private Image _fillImage;

        private Material _chargeMaterial;

        [SerializeField]
        private Color _chargeColour;

        private float _charge = 0f;

        private PlaceableItem _placeableItem;
        private Coroutine _chargeCoroutine;

        private void Awake()
        {
            _chargeMaterial = GetComponent<MeshRenderer>().material;
            _fillImage.fillAmount = 0f;

            _placeableItem = GetComponentInChildren<PlaceableItem>();

            if (_placeableItem == null)
            {
                Debug.LogError("The placeable item component could not be found on the game object");
            }
        }

        private void OnEnable()
        {
            _placeableItem.PlaceableItemGrabbable.GrabbableItem.OnGrabbed += HandleGrabbed;
            _placeableItem.PlaceableItemGrabbable.GrabbableItem.OnReleased += HandleReleased;
        }

        private void OnDisable()
        {
            _placeableItem.PlaceableItemGrabbable.GrabbableItem.OnGrabbed -= HandleGrabbed;
            _placeableItem.PlaceableItemGrabbable.GrabbableItem.OnReleased -= HandleReleased;
        }

        private void HandleGrabbed()
        {
            StopChargeCoroutine();

            _chargeCoroutine = StartCoroutine(ChargeCoroutine());
        }

        private void HandleReleased()
        {
            StopChargeCoroutine();
        }

        private IEnumerator ChargeCoroutine()
        {
            while (_charge < 1f)
            {
                _charge += Time.deltaTime * _chargeSpeed;

                UpdateFillUI();

                yield return null;
            }

            _charge = Mathf.Clamp01(_charge);

            UpdateFillUI();
            UpdateMaterialColour();
        }

        private void StopChargeCoroutine()
        {
            if (_chargeCoroutine != null)
            {
                StopCoroutine(_chargeCoroutine);
                _chargeCoroutine = null;
            }
        }

        private void UpdateFillUI()
        {
            _fillImage.fillAmount = _charge;
        }

        private void UpdateMaterialColour()
        {
            _chargeMaterial.color = _chargeColour;
        }
    }
}
