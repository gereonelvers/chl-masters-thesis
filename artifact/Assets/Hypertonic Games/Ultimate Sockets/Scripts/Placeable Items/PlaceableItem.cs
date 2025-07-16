using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hypertonic.Modules.UltimateSockets.Enums;
using Hypertonic.Modules.UltimateSockets.Highlighters;
using Hypertonic.Modules.UltimateSockets.PlaceableItems.Stacking;
using Hypertonic.Modules.UltimateSockets.Sockets;
using Hypertonic.Modules.UltimateSockets.XR;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.PlaceableItems
{
    public class PlaceableItem : MonoBehaviour
    {
        #region Events
        public delegate void PlaceableItemSocketEvent(Socket socket, PlaceableItem placeableItem);

        public event PlaceableItemSocketEvent OnPlaced;
        public event PlaceableItemSocketEvent OnRemovedFromSocket;


        public delegate void PlaceableItemEvent(PlaceableItem placeableItem);


        public event PlaceableItemEvent OnEnteredPlaceableZone;
        public event PlaceableItemEvent OnExitedPlaceableZone;

        #endregion Events

        #region Public Editor Only Properties

        public GameObject UtilityComponentContainer => _utilityComponentContainerGameObject;

        public int TabIndex { get => _tabIndex; }



        #endregion Public Editor Only Properties

        #region Public Properties

        public string ItemTag { get => _tag; set => _tag = value; }

        public bool Placed { get; private set; }
        public bool WithinPlaceableZone
        {
            get
            {
                return _withinPlaceableZone;
            }

            private set
            {
                _withinPlaceableZone = value;

                if (_withinPlaceableZone)
                {
                    OnEnteredPlaceableZone?.Invoke(this);
                }
                else
                {
                    OnExitedPlaceableZone?.Invoke(this);
                }
            }
        }

        public bool KeepScaleForDefaultPlacements => _keepScaleForDefaultPlacements;

        public Transform RootTransform => _rootTransform != null ? _rootTransform : transform;

        public List<Collider> NonSocketColliders => _nonSocketColliders;
        public SocketGrabCollider SocketGrabCollider => _socketGrabCollider;
        public PlaceableItemCollider PlaceableItemCollider => _socketDetectorCollider;

        public PlaceableItemPlacementCriteriaController PlaceableItemPlacementCriteriaController => _placementCriteriaContainer;
        public PlaceableItemPreviewController PlaceableItemPreviewController => _placeableItemPreviewController;
        public PlaceableItemMeshController PlaceableItemMeshController => _placeableItemMeshController;
        public PlaceableItemSocketScaler PlaceableItemSocketScaler => _socketScaler;
        public PlaceableItemRigidbody PlaceableItemRigidbody => _placeableItemRigidbody;
        public PlaceableItemPlacementController PlaceableItemPlacementController => _placeableItemPlacementController;
        public StackableItemController StackableItemController => _stackableItemController;

        public PlaceableItemGrabbable PlaceableItemGrabbable => _placeableItemGrabbable;

        public bool DestroyOnPlace => _destroyOnPlace;

        public Socket ClosestSocket => _closestSocket;
        public SocketHighlighter CurrentSocketHighlighter { get; private set; }

        // Used to prevent placement (Required for testing preview transitions out of play mode). 
        public bool PreventPlacement { get; private set; } = false;

        #region  Preview Object

        public bool UsePreviewController => _usePreviewController;


        #endregion Preview Object



        #endregion Public Properties

        [SerializeField]
        [HideInInspector]
        private string _tag;

        [SerializeField]
        [Tooltip("The transform that will be parented to the socket. Leave null to use this transform")]
        private Transform _rootTransform;

        [SerializeField]
        private PlaceableItemRigidbody _placeableItemRigidbody;

        [SerializeField]
        private SocketGrabCollider _socketGrabCollider;

        [SerializeField]
        private PlaceableItemCollider _socketDetectorCollider;

        [SerializeField]
        private PlaceableItemPlacementCriteriaController _placementCriteriaContainer;

        [SerializeField]
        private PlaceableItemMeshController _placeableItemMeshController;

        [SerializeField]
        private PlaceableItemSocketScaler _socketScaler;

        [SerializeField]
        private PlaceableItemPlacementController _placeableItemPlacementController;

        [SerializeField]
        private ExitPlaceZoneMethod _exitPlaceZoneMethod = ExitPlaceZoneMethod.COLLIDER;

        [SerializeField]
        private float _exitPlaceZoneThreshold = 0.001f;

        [SerializeField]
        private bool _destroyOnPlace = false;

        [SerializeField]
        private bool _clearParentWhenRemoved = true;

        [SerializeField]
        private bool _keepScaleForDefaultPlacements = true;

        [SerializeField]
        private StackableItemController _stackableItemController;

        private bool _withinPlaceableZone;

        private Socket _closestSocket;

        [SerializeField]
        private bool _usePreviewController;

        private List<Collider> _nonSocketColliders = new List<Collider>();

        [SerializeField]
        private PlaceableItemPreviewController _placeableItemPreviewController;

        [SerializeField]
        private PlaceableItemGrabbable _placeableItemGrabbable;

        private Coroutine _trackDistanceToSocketCoroutine;

        #region Private Editor Use Only Variables

        [SerializeField]
        private GameObject _utilityComponentContainerGameObject;

        // Used by PlaceableItemEditor.
        [SerializeField]
        private int _tabIndex = 0;

        // Used by PlaceableItemEditor.
        [SerializeField]
        private Socket _previewTransitionSocket;

        #endregion Private Editor Use Only Variables



        #region Unity Functions

        private void Awake()
        {
            SetComponentReferences();
        }

        #endregion Unity Functions

        #region Public Functions

        public void Enable()
        {
            PlaceableItemCollider.Enable();
        }

        public void Disable()
        {
            PlaceableItemCollider.Disable();
        }

        public void HandleEnteredPlaceableZone(Socket socket)
        {
            if (_closestSocket != null && _closestSocket.SocketPlaceCollider.IsEnabledAndActiveInHierarchy)
            {
                return;
            }

            if (_closestSocket != null && !_closestSocket.SocketPlaceCollider.IsEnabledAndActiveInHierarchy)
            {
                ClearClosestPlacePoint();
            }


            SetClosestPlacePoint(socket);

            if (CurrentSocketHighlighter != null && CurrentSocketHighlighter != socket.SocketHighlighter)
            {
                CurrentSocketHighlighter.StopHighlightingSocket(null);
                CurrentSocketHighlighter = socket.SocketHighlighter;
            }

            if (UsePreviewController)
            {
                _placeableItemPreviewController.HandleEnteredPlaceableZone(socket);
            }

            WithinPlaceableZone = true;
        }

        public void HandleLeftPlaceableZone(Socket socket)
        {
            if (!_exitPlaceZoneMethod.Equals(ExitPlaceZoneMethod.COLLIDER))
                return;

            // If we've left the placeable area of a different socket, ignore this.
            if (_closestSocket != null && _closestSocket != socket)
            {
                return;
            }

            HandleMovedOutOfPlaceRadius();
        }

        public void PlaceInSocket()
        {
            if (_closestSocket == null)
            {
                Debug.LogError("Cannot place into the socket as the closest socket is null");
                return;
            }

            PlaceInSocket(_closestSocket);
        }

        public void PlaceInSocket(Socket socket)
        {
            _closestSocket = socket;

            if (!_closestSocket.CanPlace(this))
            {
                if (!StackableItemController.Spawning)
                {
                    Debug.LogWarning("Unable to place into the socket as the socket cannot accept the item", this);
                }

                return;
            }

            _closestSocket.PlaceItem(this);
        }

        public void HandlePlaced(Socket socket)
        {
            foreach (Collider c in _nonSocketColliders)
            {
                c.enabled = false;
            }

            OnPlaced?.Invoke(socket, this);

            _closestSocket = socket;

            Placed = true;
            WithinPlaceableZone = false;

            if (DestroyOnPlace)
            {
                RemoveFromSocket();
                Destroy(RootTransform.gameObject);
            }
        }

        public void RemoveFromSocket()
        {
            if (!Placed)
            {
                Debug.LogError("The placeable item cannot be removed from the socket as it is not placed");
                return;
            }

            _closestSocket.RemoveItem(this);
        }

        public void HandleRemovedFromSocket(Socket socket)
        {
            foreach (Collider c in _nonSocketColliders)
            {
                c.enabled = true;
            }

            if (_closestSocket != null)
            {
                ClearClosestPlacePoint();
            }

            Placed = false;

            if (_clearParentWhenRemoved)
            {
                RootTransform.SetParent(null, true);
            }

            OnRemovedFromSocket?.Invoke(socket, this);
        }

        public bool SetSocketHighlighter(SocketHighlighter socketHighlighter)
        {
            if (ClosestSocket != null && ClosestSocket != socketHighlighter.Socket)
            {
                return false;
            }

            if (CurrentSocketHighlighter != null && CurrentSocketHighlighter != socketHighlighter)
            {
                CurrentSocketHighlighter.StopHighlightingSocket(null);
            }

            CurrentSocketHighlighter = socketHighlighter;

            return true;
        }

        public void ClearSocketHighlighter()
        {
            CurrentSocketHighlighter = null;
        }

        public void PreventItemRemoval()
        {
            SocketGrabCollider.DisableCollider();
            PlaceableItemGrabbable.DisableGrabbale();
        }

        public void EnableItemRemoval()
        {
            SocketGrabCollider.EnableCollider();
            PlaceableItemGrabbable.EnableGrabbable();
        }

        public void SetPreventPlacement(bool preventPlacement)
        {
            PreventPlacement = preventPlacement;
        }

        #endregion Public Functions

        #region Private Functions

        private void SetComponentReferences()
        {
            if (_socketDetectorCollider == null)
            {
                Debug.LogError("Could not obtain a reference to the _placeableItemCollider component");
                return;
            }

            if (_placeableItemPreviewController == null)
            {
                Debug.LogError("Could not obtain a refernce to the _placeableItemPreviewController component", this);
                return;
            }

            if (_socketGrabCollider == null)
            {
                Debug.LogError("Could not obtain a refernce to the _socketGrabCollider component", this);
                return;
            }

            if (string.IsNullOrEmpty(_tag))
            {
                Debug.LogErrorFormat(this, "The placeable item tag has not been set on the placeable item: [{0}]", gameObject.name);
                return;
            }

            if (_placeableItemMeshController == null)
            {
                Debug.LogError("Could not obtain a reference to the _placeableItemMeshController component", this);
                return;
            }

            _nonSocketColliders = GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.TryGetComponent(out SocketGrabCollider _) && !c.TryGetComponent(out PlaceableItemCollider _)).ToList();
        }

        private void ClearClosestPlacePoint()
        {
            _closestSocket = null;
        }

        private void SetClosestPlacePoint(Socket socket)
        {
            _closestSocket = socket;

            if (_exitPlaceZoneMethod.Equals(ExitPlaceZoneMethod.COLLIDER))
                return;

            StopTrackDistanceToPlacePointCoroutineIfActive();

            _trackDistanceToSocketCoroutine = StartCoroutine(TrackDistanceToSocketCoroutine(socket));
        }

        private IEnumerator TrackDistanceToSocketCoroutine(Socket socket)
        {
            yield return new WaitForFixedUpdate();

            float placeRadius = socket.PlaceRadius * transform.lossyScale.x;

            while (_closestSocket != null && socket == _closestSocket && !Placed)
            {
                float distanceToSocket = Vector3.Distance(_socketDetectorCollider.transform.position, _closestSocket.SocketPlaceCollider.ColliderManager.Collider.transform.position);

                float outOfRangeDistance = 0f;

                if (_exitPlaceZoneMethod.Equals(ExitPlaceZoneMethod.DISTANCE))
                {
                    outOfRangeDistance = _exitPlaceZoneThreshold;
                }
                else if (_exitPlaceZoneMethod.Equals(ExitPlaceZoneMethod.COLLIDER_THRESHOLD))
                {
                    outOfRangeDistance = placeRadius + _exitPlaceZoneThreshold;
                }


                if (distanceToSocket > outOfRangeDistance)
                {
                    HandleMovedOutOfPlaceRadius();
                    yield break;
                }

                yield return new WaitForFixedUpdate();
            }
        }

        private void HandleMovedOutOfPlaceRadius()
        {
            if (UsePreviewController)
            {
                _placeableItemPreviewController.HandleMovedAwayFromSocket(_closestSocket);
            }

            ClearClosestPlacePoint();

            WithinPlaceableZone = false;
        }

        private void StopTrackDistanceToPlacePointCoroutineIfActive()
        {
            if (_trackDistanceToSocketCoroutine != null)
            {
                StopCoroutine(_trackDistanceToSocketCoroutine);
                _trackDistanceToSocketCoroutine = null;
            }
        }

        #endregion Private Functions

        #region Editor Functions
        // Designed to only be called from the editor for setup purposes.

        public void SetTag(string tag)
        {
            _tag = tag;
        }

        public void SetRootTransform(Transform rootTransform)
        {
            _rootTransform = rootTransform;
        }

        public void SetGrabCollider(SocketGrabCollider socketGrabCollider)
        {
            _socketGrabCollider = socketGrabCollider;
        }

        public void SetSocketDetectorCollider(PlaceableItemCollider socketDetectorCollider)
        {
            _socketDetectorCollider = socketDetectorCollider;
        }

        public void SetPlacementCriteriaContainer(PlaceableItemPlacementCriteriaController placeableItemPlacementCriteriaController)
        {
            _placementCriteriaContainer = placeableItemPlacementCriteriaController;
        }

        public void SetMeshController(PlaceableItemMeshController placeableItemMeshController)
        {
            _placeableItemMeshController = placeableItemMeshController;
        }

        public void HandleMovedOutOfPlaceRadiusEditor()
        {
            StopTrackDistanceToPlacePointCoroutineIfActive();

            HandleMovedOutOfPlaceRadius();
        }

        public void SetUtilityComponentContainerGameObject(GameObject utilityComponentContainerGameObject)
        {
            _utilityComponentContainerGameObject = utilityComponentContainerGameObject;
        }

        public void SetPlaceableItemPreviewController(PlaceableItemPreviewController placeableItemPreviewController)
        {
            _placeableItemPreviewController = placeableItemPreviewController;
        }

        public void SetStackableItemController(StackableItemController stackableItemController)
        {
            _stackableItemController = stackableItemController;
        }

        public void SetGrabbable(PlaceableItemGrabbable grabbable)
        {
            _placeableItemGrabbable = grabbable;
        }

        public void SetSocketScaler(PlaceableItemSocketScaler socketScaler)
        {
            _socketScaler = socketScaler;
        }

        public void SetPlaceableItemPlacementController(PlaceableItemPlacementController placeableItemPlacementController)
        {
            _placeableItemPlacementController = placeableItemPlacementController;
        }

        public void SetPlaceableItemRigidBody(PlaceableItemRigidbody placeableItemRigidBody)
        {
            _placeableItemRigidbody = placeableItemRigidBody;
        }

        #endregion Editor Functions
    }
}
