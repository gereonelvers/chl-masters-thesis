using Hypertonic.Modules.UltimateSockets.Highlighters;
using Hypertonic.Modules.UltimateSockets.Sockets;
using Hypertonic.Modules.UltimateSockets.Sockets.Audio;
using Hypertonic.Modules.UltimateSockets.Sockets.Stacking;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Editor.Sockets
{
    public static class SocketInstantiator
    {
        public static void InstantiateSocketOptions(Socket socket)
        {
            CreateSocketPlacedTransform(socket);
            CreatePlaceableAreaCollider(socket);
            CreateHighlightAreaCollider(socket);
            CreateHighlightManager(socket);
            CreatePlacementCriteriaManager(socket);
            CreateStackableItemController(socket);
            CreateAudioController(socket);
        }

        private static void CreateSocketPlacedTransform(Socket socket)
        {
            GameObject placedTransformGameObject = new GameObject("Socket | Placed Transform");
            placedTransformGameObject.transform.SetParent(socket.transform, false);
            SocketPlaceTransform socketPlaceTransform = placedTransformGameObject.AddComponent<SocketPlaceTransform>();
            socketPlaceTransform.SetSocket(socket);
            socket.SetSocketPlacementTransform(socketPlaceTransform);
        }

        private static void CreatePlaceableAreaCollider(Socket socket)
        {
            GameObject socketColliderGameObject = new GameObject("Socket | Placeable Area Collider");
            socketColliderGameObject.transform.SetParent(socket.transform, false);

            SocketPlaceCollider placecCollider = socketColliderGameObject.AddComponent<SocketPlaceCollider>();
            placecCollider.SetSocket(socket);
            placecCollider.AddColliderManager();

            socket.SetSocketPlaceCollider(placecCollider);
        }

        private static void CreateHighlightManager(Socket socket)
        {
            GameObject socketHighlighterGameObject = new GameObject("Socket | Socket Highlighter Manager");
            socketHighlighterGameObject.transform.SetParent(socket.transform, false);
            SocketHighlighter socketHighlighter = socketHighlighterGameObject.AddComponent<SocketHighlighter>();
            socket.SetSocketHighlighter(socketHighlighter);
            socketHighlighter.SetSocketHighlightAreaCollider(socket.SocketHighlightAreaCollider);
            socketHighlighter.SetSocket(socket);
        }

        private static void CreateHighlightAreaCollider(Socket socket)
        {
            GameObject socketHighlightAreaColliderGameObject = new GameObject("Socket | Highlight Area Collider");
            socketHighlightAreaColliderGameObject.transform.SetParent(socket.transform, false);

            SocketHighlightAreaCollider highlightAreaColliderComponent = socketHighlightAreaColliderGameObject.AddComponent<SocketHighlightAreaCollider>();
            highlightAreaColliderComponent.SetSocket(socket);
            highlightAreaColliderComponent.AddColliderManager();

            socket.SetSocketHighlightAreaCollider(highlightAreaColliderComponent);
        }

        private static void CreatePlacementCriteriaManager(Socket socket)
        {
            GameObject socketPlacementCriteriaGameObject = new GameObject("Socket | Socket Placement Criteria Container");
            socketPlacementCriteriaGameObject.transform.SetParent(socket.transform, false);
            SocketPlacementCriteriaController socketPlacementCriteriaController = socketPlacementCriteriaGameObject.AddComponent<SocketPlacementCriteriaController>();
            socketPlacementCriteriaController.SetSocket(socket);

            socket.SetPlacementCriteriaContainer(socketPlacementCriteriaController);
        }

        private static void CreateStackableItemController(Socket socket)
        {
            GameObject stackableItemControllerGameObject = new GameObject("Socket | Stackable Item Controller");
            stackableItemControllerGameObject.transform.SetParent(socket.transform, false);

            SocketStackableItemController stackableItemController = stackableItemControllerGameObject.AddComponent<SocketStackableItemController>();
            stackableItemController.SetSocket(socket);
            stackableItemController.AddItemSpawnTransitionController();

            socket.SetStackableItemController(stackableItemController);
        }

        private static void CreateAudioController(Socket socket)
        {
            GameObject audioControllerGameObject = new GameObject("Socket | Audio Controller");
            audioControllerGameObject.transform.SetParent(socket.transform, false);

            SocketAudioController audioController = audioControllerGameObject.AddComponent<SocketAudioController>();
            audioController.SetSocket(socket);
            audioController.LoadDefaultSoundClips();

            socket.SetAudioController(audioController);
        }
    }
}
