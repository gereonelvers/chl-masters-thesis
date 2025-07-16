using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class TextureTiling : MonoBehaviour
{
    [SerializeField] private Vector3 objectDimensions = new Vector3(1f, 2f, 5f);
    [SerializeField] private float textureBaseSize = 1f; // The size in units that one texture repetition should cover

    private void Start()
    {
        // Get the material from the mesh renderer
        Material material = GetComponent<MeshRenderer>().material;

        // Calculate tiling based on object dimensions and desired texture size
        Vector2 tiling = new Vector2(
            objectDimensions.x / textureBaseSize,
            objectDimensions.z / textureBaseSize  // Using Z instead of Y because Unity's UV mapping uses XZ for the main faces
        );

        // Apply the tiling to the material
        material.mainTextureScale = tiling;
    }
}