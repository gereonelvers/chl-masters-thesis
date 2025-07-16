using UnityEngine;

public class GridSpawner : MonoBehaviour
{
    [Tooltip("The prefab to instantiate at each grid point.")]
    public GameObject prefab;

    [Tooltip("Spacing (in world units) between spawned objects along the X axis.")]
    public float spacingX = 1f;

    [Tooltip("Spacing (in world units) between spawned objects along the Z axis.")]
    public float spacingZ = 1f;

    [Tooltip("If true, the grid is spawned when the game starts.")]
    public bool spawnOnStart = true;

    [Tooltip("Optional parent for the spawned objects. If not set, uses this GameObject.")]
    public Transform floorSnapZonesParent;

    void Start()
    {
        if (spawnOnStart)
            SpawnGrid();
    }

    // Call this method to generate the grid at runtime.
    public void SpawnGrid()
    {
        if (prefab == null)
        {
            Debug.LogError("No prefab provided!");
            return;
        }

        // Try to get bounds from either a Collider or MeshRenderer.
        Bounds bounds;
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            bounds = col.bounds;
        }
        else
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr != null)
                bounds = mr.bounds;
            else
            {
                Debug.LogError("No Collider or MeshRenderer found on the object.");
                return;
            }
        }

        // Determine the parent for spawned objects.
        Transform parentTransform = floorSnapZonesParent != null ? floorSnapZonesParent : transform;

        // Calculate min and max positions on the plane (assuming the plane is horizontal).
        float minX = bounds.min.x;
        float maxX = bounds.max.x;
        float minZ = bounds.min.z;
        float maxZ = bounds.max.z;

        // Loop over X and Z axes with the given spacing.
        for (float x = minX; x <= maxX; x += spacingX)
        {
            for (float z = minZ; z <= maxZ; z += spacingZ)
            {
                // Use the plane's Y coordinate (bounds.center.y) for placement.
                Vector3 spawnPos = new Vector3(x, bounds.center.y, z);
                // Instantiate as a child of the specified parent for organizational purposes.
                Instantiate(prefab, spawnPos, Quaternion.identity, parentTransform);
            }
        }
    }
}
