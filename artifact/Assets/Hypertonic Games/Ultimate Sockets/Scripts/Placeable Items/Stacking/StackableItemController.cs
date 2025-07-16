using System;
using System.Collections.Generic;
using UnityEngine;
using Hypertonic.Modules.UltimateSockets.Models;
using Hypertonic.Modules.UltimateSockets.Stacking;
using Hypertonic.Modules.UltimateSockets.Sockets;

namespace Hypertonic.Modules.UltimateSockets.PlaceableItems.Stacking
{
    public class StackableItemController : MonoBehaviour
    {
        public bool Stackable { get => _stackable; set { _stackable = value; } }
        public bool Spawning => _stackableItemSpawnTransitionController.Spawning;

        public int SpawnTransitionCount => SpawnTransitionEntries.Count;
        public List<SpawnTransitionEntry> SpawnTransitionEntries => _stackableItemSpawnTransitionController.SpawnTransitionEntries;

        [SerializeField]
        private StackableItemSpawnTransitionController _stackableItemSpawnTransitionController;


        [SerializeField]
        private bool _stackable;

        #region Public Functions

        #region Transition Controller Functions
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

        public void ActivateSpawnTransitions(Socket socket, PlaceableItem placeableItem)
        {
            _stackableItemSpawnTransitionController.Spawn(socket, placeableItem);
        }

        public void SetIsSpawning(bool isSpawning)
        {
            _stackableItemSpawnTransitionController.SetIsSpawning(isSpawning);
        }

        #endregion Transition Controller Functions

        #endregion Public Functions

        #region Editor Functions

#if UNITY_EDITOR

        public void AddSpawnTransition(string transitionName)
        {
            _stackableItemSpawnTransitionController.AddSpawnTransition(transitionName);
        }

#endif

        #endregion Editor Functions
    }
}
