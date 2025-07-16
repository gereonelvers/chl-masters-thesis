using System;
using System.Collections;
using System.Collections.Generic;
using Hypertonic.Modules.UltimateSockets.Enums;
using Hypertonic.Modules.UltimateSockets.Models;
using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using Hypertonic.Modules.UltimateSockets.Stacking;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Sockets.Stacking
{
    public class SocketStackableItemController : MonoBehaviour
    {
        #region Events

        public delegate void SocketStackableItemControllerEvent();
        public event SocketStackableItemControllerEvent OnStackableItemAdded;
        public event SocketStackableItemControllerEvent OnStackableItemRemoved;
        public event SocketStackableItemControllerEvent OnStackSizeChanged;

        public event SocketStackableItemControllerEvent OnItemAddedToCloneStack;
        public event SocketStackableItemControllerEvent OnItemAddedToInstanceStack;


        #endregion Events

        public List<SpawnTransitionEntry> SpawnTransitionEntries => _stackableItemSpawnTransitionController.SpawnTransitionEntries;

        public SocketStackableItemControllerData Settings => _settings;

        public int StackSize
        {
            get { return _stackSize; }
            set
            {
                if (value < 0)
                {
                    Debug.LogError("Stack size cannot be less than 0.");
                }

                _stackSize = value;
                OnStackSizeChanged?.Invoke();
            }
        }
        public bool IsEmpty => StackSize <= 0;
        public bool IsFull => StackSize >= _settings.MaxStackSize;



        public GameObject StackCounterUI { get { return _stackCountUI; } set { _stackCountUI = value; } }

        [SerializeField]
        private SocketStackableItemControllerData _settings;

        [SerializeField]
        private StackableItemSpawnTransitionController _stackableItemSpawnTransitionController;

        private int _stackSize = 0;

        [SerializeField]
        private Socket _socket;

        [SerializeField]
        // This is purely to hold a reference for the custom inspector tab
        private GameObject _stackCountUI;

        private readonly Queue<PlaceableItem> _fifoStack = new Queue<PlaceableItem>();
        private readonly Stack<PlaceableItem> _filoStack = new Stack<PlaceableItem>();

        private Coroutine _enableReplacementInstanceCoroutine;

        private Coroutine _spawnReplacementCloneCoroutine;

        private GameObject _cloneInstance;

        #region Unity Functions

        private void Awake()
        {
            if (_socket == null)
            {
                Debug.LogErrorFormat(this, "No socket found on {0}.", this.name);
                return;
            }

            if (_settings == null)
            {
                Debug.LogErrorFormat(this, "No settings found on {0}.", this.name);
                return;
            }

            if (_settings.StackType != StackType.CLONE)
            {
                _settings.InfiniteReplacement = false;
            }
        }

        #endregion Unity Functions

        #region  Public Functions

        public bool CanAddToStack()
        {
            if (_settings.InfiniteReplacement && StackSize == 1)
            {
                return true;
            }

            return StackSize < _settings.MaxStackSize;
        }

        public void AddToStack(PlaceableItem placeableItem, bool invokeEvent = true)
        {
            if (placeableItem.StackableItemController.Spawning)
            {
                _socket.PlaceSingleItem(placeableItem, invokeEvent);
                return;
            }

            if (!CanAddToStack())
            {
                Debug.LogErrorFormat(this, "Cannot add {0} to stack. Stack is full.", placeableItem.name);
                return;
            }

            if (IsEmpty)
            {
                if (_settings.StackType == StackType.CLONE)
                {
                    if (_cloneInstance != null)
                    {
                        Destroy(_cloneInstance);
                    }

                    _cloneInstance = Instantiate(placeableItem.RootTransform.gameObject);
                    _cloneInstance.SetActive(false);
                    _cloneInstance.transform.parent = transform;
                }
                _socket.PlaceSingleItem(placeableItem, invokeEvent);
            }
            else
            {
                switch (_settings.StackType)
                {
                    case StackType.CLONE: AddToStackClone(placeableItem); break;
                    case StackType.INSTANCE: AddToStackInstance(placeableItem, invokeEvent); break;
                }
            }

            StackSize++;
            OnStackableItemAdded?.Invoke();
        }

        public PlaceableItem RemoveFromStack(PlaceableItem placeableItem)
        {
            if (IsEmpty)
            {
                Debug.LogErrorFormat(this, "Cannot remove item from stack. Stack is empty.");
                return null;
            }

            switch (_settings.StackType)
            {
                case StackType.CLONE: RemoveFromStackClone(placeableItem); break;
                case StackType.INSTANCE: RemoveFromStackInstance(placeableItem); break;
            }

            StackSize--;

            if (_settings.InfiniteReplacement && StackSize <= 0)
            {
                StackSize = 1;
            }

            OnStackableItemRemoved?.Invoke();

            return placeableItem;
        }

        public void AddItemSpawnTransitionController()
        {
            if (_stackableItemSpawnTransitionController == null)
            {
                _stackableItemSpawnTransitionController = gameObject.AddComponent<StackableItemSpawnTransitionController>();
            }
        }

        public void AddSpawnTransition(string transitionName, Type transitionType)
        {
            _stackableItemSpawnTransitionController.AddSpawnTransition(transitionName, transitionType);
        }

        public void RemoveSpawnTransition(string transitionName)
        {
            _stackableItemSpawnTransitionController.RemoveSpawnTransition(transitionName);
        }

        public bool HasSpawnTransition(string transitionName)
        {
            return _stackableItemSpawnTransitionController.HasSpawnTransition(transitionName);
        }

        #endregion Public Functions

        #region Private Functions

        private void AddToStackClone(PlaceableItem placeableItem)
        {
            OnItemAddedToCloneStack?.Invoke();

            Destroy(placeableItem.RootTransform.gameObject);
        }

        private void AddToStackInstance(PlaceableItem placeableItem, bool invokeEvent = true)
        {
            if (_settings.InstanceStackType == InstanceStackType.FIFO)
            {
                placeableItem.RootTransform.gameObject.SetActive(false);
                placeableItem.RootTransform.SetParent(_socket.SocketPlaceTransform.transform);
                _fifoStack.Enqueue(placeableItem);
            }
            else if (_settings.InstanceStackType == InstanceStackType.FILO)
            {
                if (_socket.PlacedItem != null)
                {
                    _socket.PlacedItem.RootTransform.gameObject.SetActive(false);

                    _socket.PlacedItem.PlaceableItemRigidbody.SetKinematic(true);

                    _filoStack.Push(_socket.PlacedItem);
                }

                _socket.PlaceSingleItem(placeableItem, invokeEvent);
            }

            OnItemAddedToInstanceStack?.Invoke();
        }

        private void RemoveFromStackClone(PlaceableItem placeableItem)
        {
            _socket.RemoveSingleItem(placeableItem);

            if (StackSize <= 1 && !_settings.InfiniteReplacement)
            {
                return;
            }

            StopSpawnReplacementCloneCoroutine();

            _spawnReplacementCloneCoroutine = StartCoroutine(SpawnReplacementCloneCoroutine());
        }

        private void RemoveFromStackInstance(PlaceableItem placeableItem)
        {
            _socket.RemoveSingleItem(placeableItem);

            if (StackSize <= 1)
            {
                return;
            }

            StopEnableReplacementInstanceCoroutine();

            _enableReplacementInstanceCoroutine = StartCoroutine(EnableReplacementInstanceCoroutine());
        }

        private PlaceableItem FindPlaceableItem(GameObject gameObject)
        {
            PlaceableItem placeableItem = gameObject.GetComponent<PlaceableItem>();
            if (placeableItem != null)
            {
                return placeableItem;
            }

            foreach (Transform child in gameObject.transform)
            {
                placeableItem = FindPlaceableItem(child.gameObject);

                if (placeableItem != null)
                {
                    return placeableItem;
                }
            }

            return null;
        }

        private IEnumerator SpawnReplacementCloneCoroutine()
        {
            yield return new WaitForSeconds(_settings.StackReplacementDelay);

            GameObject replacement = GameObject.Instantiate(_cloneInstance,
            _socket.SocketPlaceTransform.transform.position,
            _socket.SocketPlaceTransform.transform.rotation);

            replacement.name = replacement.name.Replace("(Clone)", "");

            replacement.SetActive(true);

            PlaceableItem replacementPlaceableItem = FindPlaceableItem(replacement);

            if (replacementPlaceableItem == null)
            {
                Debug.LogErrorFormat(this, "Could not find PlaceableItem component on replacement item.");
                yield break;
            }


            // Setting IsSpawning to true here so that when it's placed in the socket, it is handled as a spawning stack item.
            // Waiting for IsSpawning to be set on PreformSapwnTransition causes the spawn transitions to alter the state of the item before it's placed causing issues when systems
            // store the state of the item when it's placed such as SocketScaler. 
            replacementPlaceableItem.StackableItemController.SetIsSpawning(true);
            replacementPlaceableItem.HandleEnteredPlaceableZone(_socket);
            replacementPlaceableItem.PlaceInSocket(_socket);

            PerformSpawnTranstion(replacementPlaceableItem);
        }

        private IEnumerator EnableReplacementInstanceCoroutine()
        {
            yield return new WaitForSeconds(_settings.StackReplacementDelay);

            // If an item has been placed in the time it's taken to replace it, don't replace it.
            if (_socket.PlacedItem != null)
            {
                yield break;
            }

            PlaceableItem replacementPlaceableItem = GetInstanceFromStack();

            if (replacementPlaceableItem == null)
            {
                Debug.LogErrorFormat(this, "Unable to get the replacement stack item.");
                yield break;
            }

            replacementPlaceableItem.RootTransform.gameObject.SetActive(true);

            replacementPlaceableItem.StackableItemController.SetIsSpawning(true);

            replacementPlaceableItem.HandleEnteredPlaceableZone(_socket);
            replacementPlaceableItem.PlaceInSocket(_socket);

            PerformSpawnTranstion(replacementPlaceableItem);

            _socket.PlacedItem = replacementPlaceableItem;
        }

        private void PerformSpawnTranstion(PlaceableItem placeableItem)
        {
            placeableItem.StackableItemController.ActivateSpawnTransitions(_socket, placeableItem);
        }

        private void StopEnableReplacementInstanceCoroutine()
        {
            if (_enableReplacementInstanceCoroutine != null)
            {
                StopCoroutine(_enableReplacementInstanceCoroutine);
                _enableReplacementInstanceCoroutine = null;
            }
        }

        private void StopSpawnReplacementCloneCoroutine()
        {
            if (_spawnReplacementCloneCoroutine != null)
            {
                StopCoroutine(_spawnReplacementCloneCoroutine);
                _spawnReplacementCloneCoroutine = null;
            }
        }

        private PlaceableItem GetInstanceFromStack()
        {
            PlaceableItem replacementPlaceableItem = null;

            if (_settings.InstanceStackType == InstanceStackType.FIFO)
            {
                if (_fifoStack.Count <= 0)
                {
                    Debug.LogErrorFormat(this, "FIFO Stack is empty.");
                    return null;
                }

                replacementPlaceableItem = _fifoStack.Dequeue();
            }
            else if (_settings.InstanceStackType == InstanceStackType.FILO)
            {
                if (_filoStack.Count <= 0)
                {
                    Debug.LogErrorFormat(this, "FILO Stack is empty.");
                    return null;
                }

                replacementPlaceableItem = _filoStack.Pop();
            }
            else
            {
                Debug.LogErrorFormat(this, "Invalid InstanceStackType: {0}", _settings.InstanceStackType);
            }

            return replacementPlaceableItem;
        }

        #endregion Private Functions

        #region Editor Functions

        public void SetSocket(Socket socket)
        {
            _socket = socket;
        }
#if UNITY_EDITOR
        public void AddSpawnTransition(string transitionName)
        {
            _stackableItemSpawnTransitionController.AddSpawnTransition(transitionName);
        }

#endif

        #endregion Editor Functions
    }
}
