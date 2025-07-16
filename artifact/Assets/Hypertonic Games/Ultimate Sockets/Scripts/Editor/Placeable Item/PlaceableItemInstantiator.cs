using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using Hypertonic.Modules.UltimateSockets.PlaceableItems.Stacking;
using Hypertonic.Modules.UltimateSockets.Sockets.PlacementCriterias;
using Hypertonic.Modules.UltimateSockets.XR;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Editor.PlaceableItems
{
    public static class PlaceableItemInstantiator
    {

        public static void InstantiatePlaceableItemComponents(PlaceableItem placeableItem)
        {
            GameObject utilityComponent = AddPlaceableItemUtilityComponents(placeableItem);

            AddGrabCollider(placeableItem);
            AddSocketDetectorCollider(placeableItem);
            AddPlacementCriteriaContainer(placeableItem);

            AddPlaceableItemScaler(placeableItem, utilityComponent);

            AddPlaceableItemPlacementController(placeableItem, utilityComponent);

            AddPlaceableItemPreviewController(placeableItem, utilityComponent);

            AddPlaceableItemRigidbody(placeableItem, utilityComponent);

            AddPlaceableItemGrabbable(placeableItem, utilityComponent);

            AddMeshController(placeableItem, utilityComponent);

            AddStackableItemController(placeableItem, utilityComponent);
        }

        private static void AddGrabCollider(PlaceableItem placeableItem)
        {
            GameObject socketGrabColliderGameObject = new GameObject("Placeable Item | Grab Collider");
            socketGrabColliderGameObject.transform.SetParent(placeableItem.transform, false);

            SocketGrabCollider socketGrabCollider = socketGrabColliderGameObject.AddComponent<SocketGrabCollider>();
            socketGrabCollider.SetPlaceableItem(placeableItem);
            socketGrabCollider.AddColliderManager();

            placeableItem.SetGrabCollider(socketGrabCollider);
        }

        private static void AddSocketDetectorCollider(PlaceableItem placeableItem)
        {
            GameObject placeableItemColliderGameObject = new GameObject("Placeable Item | Socket Detector Collider");
            placeableItemColliderGameObject.transform.SetParent(placeableItem.transform, false);

            PlaceableItemCollider placeableItemCollider = placeableItemColliderGameObject.AddComponent<PlaceableItemCollider>();
            placeableItemCollider.SetPlaceableItem(placeableItem);
            placeableItemCollider.AddColliderManager();


            placeableItem.SetSocketDetectorCollider(placeableItemCollider);
        }

        private static GameObject AddPlaceableItemUtilityComponents(PlaceableItem placeableItem)
        {
            GameObject placeableItemUtilityComponentsGameObject = new GameObject("Placeable Item | Utility Components");

            placeableItemUtilityComponentsGameObject.transform.SetParent(placeableItem.transform, false);

            placeableItem.SetUtilityComponentContainerGameObject(placeableItemUtilityComponentsGameObject);

            return placeableItemUtilityComponentsGameObject;
        }

        private static void AddPlaceableItemScaler(PlaceableItem placeableItem, GameObject utilityGameObject)
        {
            PlaceableItemSocketScaler placeableItemSocketScaler = utilityGameObject.AddComponent<PlaceableItemSocketScaler>();
            placeableItem.SetSocketScaler(placeableItemSocketScaler);
        }

        private static void AddPlaceableItemPreviewController(PlaceableItem placeableItem, GameObject utilityGameObject)
        {
            PlaceableItemPreviewController placeableItemPreviewController = utilityGameObject.AddComponent<PlaceableItemPreviewController>();

            placeableItem.SetPlaceableItemPreviewController(placeableItemPreviewController);
        }

        private static void AddPlacementCriteriaContainer(PlaceableItem placeableItem)
        {
            GameObject placeableItemPlacementCriteriaGameObject = new GameObject("Placeable Item | Placement Criteria Container");
            placeableItemPlacementCriteriaGameObject.transform.SetParent(placeableItem.transform, false);

            PlaceableItemPlacementCriteriaController placeableItemPlacementCriteriaController = placeableItemPlacementCriteriaGameObject.AddComponent<PlaceableItemPlacementCriteriaController>();
            placeableItemPlacementCriteriaController.SetPlaceableItem(placeableItem);
            placeableItem.SetPlacementCriteriaContainer(placeableItemPlacementCriteriaController);

            AddResocketCooldownPassedCriteria(placeableItem);
        }

        private static void AddResocketCooldownPassedCriteria(PlaceableItem placeableItem)
        {
            string criteriaName = typeof(ResocketCooldownPassed).Name;

            placeableItem.PlaceableItemPlacementCriteriaController.AddPlacementCriteria(criteriaName);
        }

        private static void AddPlaceableItemPlacementController(PlaceableItem placeableItem, GameObject utilityGameObject)
        {
            PlaceableItemPlacementController placeableItemPlacementController = utilityGameObject.AddComponent<PlaceableItemPlacementController>();
            placeableItem.SetPlaceableItemPlacementController(placeableItemPlacementController);
        }

        private static void AddPlaceableItemRigidbody(PlaceableItem placeableItem, GameObject utilityGameObject)
        {
            PlaceableItemRigidbody placeableItemRigidbody = utilityGameObject.AddComponent<PlaceableItemRigidbody>();

            if (placeableItem.RootTransform.TryGetComponent(out Rigidbody rigidbody))
            {
                placeableItemRigidbody.SetRigidBody(rigidbody);
            }

            placeableItemRigidbody.SetPlaceableItem(placeableItem);

            placeableItem.SetPlaceableItemRigidBody(placeableItemRigidbody);
        }

        private static void AddPlaceableItemGrabbable(PlaceableItem placeableItem, GameObject utilityGameObject)
        {
            PlaceableItemGrabbable placeableItemGrabbable = utilityGameObject.AddComponent<PlaceableItemGrabbable>();
            placeableItemGrabbable.SetPlaceableItem(placeableItem);

            IGrabbableItem grabbableItem = placeableItem.RootTransform.GetComponent<IGrabbableItem>();

            if (grabbableItem != null)
            {
                placeableItemGrabbable.SetGrabbableItem(placeableItem.RootTransform.gameObject);
            }

            placeableItem.SetGrabbable(placeableItemGrabbable);
        }

        private static void AddMeshController(PlaceableItem placeableItem, GameObject utilityGameObject)
        {
            PlaceableItemMeshController meshController = utilityGameObject.AddComponent<PlaceableItemMeshController>();
            meshController.SetPlaceableItem(placeableItem);

            placeableItem.SetMeshController(meshController);
        }

        private static void AddStackableItemController(PlaceableItem placeableItem, GameObject utilityGameObject)
        {
            StackableItemController stackableItemController = utilityGameObject.AddComponent<StackableItemController>();
            placeableItem.SetStackableItemController(stackableItemController);

            stackableItemController.AddItemSpawnTransitionController();
        }
    }
}
