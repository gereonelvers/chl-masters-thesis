using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class XRGridSnapper : MonoBehaviour
{
    [Header("Grid & Cube Settings")]
    [Tooltip("Size of one grid cell (1 unit = 10cm if using your scale)")]
    public float cellSize = 1f;
    [Tooltip("Cube height in grid units (should be 2 for a 20cm cube if 1 unit = 10cm)")]
    public float cubeHeight = 2f;

    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            Debug.LogError("XRGrabInteractable component is missing on " + gameObject.name);
        }
    }

    private void OnEnable()
    {
        // Subscribe to drop events.
        grabInteractable.selectExited.AddListener(OnSelectExited);
    }

    private void OnDisable()
    {
        grabInteractable.selectExited.RemoveListener(OnSelectExited);
    }

    // When you drop the cube, snap its position.
    private void OnSelectExited(SelectExitEventArgs args)
    {
        // Compute the snapped position from the cube's current position.
        Vector3 snappedPosition = SnapToGrid(transform.position);
        transform.position = snappedPosition;
        Debug.Log($"{gameObject.name}: Dropped and snapped to {snappedPosition}");
    }

    // Rounds X and Z to the nearest cell, then figures out Y via a downward raycast.
    private Vector3 SnapToGrid(Vector3 pos)
    {
        float snappedX = Mathf.Round(pos.x / cellSize) * cellSize;
        float snappedZ = Mathf.Round(pos.z / cellSize) * cellSize;
        float snappedY = GetSnappedY(new Vector3(snappedX, pos.y, snappedZ));
        return new Vector3(snappedX, snappedY, snappedZ);
    }

    // This method casts a ray downward from above the snapped X/Z location to find the grid’s collider.
    private float GetSnappedY(Vector3 pos)
    {
        RaycastHit hit;
        // Start the ray from 1 unit above the target position.
        Vector3 rayOrigin = pos + Vector3.up * 1f;
        float maxDistance = 10f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, maxDistance))
        {
            // Assume the grid is at hit.point.y.
            // Adjust by half the cube height so that the cube's bottom sits on the grid.
            return hit.point.y + cubeHeight / 2f;
        }
        // If nothing was hit, default to having the cube rest with its bottom at y = 0.
        return cubeHeight / 2f;
    }
}
