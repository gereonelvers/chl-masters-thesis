using UnityEngine;

public class CollisionMaterialIndicator : MonoBehaviour
{
    // Time in seconds to show the material after collision.
    public float displayTime = 1f;

    // Additional GameObject whose visibility should match.
    public GameObject additionalGameObject;

    // Cached reference to the MeshRenderer on the current GameObject.
    private MeshRenderer meshRenderer;

    // Cached reference to the MeshRenderer on the additional GameObject.
    private MeshRenderer additionalMeshRenderer;

    void Start()
    {
        // Cache and disable the MeshRenderer on this GameObject.
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        // If an additional GameObject is provided, cache its MeshRenderer and disable it.
        if (additionalGameObject != null)
        {
            additionalMeshRenderer = additionalGameObject.GetComponent<MeshRenderer>();
            if (additionalMeshRenderer != null)
            {
                additionalMeshRenderer.enabled = false;
            }
        }
    }

    // Alternatively, if you prefer using physical collisions, comment out the above method
    // and uncomment the method below.
    private void OnCollisionEnter(Collision collision)
    {
        ShowMaterial();
    }

    private void ShowMaterial()
    {
        // Enable the renderer on the current GameObject.
        if (meshRenderer != null)
        {
            meshRenderer.enabled = true;
        }

        // Enable the renderer on the additional GameObject if available.
        if (additionalMeshRenderer != null)
        {
            additionalMeshRenderer.enabled = true;
        }

        // Cancel any pending hide requests to avoid multiple Invoke calls.
        CancelInvoke("HideMaterial");
        // Schedule the material to be hidden after displayTime.
        Invoke("HideMaterial", displayTime);
    }

    private void HideMaterial()
    {
        // Disable the renderer on the current GameObject.
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        // Disable the renderer on the additional GameObject if available.
        if (additionalMeshRenderer != null)
        {
            additionalMeshRenderer.enabled = false;
        }
    }
}
