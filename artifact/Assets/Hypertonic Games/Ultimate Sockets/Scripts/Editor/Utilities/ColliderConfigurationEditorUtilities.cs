using Hypertonic.Modules.UltimateSockets.Enums;
using Hypertonic.Modules.UltimateSockets.Interfaces;
using UnityEditor;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Editor
{
    public static class ColliderConfigurationEditorUtilities
    {
        private static string[] _colliderTabsTypes = new string[] { "Sphere Collider", "Box Collider", "Mesh Collider" };

        public static void DrawColliderSettingsRegion(string regionTitle, ColliderManager colliderManager, ref bool adjustDetectionCollider)
        {
            Collider collider = colliderManager.Collider;

            EditorLayoutUtilities.DrawTopOfSection(regionTitle);

            EditorGUILayout.LabelField("Collider Settings", EditorStyles.boldLabel);

            DrawDisableOnStartRegion(colliderManager);
            DrawColliderTypeTabs(colliderManager, ref adjustDetectionCollider);


            if (collider != null)
            {
                DrawEditPositionButton(colliderManager);
            }

            if (collider != null)
            {
                EditorGUILayout.Space();
                DrawGoToColliderGameObjectButton(collider, ref adjustDetectionCollider);
                EditorGUILayout.Space();
            }

            EditorLayoutUtilities.DrawBottomOfSection();
        }

        public static void DrawDisableOnStartRegion(ColliderManager colliderManager)
        {
            EditorGUILayout.Space();
            colliderManager.DisableOnStart = EditorGUILayout.Toggle("Disable On Start", colliderManager.DisableOnStart);
        }

        public static void DrawColliderTypeTabs(IColliderManager colliderManager, ref bool adjustCollider)
        {
            EditorGUILayout.BeginVertical();

            int oldColliderType = colliderManager.GetColliderTypeIndex();

            int newColliderType = GUILayout.Toolbar(colliderManager.GetColliderTypeIndex(), _colliderTabsTypes);

            if (newColliderType != oldColliderType)
            {
                HandleColliderChanged(newColliderType, colliderManager, ref adjustCollider);
            }

            EditorGUILayout.EndVertical();
        }

        public static void DrawGoToColliderGameObjectButton(Collider collider, ref bool adjustCollider)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Go To Collider", GUILayout.Height(EditorGUIUtility.singleLineHeight * 1.5f), GUILayout.Width(220f)))
            {
                if (_selectedObject != null)
                {
                    StopPositioningCollider();
                }

                adjustCollider = false;
                Selection.activeGameObject = collider.gameObject;
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }


        public static void HandleColliderChanged(int colliderType, IColliderManager colliderManager, ref bool adjustCollider)
        {
            colliderManager.RemoveColliders();

            switch (colliderType)
            {
                case (int)ColliderType.SPHERE:
                    colliderManager.AddSphereCollider();
                    break;
                case (int)ColliderType.BOX:
                    colliderManager.AddBoxCollider();
                    break;
                case (int)ColliderType.MESH:
                    colliderManager.AddMeshCollider();
                    break;
                default:
                    Debug.LogError("Unhandled collider type. Type: " + colliderType);
                    break;
            }

            RefreshColliderGizmos(colliderManager);
        }

        public static void DrawAdjustColliderDetectionButtons(ref bool adjustDetectionCollider)
        {
            if (!adjustDetectionCollider && GUILayout.Button("Edit Collider Bounding Volume"))
            {
                adjustDetectionCollider = true;
                SceneView.RepaintAll();
            }
            else if (adjustDetectionCollider && GUILayout.Button("Stop Editing Collider Bounding Volume"))
            {
                adjustDetectionCollider = false;
                SceneView.RepaintAll();
            }
        }

        public static void DrawColliderInputFields(Collider collider)
        {
            if (collider is BoxCollider)
            {
                BoxCollider boxCollider = (BoxCollider)collider;
                boxCollider.size = EditorGUILayout.Vector3Field("Size", boxCollider.size);
                boxCollider.center = EditorGUILayout.Vector3Field("Center", boxCollider.center);
            }
            else if (collider is SphereCollider)
            {
                SphereCollider sphereCollider = (SphereCollider)collider;
                sphereCollider.radius = EditorGUILayout.FloatField("Radius", sphereCollider.radius);
                sphereCollider.center = EditorGUILayout.Vector3Field("Center", sphereCollider.center);
            }
        }

        public static void AdjustItemDetectionCollider(Collider detectionCollider, System.Action repaint)
        {
            if (detectionCollider is BoxCollider)
            {
                AdjustDetectionBoxCollider((BoxCollider)detectionCollider, repaint);
            }
            else if (detectionCollider is SphereCollider)
            {
                AdjustDetectionSphereCollider((SphereCollider)detectionCollider, repaint);
            }
        }

        public static void AdjustDetectionBoxCollider(BoxCollider boxCollider, System.Action repaint)
        {
            Vector3 center = boxCollider.transform.TransformPoint(boxCollider.center);
            Vector3 size = Vector3.Scale(boxCollider.size, boxCollider.transform.lossyScale);

            EditorGUI.BeginChangeCheck();

            // Draw handles for each face of the box collider
            float handleSize = HandleUtility.GetHandleSize(center) * 0.2f;

            Vector3 rightHandle = Handles.Slider(center + boxCollider.transform.right * size.x * 0.5f, boxCollider.transform.right, handleSize, Handles.CubeHandleCap, 0);
            Vector3 leftHandle = Handles.Slider(center - boxCollider.transform.right * size.x * 0.5f, -boxCollider.transform.right, handleSize, Handles.CubeHandleCap, 0);
            Vector3 topHandle = Handles.Slider(center + boxCollider.transform.up * size.y * 0.5f, boxCollider.transform.up, handleSize, Handles.CubeHandleCap, 0);
            Vector3 bottomHandle = Handles.Slider(center - boxCollider.transform.up * size.y * 0.5f, -boxCollider.transform.up, handleSize, Handles.CubeHandleCap, 0);
            Vector3 forwardHandle = Handles.Slider(center + boxCollider.transform.forward * size.z * 0.5f, boxCollider.transform.forward, handleSize, Handles.CubeHandleCap, 0);
            Vector3 backHandle = Handles.Slider(center - boxCollider.transform.forward * size.z * 0.5f, -boxCollider.transform.forward, handleSize, Handles.CubeHandleCap, 0);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(boxCollider, "Modify Box Collider");

                // Calculate the new size based on the handle positions
                Vector3 newSize = size;
                newSize.x = Vector3.Distance(leftHandle, rightHandle);
                newSize.y = Vector3.Distance(bottomHandle, topHandle);
                newSize.z = Vector3.Distance(backHandle, forwardHandle);

                // Calculate the delta size
                Vector3 deltaSize = newSize - size;

                // Calculate the new center position
                Vector3 newCenter = center;

                // Adjust the center position based on the handle movement
                if (rightHandle != center + boxCollider.transform.right * size.x * 0.5f)
                    newCenter += boxCollider.transform.right * deltaSize.x * 0.5f;
                if (leftHandle != center - boxCollider.transform.right * size.x * 0.5f)
                    newCenter -= boxCollider.transform.right * deltaSize.x * 0.5f;
                if (topHandle != center + boxCollider.transform.up * size.y * 0.5f)
                    newCenter += boxCollider.transform.up * deltaSize.y * 0.5f;
                if (bottomHandle != center - boxCollider.transform.up * size.y * 0.5f)
                    newCenter -= boxCollider.transform.up * deltaSize.y * 0.5f;
                if (forwardHandle != center + boxCollider.transform.forward * size.z * 0.5f)
                    newCenter += boxCollider.transform.forward * deltaSize.z * 0.5f;
                if (backHandle != center - boxCollider.transform.forward * size.z * 0.5f)
                    newCenter -= boxCollider.transform.forward * deltaSize.z * 0.5f;

                // Apply the new size and center position
                boxCollider.size = Vector3.Scale(newSize, new Vector3(1f / boxCollider.transform.lossyScale.x, 1f / boxCollider.transform.lossyScale.y, 1f / boxCollider.transform.lossyScale.z));
                boxCollider.center = boxCollider.transform.InverseTransformPoint(newCenter);

                //Repaint();
                repaint?.Invoke();
            }
        }

        public static void AdjustDetectionSphereCollider(SphereCollider sphereCollider, System.Action repaint)
        {
            Vector3 center = sphereCollider.transform.TransformPoint(sphereCollider.center);
            float radius = sphereCollider.radius * sphereCollider.transform.lossyScale.x;

            EditorGUI.BeginChangeCheck();

            // Draw handles for the sphere collider
            float handleSize = HandleUtility.GetHandleSize(center) * 0.1f;

            Vector3[] handleDirections = new Vector3[]
            {
                sphereCollider.transform.right,
                -sphereCollider.transform.right,
                sphereCollider.transform.up,
                -sphereCollider.transform.up,
                sphereCollider.transform.forward,
                -sphereCollider.transform.forward
            };

            Vector3[] handlePositions = new Vector3[handleDirections.Length];

            for (int i = 0; i < handleDirections.Length; i++)
            {
                handlePositions[i] = center + handleDirections[i] * radius;
                handlePositions[i] = Handles.Slider(handlePositions[i], handleDirections[i], handleSize, Handles.SphereHandleCap, 0);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sphereCollider, "Modify Sphere Collider");

                // Calculate the new radius based on the handle positions
                float newRadius = radius;

                for (int i = 0; i < handlePositions.Length; i++)
                {
                    float handleDistance = Vector3.Distance(center, handlePositions[i]);
                    float deltaRadius = handleDistance - radius;

                    if (Mathf.Abs(deltaRadius) > Mathf.Abs(newRadius - radius))
                    {
                        newRadius = Mathf.Max(0f, radius + deltaRadius);
                    }
                }

                // Apply the new radius
                sphereCollider.radius = newRadius / sphereCollider.transform.lossyScale.x;

                //Repaint();
                repaint?.Invoke();
            }
        }


        private static GameObject _selectedObject;

        private static void DrawEditPositionButton(ColliderManager colliderManager)
        {
            if (_selectedObject == null && GUILayout.Button("Position Collider"))
            {
                _selectedObject = Selection.activeGameObject;

                ActiveEditorTracker.sharedTracker.isLocked = true;
                Selection.activeGameObject = colliderManager.gameObject;
                Tools.pivotMode = PivotMode.Center;
            }

            if (_selectedObject != null && GUILayout.Button("Cancel"))
            {
                StopPositioningCollider();
            }
        }

        private static void StopPositioningCollider()
        {
            Selection.activeGameObject = _selectedObject;
            _selectedObject = null;
            ActiveEditorTracker.sharedTracker.isLocked = false;
        }

        private static void RefreshColliderGizmos(IColliderManager colliderManager)
        {
            GameObject currentSelectedObject = Selection.activeGameObject;

            ActiveEditorTracker.sharedTracker.isLocked = true;
            Selection.activeGameObject = ((MonoBehaviour)colliderManager).gameObject;

            EditorApplication.delayCall += () =>
            {
                Selection.activeGameObject = currentSelectedObject;
                ActiveEditorTracker.sharedTracker.isLocked = false;
            };

        }
    }
}
