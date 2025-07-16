using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // Make sure this namespace is available

/// <summary>
/// This manager casts a ray (from the main camera’s position for testing, or via MRTK pointer events),
/// calculates a snapped position (using the given cellSize), shows a transparent preview,
/// and on user input, places a block (updating the internal stack count for that grid cell).
/// A maximum placement distance is enforced to prevent placing blocks too far away.
/// </summary>
public class BlockPlacementManager : MonoBehaviour
{
    [Header("Prefab Settings")]
    [Tooltip("The actual cube block prefab that will be placed.")]
    public GameObject blockPrefab;

    [Tooltip("A transparent version of the cube used as a preview.")]
    public GameObject blockPreviewPrefab;

    [Header("Grid Settings")]
    [Tooltip("Cell size in world units (assumed to match the cube dimensions).")]
    public float cellSize = 1.0f;

    [Tooltip("The total grid size in one direction (should match your GridGenerator).")]
    public int gridSize = 10; // e.g. Grid from -gridSize/2 to +gridSize/2

    [Header("Raycast Settings")]
    [Tooltip("Layers to raycast against (your grid collider and block colliders should be in these layers).")]
    public LayerMask placementLayerMask;

    [Header("Placement Distance")]
    [Tooltip("Maximum allowed distance from the camera (or pointer origin) for placement.")]
    public float maxPlacementDistance = 5f;

    // Keeps track of how many blocks have been placed in each grid cell.
    private Dictionary<Vector2Int, int> blockStackCounts = new Dictionary<Vector2Int, int>();

    // The preview instance that follows the pointer.
    private GameObject previewInstance;

    private void Start()
    {
        if (blockPrefab == null || blockPreviewPrefab == null)
        {
            Debug.LogError("Please assign both the blockPrefab and blockPreviewPrefab in the inspector.");
            return;
        }

        // Instantiate the preview block. It should be a transparent version.
        previewInstance = Instantiate(blockPreviewPrefab);
        previewInstance.SetActive(false);
    }

    private void Update()
    {
        // Use the new Input System for editor testing.
        // Ensure we have a main camera.
        if (Camera.main == null)
        {
            Debug.LogError("Main Camera not found.");
            return;
        }

        // Get a ray using the current mouse position from the new Input System.
        Ray ray;
        if (Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            ray = Camera.main.ScreenPointToRay(mousePos);
        }
        else
        {
            // Fallback ray if Mouse is not available.
            ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        }

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f, placementLayerMask))
        {
            // Check the distance between the camera (or pointer origin) and the hit point.
            float distance = Vector3.Distance(Camera.main.transform.position, hit.point);
            if (distance > maxPlacementDistance)
            {
                // If the hit is too far, disable the preview and do not allow placement.
                previewInstance.SetActive(false);
                return;
            }

            // Compute the snapped position based on the hit point.
            Vector3 snappedPos = GetSnappedPosition(hit.point);

            // Optionally check if the snapped position is within the intended grid boundaries.
            if (IsWithinGrid(snappedPos))
            {
                previewInstance.SetActive(true);
                previewInstance.transform.position = snappedPos;

                // Check for input that “places” the block using the new Input System.
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    PlaceBlock(snappedPos);
                }
            }
            else
            {
                previewInstance.SetActive(false);
            }
        }
        else
        {
            previewInstance.SetActive(false);
        }
    }

    /// <summary>
    /// Rounds the hit point to the nearest grid coordinates and computes the y based on the current stack height.
    /// </summary>
    Vector3 GetSnappedPosition(Vector3 hitPoint)
    {
        // Snap the X and Z positions to the nearest multiple of cellSize.
        float snappedX = Mathf.Round(hitPoint.x / cellSize) * cellSize;
        float snappedZ = Mathf.Round(hitPoint.z / cellSize) * cellSize;

        // Compute grid indices (as integers).
        int gridX = Mathf.RoundToInt(snappedX / cellSize);
        int gridZ = Mathf.RoundToInt(snappedZ / cellSize);
        Vector2Int cell = new Vector2Int(gridX, gridZ);

        // Look up the current block count for this cell (if any).
        int currentCount = 0;
        blockStackCounts.TryGetValue(cell, out currentCount);

        // Compute the y coordinate:
        // For ground placement, when no blocks are there, the cube’s center should be at cellSize/2.
        // For stacking, each additional block increases the y by cellSize.
        float snappedY = currentCount * cellSize + cellSize / 2f;

        return new Vector3(snappedX, snappedY, snappedZ);
    }

    /// <summary>
    /// Optional: Checks if the snapped position is within the grid boundaries.
    /// Adjust as needed based on how your grid is defined.
    /// </summary>
    bool IsWithinGrid(Vector3 position)
    {
        // Assuming your grid (as generated by GridGenerator) runs from -gridOffset to +gridOffset.
        float totalGridSize = gridSize * cellSize;
        float gridOffset = totalGridSize / 2f;
        if (position.x < -gridOffset || position.x > gridOffset) return false;
        if (position.z < -gridOffset || position.z > gridOffset) return false;
        return true;
    }

    /// <summary>
    /// Instantiates a block at the given snapped position and updates the cell’s block count.
    /// </summary>
    void PlaceBlock(Vector3 position)
    {
        // Instantiate the actual block (it will typically have its own collider, etc.).
        Instantiate(blockPrefab, position, Quaternion.identity);

        // Update the count for the grid cell so that the next block in this cell goes higher.
        int gridX = Mathf.RoundToInt(position.x / cellSize);
        int gridZ = Mathf.RoundToInt(position.z / cellSize);
        Vector2Int cell = new Vector2Int(gridX, gridZ);
        if (blockStackCounts.ContainsKey(cell))
            blockStackCounts[cell]++;
        else
            blockStackCounts[cell] = 1;
    }
}
