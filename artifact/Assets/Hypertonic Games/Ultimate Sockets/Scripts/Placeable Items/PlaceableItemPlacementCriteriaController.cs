using System;
using System.Collections.Generic;
using System.Linq;
using Hypertonic.Modules.UltimateSockets.Interfaces;
using Hypertonic.Modules.UltimateSockets.Models;
using UnityEditor;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.PlaceableItems
{
    public class PlaceableItemPlacementCriteriaController : MonoBehaviour
    {
        public List<CriteriaEntry> Criterias => _criterias;

        [SerializeField]
        private PlaceableItem _placeableItem;

        [SerializeField]
        private List<CriteriaEntry> _criterias = new List<CriteriaEntry>();

        public bool CanPlace(List<string> ignoreCriteriaNames = null)
        {
            if (_criterias.Count == 0)
                return true;

            foreach (CriteriaEntry criteriaEntry in _criterias)
            {
                IPlaceableItemPlacementCriteria criteria = (IPlaceableItemPlacementCriteria)criteriaEntry.CriteriaComponent;

                if (!criteria.UseCriteria())
                {
                    continue;
                }

                if (!criteria.CanPlace())
                {
                    if (ignoreCriteriaNames != null && ignoreCriteriaNames.Contains(criteriaEntry.CriteriaName))
                    {
                        continue;
                    }

                    return false;
                }
            }

            return true;
        }

        public bool CanHighlight()
        {
            if (_criterias.Count == 0)
                return true;

            foreach (CriteriaEntry criteriaEntry in _criterias)
            {
                IPlaceableItemPlacementCriteria criteria = (IPlaceableItemPlacementCriteria)criteriaEntry.CriteriaComponent;

                if (!criteria.CanPlace() && criteria.PreventHighlight())
                {
                    return false;
                }
            }

            return true;
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
            Debug.Log("Removed: " + criteriaName);
        }

        public bool HasCriteria(string criteriaName)
        {
            return _criterias.Any(entry => entry.CriteriaName == criteriaName);
        }

        #region Editor Functions

        public void SetPlaceableItem(PlaceableItem placeableItem)
        {
            _placeableItem = placeableItem;
        }

#if UNITY_EDITOR


        public void AddPlacementCriteria(string criteriaName)
        {
            if (_criterias.Any(entry => entry.CriteriaName == criteriaName))
            {
                Debug.LogErrorFormat(this, "Criteria with name [{0}] already exists", criteriaName);
                return;
            }

            Type criteriaType = TypeCache.GetTypesDerivedFrom<IPlaceableItemPlacementCriteria>()
                               .FirstOrDefault(type => type.Name == criteriaName);

            if (criteriaType == null)
            {
                Debug.LogErrorFormat(this, "Could not find criteria type: [{0}]", criteriaName);
                return;
            }

            Component criteriaComponent = gameObject.AddComponent(criteriaType);
            CriteriaEntry entry = new CriteriaEntry { CriteriaName = criteriaName, CriteriaComponent = criteriaComponent as MonoBehaviour };
            _criterias.Add(entry);

            ((IPlaceableItemPlacementCriteria)criteriaComponent).Setup(_placeableItem);

            PrefabUtility.RecordPrefabInstancePropertyModifications(this);

            Debug.Log("Added: " + criteriaName);
        }
#endif

        #endregion Editor Functions
    }
}
