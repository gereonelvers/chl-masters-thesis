using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridSize = 10; // Number of cells in one direction from the center
    public float cellSize = 1f;

    [Header("Line Settings")]
    public Material lineMaterial;
    public float lineWidth = 0.05f; // Line width

    [Header("Prefab Settings")]
    [Tooltip("Assign the prefab that the grid should follow.")]
    public Transform targetPrefab; // Reference to the prefab

    void Start()
    {
        if (targetPrefab == null)
        {
            Debug.LogError("Target Prefab is not assigned. Please assign a prefab in the inspector.");
            return;
        }

        GenerateGrid();
    }

    void GenerateGrid()
    {
        // Create a parent object for the grid as a child of the GameObject this script is attached to
        GameObject gridParent = new GameObject("Grid");
        gridParent.transform.parent = transform; // Now the grid is parented to this GameObject
        gridParent.transform.localPosition = Vector3.zero; // Reset local position
        gridParent.transform.localRotation = Quaternion.identity; // Reset local rotation
        gridParent.transform.localScale = Vector3.one; // Reset local scale

        // Calculate the total grid size in world units
        float totalGridSize = gridSize * cellSize;

        // Calculate the offset to center the grid
        float gridOffset = totalGridSize / 2f;

        for (int i = 0; i <= gridSize; i++)
        {
            // Calculate the current position with offset
            float position = i * cellSize - gridOffset;

            // Horizontal Lines (parallel to X-axis)
            GameObject hLine = new GameObject("HLine_" + i);
            hLine.transform.parent = gridParent.transform; // Parent to Grid
            LineRenderer hr = hLine.AddComponent<LineRenderer>();
            hr.material = lineMaterial;
            hr.positionCount = 2;
            // These positions are now local since we set useWorldSpace to false.
            hr.SetPosition(0, new Vector3(-gridOffset, 0, position)); // Start point
            hr.SetPosition(1, new Vector3(gridOffset, 0, position));  // End point
            hr.widthMultiplier = lineWidth;
            hr.useWorldSpace = false; // Important! Make positions relative to hLine's transform.

            // Vertical Lines (parallel to Z-axis)
            GameObject vLine = new GameObject("VLine_" + i);
            vLine.transform.parent = gridParent.transform; // Parent to Grid
            LineRenderer vr = vLine.AddComponent<LineRenderer>();
            vr.material = lineMaterial;
            vr.positionCount = 2;
            vr.SetPosition(0, new Vector3(position, 0, -gridOffset)); // Start point
            vr.SetPosition(1, new Vector3(position, 0, gridOffset));  // End point
            vr.widthMultiplier = lineWidth;
            vr.useWorldSpace = false; // Important! Make positions relative to vLine's transform.
        }

        // Add a single BoxCollider to the grid parent to cover the entire grid area
        BoxCollider gridCollider = gridParent.AddComponent<BoxCollider>();
        // The collider covers the grid in X and Z with a very thin Y dimension.
        gridCollider.size = new Vector3(totalGridSize, 0.1f, totalGridSize);
        gridCollider.center = Vector3.zero;
    }
}
