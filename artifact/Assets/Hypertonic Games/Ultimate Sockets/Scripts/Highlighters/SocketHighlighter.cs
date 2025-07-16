using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Hypertonic.Modules.UltimateSockets.Models;
using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using Hypertonic.Modules.UltimateSockets.Sockets;
using System;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Hypertonic.Modules.UltimateSockets.Highlighters
{
    public class SocketHighlighter : MonoBehaviour
    {
        public Socket Socket => _socket;
        public bool UseHighlighter => _useHighlighters;
        public List<HighlighterEntry> SocketHighlighters => _socketHighlighters;

        [SerializeField]
        private Socket _socket;

        private bool _isHighlighting = false;

        [SerializeField]
        private bool _useHighlighters = true;

        [SerializeField]
        private List<HighlighterEntry> _socketHighlighters = new List<HighlighterEntry>();

        [SerializeField]
        private SocketHighlightAreaCollider _socketHighlightAreaCollider;

        #region Unity Functions

        private void OnEnable()
        {
            _socket.OnItemPlaced += HandleItemPlaced;

            _socketHighlightAreaCollider.OnPlaceableItemNear += HandlePlaceableItemNear;
            _socketHighlightAreaCollider.OnPlaceableItemStay += HandleItemWithinHighlightArea;
            _socketHighlightAreaCollider.OnPlaceableItemLeftArea += HandlePlaceableItemFar;
        }

        private void OnDisable()
        {
            _socket.OnItemPlaced -= HandleItemPlaced;

            _socketHighlightAreaCollider.OnPlaceableItemNear -= HandlePlaceableItemNear;
            _socketHighlightAreaCollider.OnPlaceableItemStay -= HandleItemWithinHighlightArea;
            _socketHighlightAreaCollider.OnPlaceableItemLeftArea -= HandlePlaceableItemFar;
        }

        #endregion Unity Functions

        #region Private Functions

        private void HandleItemPlaced(Socket socket, PlaceableItem placeableItem)
        {
            StopHighlightingSocket(placeableItem);
        }

        private void HandlePlaceableItemNear(PlaceableItem placeableItem)
        {
            if (placeableItem.CurrentSocketHighlighter == this)
            {
                return;
            }

            if (placeableItem.ClosestSocket != null && placeableItem.ClosestSocket != _socket)
            {
                return;
            }

            if (!CanHighlight(placeableItem))
            {
                return;
            }

            HighlightSocket(placeableItem);
        }

        private void HandleItemWithinHighlightArea(PlaceableItem placeableItem)
        {
            if (_isHighlighting)
            {
                return;
            }

            if (placeableItem.CurrentSocketHighlighter != null && placeableItem.CurrentSocketHighlighter != this)
            {
                return;
            }

            if (!CanHighlight(placeableItem))
            {
                return;
            }

            HighlightSocket(placeableItem);
        }

        private void HandlePlaceableItemFar(PlaceableItem placeableItem)
        {
            StopHighlightingSocket(placeableItem);
        }

        #endregion Private Functions

        #region Public Functions

        public bool CanHighlight(PlaceableItem placeableItem)
        {
            if (!_socket.StackableItemController.Settings.Stackable && _socket.IsHoldingItem)
                return false;

            if (!placeableItem.StackableItemController.Stackable && _socket.IsHoldingItem)
                return false;

            if (_socket.IsHoldingItem && !_socket.PlacedItem.StackableItemController.Stackable)
                return false;

            if (!_socket.AnyTagAllowed && !_socket.AllowedTags.Contains(placeableItem.ItemTag))
                return false;

            if (!placeableItem.PlaceableItemPlacementCriteriaController.CanHighlight())
                return false;

            if (_socket.PreventItemPlacement)
                return false;

            if (!_socket.SocketPlacementCriteriaController.CanHighlight(placeableItem))
                return false;

            return true;
        }

        public void HighlightSocket(PlaceableItem placeableItem)
        {
            if (_isHighlighting)
            {
                return;
            }

            if (!placeableItem.SetSocketHighlighter(this))
            {
                return;
            }

            foreach (HighlighterEntry entry in _socketHighlighters)
            {
                ((ISocketHighlighter)entry.HighlighterComponent).StartHighlight(placeableItem);
            }

            _isHighlighting = true;
        }

        public void StopHighlightingSocket(PlaceableItem placeableItem)
        {
            if (placeableItem != null && placeableItem.CurrentSocketHighlighter == this)
            {
                placeableItem.ClearSocketHighlighter();
            }

            foreach (HighlighterEntry entry in _socketHighlighters)
            {
                ((ISocketHighlighter)entry.HighlighterComponent).StopHighlight();
            }

            _isHighlighting = false;
        }

        public bool HasHighlighter(string highlighterName)
        {
            return _socketHighlighters.Any(entry => entry.HighlighterName.Equals(highlighterName));
        }

        public void SetUseHighlighters(bool useHighlighters)
        {
            _useHighlighters = useHighlighters;
        }

        #endregion  Public Functions

        #region Editor Functions

#if UNITY_EDITOR
        public void SetSocket(Socket socket)
        {
            _socket = socket;
        }

        public void SetSocketHighlightAreaCollider(SocketHighlightAreaCollider socketHighlightAreaCollider)
        {
            _socketHighlightAreaCollider = socketHighlightAreaCollider;
        }

        public void AddHighlighter(string highlighterName)
        {
            if (_socketHighlighters.Any(entry => entry.HighlighterName == highlighterName))
            {
                Debug.LogErrorFormat(this, "Highlighter with name [{0}] already exists", highlighterName);
                return;
            }

            Type highlighterType = TypeCache.GetTypesDerivedFrom<ISocketHighlighter>()
                .FirstOrDefault(type => type.Name == highlighterName);

            if (highlighterType == null)
            {
                Debug.LogErrorFormat(this, "Could not find highlighter type: [{0}]", highlighterName);
                return;
            }

            var highlighterComponent = gameObject.AddComponent(highlighterType);
            HighlighterEntry entry = new HighlighterEntry { HighlighterName = highlighterName, HighlighterComponent = highlighterComponent as MonoBehaviour };
            _socketHighlighters.Add(entry);

            _socketHighlighters = _socketHighlighters.OrderBy(e => e.HighlighterName).ToList();

            ((ISocketHighlighter)highlighterComponent).Setup(this);

            PrefabUtility.RecordPrefabInstancePropertyModifications(this);

            Debug.Log("Added: " + highlighterName);
        }

        public void RemoveHighlighter(string highlighterName)
        {
            HighlighterEntry entry = _socketHighlighters.FirstOrDefault(e => e.HighlighterName == highlighterName);

            if (entry == null)
            {
                Debug.LogErrorFormat(this, "Highlighter with name [{0}] does not exist", highlighterName);
                return;
            }

            _socketHighlighters.Remove(entry);

            if (_socketHighlighters.Count > 0)
            {
                _socketHighlighters = _socketHighlighters.OrderBy(e => e.HighlighterName).ToList();
            }

            DestroyImmediate(entry.HighlighterComponent as MonoBehaviour);

            PrefabUtility.RecordPrefabInstancePropertyModifications(this);

            Debug.Log("Removed: " + highlighterName);
        }


#endif
        #endregion Editor Functions
    }
}
