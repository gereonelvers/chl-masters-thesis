using Hypertonic.Modules.UltimateSockets.Enums;
using Hypertonic.Modules.UltimateSockets.Highlighters;
using Hypertonic.Modules.UltimateSockets.Models;
using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using Hypertonic.Modules.UltimateSockets.Sockets.Audio;
using Hypertonic.Modules.UltimateSockets.Sockets.Stacking;
using System.Collections.Generic;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Sockets
{
    public class Socket : MonoBehaviour
    {
        #region Events

        public delegate void SocketEvent(Socket socket);
        public delegate void SocketItemEvent(Socket socket, PlaceableItem placeableItem);

        public event SocketItemEvent OnItemPlaced;
        public event SocketItemEvent OnItemRemoved;
        public event SocketItemEvent OnItemEnteredPlaceableAreaCollider;
        public event SocketItemEvent OnItemLeftPlaceableAreaCollider;

        public delegate void SocketBoolEvent(Socket socket);
        public event SocketBoolEvent OnPreventItemPlacementChanged;
        public event SocketBoolEvent OnPreventPhysicalItemPlacementChanged;
        public event SocketBoolEvent OnPreventItemRemovalChanged;

        public event SocketEvent OnStackSizeChanged;

        #endregion Events

        #region  Properties


        #region Public Editor Only Properties

        public int TabIndex { get => _tabIndex; }


        #endregion Public Editor Only Properties
        public bool IsSetup => _isSetup;

        public PlaceableItem PlacedItem { get; set; }

        public bool IsHoldingItem => PlacedItem != null;

        public SocketPlaceCollider SocketPlaceCollider => _socketPlaceCollider;

        public SocketPlaceTransform SocketPlaceTransform => _socketPlaceTransform;

        public SocketPlacementProfileScriptableObject SocketPlacementProfileScriptableObject => _placementProfileScriptableObject;

        public SocketPlacementProfile SocketPlacementProfile => _placementProfileType == PlacementProfileType.SCRIPTABLE_OBJECT ? _placementProfileScriptableObject?.SocketPlacementProfile : _placementProfile;

        public PlacementProfileType PlacementProfileType => _placementProfileType;

        public SocketHighlighter SocketHighlighter => _socketHighlighter;
        public SocketHighlightAreaCollider SocketHighlightAreaCollider => _socketHighlightAreaCollider;
        public SocketPlacementCriteriaController SocketPlacementCriteriaController => _socketPlacementCriteriaController;
        public SocketStackableItemController StackableItemController => _stackableItemController;
        public SocketAudioController SocketAudioController => _socketAudioController;

        public bool AnyTagAllowed => _anyTagAllowed;

        public List<string> AllowedTags => _allowedTags;

        public bool PreventPreviewItems => _preventPreviewItems;

        public float PlaceRadius => _socketPlaceCollider == null ? 0f : _socketPlaceCollider.ColliderManager.GetRadius();

        public ItemPlacementConfig DefaultItemPlacementConfig => _defaultItemPlacementConfig;

        public PlaceableItem PlaceOnStart => _placeOnStart;

        public bool PreventItemPlacement
        {
            get { return _preventItemPlacement; }
            set
            {
                _preventItemPlacement = value;
                UpdatePreventItemPlacementFunctionality();
                OnPreventItemPlacementChanged?.Invoke(this);
            }
        }

        public bool PreventPhysicalItemPlacement
        {
            get { return _preventPhysicalItemPlacement; }
            set
            {
                _preventPhysicalItemPlacement = value;
                UpdatePreventPhysicalItemPlacement();
                OnPreventPhysicalItemPlacementChanged?.Invoke(this);
            }
        }

        public bool PreventItemRemoval
        {
            get { return _preventItemRemoval; }
            set
            {
                _preventItemRemoval = value;
                UpdatePreventItemRemovalFunctionality();
                OnPreventItemRemovalChanged?.Invoke(this);
            }
        }

        #endregion Properties

        [SerializeField]
        private SocketPlacementProfileScriptableObject _placementProfileScriptableObject;

        [SerializeField]
        private SocketPlacementProfile _placementProfile;

        [SerializeField]
        private PlacementProfileType _placementProfileType;

        [SerializeField]
        private SocketPlaceCollider _socketPlaceCollider;

        [SerializeField]
        private SocketPlaceTransform _socketPlaceTransform;

        [SerializeField]
        private SocketHighlighter _socketHighlighter;

        [SerializeField]
        private SocketHighlightAreaCollider _socketHighlightAreaCollider;

        [SerializeField]
        private SocketStackableItemController _stackableItemController;

        [SerializeField]
        private SocketAudioController _socketAudioController;

        [HideInInspector]
        [SerializeField]
        private List<string> _allowedTags = new List<string>();

        [SerializeField]
        private bool _anyTagAllowed = false;

        [SerializeField]
        private SocketPlacementCriteriaController _socketPlacementCriteriaController;

        [SerializeField]
        private ItemPlacementConfig _defaultItemPlacementConfig = new ItemPlacementConfig();

        [SerializeField]
        private bool _preventItemPlacement = false;

        [SerializeField]
        private bool _preventPhysicalItemPlacement = false;

        [SerializeField]
        private bool _preventItemRemoval = false;

        [SerializeField]
        private PlaceableItem _placeOnStart = null;

        [SerializeField]
        private bool _preventPreviewItems = false;

        // A way of allowing the socket to place an item on start when the prevent item placement is set to true.
        private bool _isSetup = false;

        #region Editor Only Private Variables

        // Used by SocketEditor.cs
        [SerializeField]
        private int _tabIndex = 0;

        #endregion Editor Only Private Variables


        #region Unity Functions

        private void Awake()
        {
            if (_socketHighlighter == null)
            {
                Debug.LogError("The _placePointHighlighter has not been assigned", this);
                return;
            }

            if (_socketPlaceTransform == null)
            {
                Debug.LogError("The _socketPlaceTransform has not been assigned", this);
                return;
            }

            if (_socketPlaceCollider == null)
            {
                Debug.LogError("The _socketPlaceCollider has not been assigned", this);
                return;
            }

            if (_socketPlacementCriteriaController == null)
            {
                Debug.LogError("The _socketPlacementCriteriaController has not been assigned", this);
                return;
            }
        }

        private void OnEnable()
        {
            _socketPlaceCollider.OnItemWithinPlaceableArea += HandlePlaceableItemNear;
            _socketPlaceCollider.OnItemLeftPlaceableArea += HandlePlaceableItemLeft;

            _stackableItemController.OnStackableItemAdded += HandleItemStackIncreased;
            StackableItemController.OnStackSizeChanged += HandleStackSizeChanged;

            UpdatePreventItemPlacementFunctionality();
        }

        private void OnDisable()
        {
            _socketPlaceCollider.OnItemWithinPlaceableArea -= HandlePlaceableItemNear;
            _socketPlaceCollider.OnItemLeftPlaceableArea -= HandlePlaceableItemLeft;

            _stackableItemController.OnStackableItemAdded -= HandleItemStackIncreased;
            StackableItemController.OnStackSizeChanged -= HandleStackSizeChanged;
        }

        private void Start()
        {
            if (_placeOnStart != null)
            {
                PlaceItem(_placeOnStart);
            }

            if (_preventPhysicalItemPlacement)
            {
                SocketPlaceCollider.Disable();
                SocketHighlightAreaCollider.Disable();
            }

            _isSetup = true;
        }

        #endregion Unity Functions

        #region Public Functions

        public void Enable()
        {
            SocketPlaceCollider.Enable();
            SocketHighlightAreaCollider.Enable();
        }

        public void Disable()
        {
            SocketPlaceCollider.Disable();
            SocketHighlightAreaCollider.Disable();
        }

        public void PlaceItem(PlaceableItem placeableItem, bool invokeEvent = true)
        {
            if (PreventItemPlacement && _isSetup && !placeableItem.StackableItemController.Spawning)
            {
                Debug.LogErrorFormat(this, "Cannot place item. PreventItemPlacement is set to true");
                return;
            }

            if (IsStackableItem(placeableItem))
            {
                PlaceStackableItem(placeableItem, invokeEvent);
            }
            else
            {
                PlaceSingleItem(placeableItem, invokeEvent);
            }
        }

        public void PlaceSingleItem(PlaceableItem placeableItem, bool invokeEvent = true)
        {
            OnBeforeItemPlaced(placeableItem);

            if (!CanPlace(placeableItem))
            {
                Debug.LogError("Cannot place item. Try calling CanPlace() first to check if the place point will accept the item", this);
                return;
            }

            placeableItem.HandlePlaced(this);

            PlacedItem = placeableItem;

            _socketPlaceTransform.PlaceItem(PlacedItem);

            if (PreventItemRemoval)
            {
                PlacedItem.SocketGrabCollider.DisableCollider();
            }

            if (invokeEvent)
            {
                OnItemPlaced?.Invoke(this, placeableItem);
            }

            OnAfterItemPlaced(placeableItem);
        }

        public virtual void RemoveItem()
        {
            RemoveItem(PlacedItem);
        }

        public virtual void RemoveItem(PlaceableItem placeableItem)
        {
            if (placeableItem == null)
            {
                Debug.LogErrorFormat(this, "Cannot remove the placeable item as it is null");
                return;
            }

            if (_preventItemRemoval)
            {
                Debug.LogWarningFormat(this, "Cannot remove item from socket because it is set to prevent item removal");
                return;
            }

            if (_stackableItemController.Settings.Stackable && placeableItem.StackableItemController.Stackable)
            {
                _stackableItemController.RemoveFromStack(placeableItem);
                return;
            }

            RemoveSingleItem(placeableItem);
        }

        public void RemoveSingleItem(PlaceableItem placeableItem)
        {
            placeableItem.HandleRemovedFromSocket(this);

            PlacedItem = null;

            OnItemRemoved?.Invoke(this, placeableItem);
        }

        public void SetAllowedTag(string tag)
        {
            if (_allowedTags.Contains(tag))
            {
                Debug.LogErrorFormat(this, "Cannot add the tag: [{0}] to the list of AllowedPlaceableTags as it is already in the list", tag);
                return;
            }

            _allowedTags.Add(tag);
        }

        public void SetNotAllowedTag(string tag)
        {
            if (!_allowedTags.Contains(tag))
            {
                Debug.LogErrorFormat(this, "Cannot remove the tag: [{0}] from the list of AllowedPlaceableTags as it is not already in the list", tag);
                return;
            }

            _allowedTags.Remove(tag);
        }

        public void SetAllowedTags(List<string> tags)
        {
            _allowedTags.Clear();

            _allowedTags.AddRange(tags);
        }

        public bool CanPlace(PlaceableItem placeableItem, List<string> placementCriteriaNamesToIgnore = null)
        {
            if (!_stackableItemController.Settings.Stackable && IsHoldingItem)
            {
                return false;
            }

            if (!placeableItem.StackableItemController.Stackable && IsHoldingItem)
            {
                return false;
            }

            if (IsHoldingItem && !PlacedItem.StackableItemController.Stackable)
            {
                return false;
            }

            if (_stackableItemController.Settings.Stackable && !_stackableItemController.CanAddToStack())
            {
                return false;
            }

            if (!_anyTagAllowed && !AllowedTags.Contains(placeableItem.ItemTag))
            {
                return false;
            }

            if (!placeableItem.PlaceableItemPlacementCriteriaController.CanPlace(placementCriteriaNamesToIgnore))
            {
                return false;
            }

            if (PreventItemPlacement && _isSetup && !placeableItem.StackableItemController.Spawning)
            {
                return false;
            }

            if (!_socketPlacementCriteriaController.CanPlace(placeableItem, placementCriteriaNamesToIgnore))
            {
                return false;
            }

            return true;
        }




        #endregion Public Functions

        #region Protected Functions

        protected virtual void OnBeforeItemPlaced(PlaceableItem placeableItem) { }

        protected virtual void OnAfterItemPlaced(PlaceableItem placeableItem) { }

        #endregion Protected Functions

        #region Private Functions

        private void HandlePlaceableItemNear(PlaceableItem placeableItem)
        {
            placeableItem.HandleEnteredPlaceableZone(this);
            OnItemEnteredPlaceableAreaCollider?.Invoke(this, placeableItem);
        }

        private void HandlePlaceableItemLeft(PlaceableItem placeableItem)
        {
            placeableItem.HandleLeftPlaceableZone(this);
            OnItemLeftPlaceableAreaCollider?.Invoke(this, placeableItem);
        }

        private void HandleItemStackIncreased()
        {
            _socketHighlighter.StopHighlightingSocket(null);
        }

        private void HandleStackSizeChanged()
        {
            OnStackSizeChanged?.Invoke(this);
        }

        private void PlaceStackableItem(PlaceableItem placeableItem, bool invokeEvent = true)
        {
            if (!_stackableItemController.CanAddToStack() && !placeableItem.StackableItemController.Spawning)
            {
                Debug.LogError("Cannot stack item. Try calling CanPlace() first to check if the socket will accept the item");
                return;
            }

            _stackableItemController.AddToStack(placeableItem, invokeEvent);
        }

        private void UpdatePreventItemRemovalFunctionality()
        {
            if (PreventItemRemoval)
            {
                PlacedItem?.PreventItemRemoval();
            }
            else
            {
                PlacedItem?.EnableItemRemoval();
            }
        }

        private void UpdatePreventItemPlacementFunctionality()
        {
            if (PreventItemPlacement)
            {
                DisablePhysicalItemPlacement();
            }
            else
            {
                EnablePhysicalItemPlacement();
            }
        }

        private void UpdatePreventPhysicalItemPlacement()
        {
            if (PreventPhysicalItemPlacement)
            {
                DisablePhysicalItemPlacement();
            }
            else
            {
                EnablePhysicalItemPlacement();
            }
        }

        private void DisablePhysicalItemPlacement()
        {
            SocketPlaceCollider.Disable();
            _socketHighlightAreaCollider.Disable();
        }

        private void EnablePhysicalItemPlacement()
        {
            SocketPlaceCollider.Enable();
            _socketHighlightAreaCollider.Enable();
        }

        private bool IsStackableItem(PlaceableItem placeableItem)
        {
            return _stackableItemController.Settings.Stackable && placeableItem.StackableItemController.Stackable;
        }

        #endregion Private Functions

        #region Editor Functions
        // Designed to only be called from the editor for setup purposes

        public void SetSocketPlaceCollider(SocketPlaceCollider socketPlaceCollider)
        {
            _socketPlaceCollider = socketPlaceCollider;
        }

        public void SetSocketHighlighter(SocketHighlighter socketHighlighter)
        {
            _socketHighlighter = socketHighlighter;
        }

        public void SetSocketHighlightAreaCollider(SocketHighlightAreaCollider socketHighlightAreaCollider)
        {
            _socketHighlightAreaCollider = socketHighlightAreaCollider;
        }

        public void SetPlacementCriteriaContainer(SocketPlacementCriteriaController socketPlacementCriteriaController)
        {
            _socketPlacementCriteriaController = socketPlacementCriteriaController;
        }

        public void SetSocketPlacementTransform(SocketPlaceTransform socketPlaceTransform)
        {
            _socketPlaceTransform = socketPlaceTransform;
        }

        public void SetDefaultItemPlacementConfig(ItemPlacementConfig defaultItemPlacementConfig)
        {
            _defaultItemPlacementConfig = defaultItemPlacementConfig;
        }

        public void SetPlacementProfile(SocketPlacementProfileScriptableObject placementProfile)
        {
            _placementProfileScriptableObject = placementProfile;
        }

        public void SetStackableItemController(SocketStackableItemController stackableItemController)
        {
            _stackableItemController = stackableItemController;
        }

        public void SetAudioController(SocketAudioController socketAudioController)
        {
            _socketAudioController = socketAudioController;
        }

        #endregion Editor Functions
    }
}
