using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.PlaceableItems
{
    public class PlaceableItemMeshController : MonoBehaviour
    {
        public bool HasActiveRenderers => _activeRenderers.Length > 0;

        [SerializeField]
        private PlaceableItem _placeableItem;

        private Renderer[] _activeRenderers = new Renderer[0];

        #region Unity Functions

        private void Awake()
        {
            if (_placeableItem == null)
            {
                Debug.LogError("The _placeableItem reference has not beed assigned");
                return;
            }
        }

        #endregion Unity Functions

        #region  Public Functions

        public void EnableMeshRenderers()
        {
            if (_activeRenderers == null || _activeRenderers.Length == 0)
            {
                Debug.LogWarningFormat(this, "The _activeRenderers have not been assigned yet. But EnableMeshRenderers() was called on GameObject: [{0}].", _placeableItem.RootTransform.name);
                return;
            }

            SetRenderersState(true, _activeRenderers);
        }

        public void DisableMeshRenderers()
        {
            _activeRenderers = GetActiveRenderers();

            SetRenderersState(false, _activeRenderers);
        }

        #endregion Public Functions

        #region Private Functions

        private Renderer[] GetActiveRenderers()
        {
            List<Renderer> renderers = new List<Renderer>();

            renderers.AddRange(_placeableItem.RootTransform.GetComponents<Renderer>());
            renderers.AddRange(_placeableItem.RootTransform.GetComponentsInChildren<Renderer>(false).ToList());

            return renderers.Where(r => r.enabled).ToArray();
        }

        private void SetRenderersState(bool state, Renderer[] renderers)
        {
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = state;
            }
        }

        #endregion Private Functions

        #region Editor Functions

#if UNITY_EDITOR

        public void SetPlaceableItem(PlaceableItem placeableItem)
        {
            _placeableItem = placeableItem;
        }

#endif

        #endregion Editor Functions
    }
}
