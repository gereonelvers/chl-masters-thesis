using System;
using System.Collections.Generic;
using System.Linq;
using Hypertonic.Modules.UltimateSockets.Models;
using UnityEngine;
using Hypertonic.Modules.UltimateSockets.Interfaces;
using Hypertonic.Modules.UltimateSockets.PlaceableItems;



#if UNITY_EDITOR

using UnityEditor;

#endif

namespace Hypertonic.Modules.UltimateSockets.Sockets
{
    public class SocketPlacementCriteriaController : MonoBehaviour
    {
        public List<CriteriaEntry> Criterias => _criterias;

        [SerializeField]
        private Socket _socket;

        [SerializeField]
        private List<CriteriaEntry> _criterias = new List<CriteriaEntry>();

        public bool CanPlace(PlaceableItem placeableItem, List<string> placementCriteriasToIgnore = null)
        {
            if (Criterias.Count == 0)
                return true;

            foreach (CriteriaEntry criteriaEntry in _criterias)
            {
                if (!((ISocketPlacementCriteria)criteriaEntry.CriteriaComponent).UseCriteria())
                {
                    continue;
                }

                if (placementCriteriasToIgnore != null && placementCriteriasToIgnore.Contains(criteriaEntry.CriteriaName))
                {
                    continue;
                }

                if (!((ISocketPlacementCriteria)criteriaEntry.CriteriaComponent).CanPlace(placeableItem))
                {
                    return false;
                }
            }

            return true;
        }

        public bool CanHighlight(PlaceableItem placeableItem)
        {
            if (Criterias.Count == 0)
                return true;

            foreach (CriteriaEntry criteriaEntry in _criterias)
            {
                ISocketPlacementCriteria criteria = (ISocketPlacementCriteria)criteriaEntry.CriteriaComponent;

                if (!criteria.UseCriteria())
                {
                    continue;
                }

                if (!criteria.CanPlace(placeableItem) && criteria.PreventHighlight())
                {
                    return false;
                }
            }

            return true;
        }

#if UNITY_EDITOR
        public void AddPlacementCriteria(string criteriaName)
        {
            if (_criterias.Any(entry => entry.CriteriaName == criteriaName))
            {
                Debug.LogErrorFormat(this, "Criteria with name [{0}] already exists", criteriaName);
                return;
            }

            Type criteriaType = TypeCache.GetTypesDerivedFrom<ISocketPlacementCriteria>()
                               .FirstOrDefault(type => type.Name == criteriaName);

            if (criteriaType == null)
            {
                Debug.LogErrorFormat(this, "Could not find criteria type: [{0}]", criteriaName);
                return;
            }

            AddPlacementCriteria(criteriaName, criteriaType);

            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }
#endif 

        public void AddPlacementCriteria(string criteriaName, Type criteriaType)
        {
            Component criteriaComponent = gameObject.AddComponent(criteriaType);
            CriteriaEntry entry = new CriteriaEntry { CriteriaName = criteriaName, CriteriaComponent = criteriaComponent as MonoBehaviour };
            _criterias.Add(entry);

            ((ISocketPlacementCriteria)criteriaComponent).Setup(_socket);

            Debug.Log("Added: " + criteriaName);
        }


        public void RemovePlacementCriteria(string criteriaName)
        {
            CriteriaEntry entry = _criterias.FirstOrDefault(e => e.CriteriaName == criteriaName);

            if (entry == null)
            {
                Debug.LogErrorFormat(this, "Placement Criteria with name [{0}] does not exist", criteriaName);
                return;
            }

            _criterias.Remove(entry);
            DestroyImmediate(entry.CriteriaComponent as MonoBehaviour);

#if UNITY_EDITOR
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
#endif
        }

        public bool HasCriteria(string criteriaName)
        {
            return _criterias.Any(entry => entry.CriteriaName == criteriaName);
        }

        #region Editor Functions

        public void SetSocket(Socket socket)
        {
            _socket = socket;
        }

        #endregion Editor Functions
    }
}
