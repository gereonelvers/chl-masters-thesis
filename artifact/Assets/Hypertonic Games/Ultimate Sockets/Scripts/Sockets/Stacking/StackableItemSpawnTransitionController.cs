using System;
using System.Collections.Generic;
using System.Linq;
using Hypertonic.Modules.UltimateSockets.Models;
using Hypertonic.Modules.UltimateSockets.PlaceableItems.Stacking;
using UnityEngine;
using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using Hypertonic.Modules.UltimateSockets.Sockets;
using System.Collections;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Hypertonic.Modules.UltimateSockets.Stacking
{
    public class StackableItemSpawnTransitionController : MonoBehaviour
    {
        public bool Spawning { get; private set; }

        public List<SpawnTransitionEntry> SpawnTransitionEntries => _spawnTransitionEntries;

        [SerializeField]
        private List<SpawnTransitionEntry> _spawnTransitionEntries = new List<SpawnTransitionEntry>();

        /// <summary>
        /// Holds the spawn transitions for the placeable item AND the spawn transitions set on the socket
        /// </summary>
        [SerializeField]
        private List<SpawnTransitionEntry> _combinedSpawnTransitionEntries = new List<SpawnTransitionEntry>();

        private Coroutine _checkSpawningStateCoroutine;

        public void SetIsSpawning(bool isSpawning)
        {
            Spawning = isSpawning;
        }

        public void Spawn(Socket socket, PlaceableItem placeableItem)
        {
            UpdateCombinedSpawnTransitionEntries(socket);

            Spawning = true;

            if (_combinedSpawnTransitionEntries.Count == 0)
            {
                StartCoroutine(NoSpawnTransitionsCoroutine());
                return;
            }

            foreach (SpawnTransitionEntry spawnTransition in _combinedSpawnTransitionEntries)
            {
                ((IStackSpawnTransition)spawnTransition.TransitionComponent).Spawn(socket, placeableItem);
            }

            if (_checkSpawningStateCoroutine != null)
            {
                StopCoroutine(_checkSpawningStateCoroutine);
            }

            _checkSpawningStateCoroutine = StartCoroutine(CheckSpawningStateCoroutine());
        }

        public void AddSpawnTransition(string transitionName, Type transitionType)
        {
            Component criteriaComponent = gameObject.AddComponent(transitionType);
            SpawnTransitionEntry entry = new SpawnTransitionEntry { TransitionName = transitionName, TransitionComponent = criteriaComponent as MonoBehaviour };
            _spawnTransitionEntries.Add(entry);

#if UNITY_EDITOR
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
#endif
            Debug.Log("Added: " + transitionName);
        }

        public void RemoveSpawnTransition(string transitionName)
        {
            SpawnTransitionEntry entry = _spawnTransitionEntries.Find(e => e.TransitionName == transitionName);
            if (entry == null)
            {
                Debug.LogErrorFormat(this, "Transition with name [{0}] does not exist", transitionName);
                return;
            }

            _spawnTransitionEntries.Remove(entry);
            DestroyImmediate(entry.TransitionComponent as MonoBehaviour);


#if UNITY_EDITOR
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
#endif
        }

        public bool HasSpawnTransition(string transitionName)
        {
            return _spawnTransitionEntries.Any(entry => entry.TransitionName == transitionName);
        }

        private IEnumerator CheckSpawningStateCoroutine()
        {
            while (Spawning)
            {
                bool finishedSpawning = true;

                foreach (SpawnTransitionEntry spawnTransition in _combinedSpawnTransitionEntries)
                {
                    IStackSpawnTransition ISpawnTransition = (IStackSpawnTransition)spawnTransition.TransitionComponent;

                    if (ISpawnTransition.IsSpawning())
                    {
                        finishedSpawning = false;
                        break;
                    }
                }

                Spawning = !finishedSpawning;

                yield return null;
            }
        }

        private IEnumerator NoSpawnTransitionsCoroutine()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            Spawning = false;
        }

        private void UpdateCombinedSpawnTransitionEntries(Socket socket)
        {
            _combinedSpawnTransitionEntries.Clear();
            _combinedSpawnTransitionEntries.AddRange(_spawnTransitionEntries);

            foreach (SpawnTransitionEntry spawnTransitionEntry in socket.StackableItemController.SpawnTransitionEntries)
            {
                if (!_combinedSpawnTransitionEntries.Any(entry => entry.TransitionName == spawnTransitionEntry.TransitionName))
                {
                    _combinedSpawnTransitionEntries.Add(spawnTransitionEntry);
                }
            }
        }

        #region Editor Functions
#if UNITY_EDITOR

        public void AddSpawnTransition(string transitionName)
        {
            if (_spawnTransitionEntries.Any(entry => entry.TransitionName == transitionName))
            {
                Debug.LogErrorFormat(this, "Transition with name [{0}] already exists", transitionName);
                return;
            }

            Type transitionType = TypeCache.GetTypesDerivedFrom<IStackSpawnTransition>()
                .FirstOrDefault(type => type.Name == transitionName);

            if (transitionType == null)
            {
                Debug.LogErrorFormat(this, "Could not find spawn transition type: [{0}]", transitionName);
                return;
            }

            AddSpawnTransition(transitionName, transitionType);
        }

#endif
        #endregion Editor Functions
    }
}
