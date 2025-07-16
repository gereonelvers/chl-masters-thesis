using UnityEngine;

public class PhysicsObjectCorrector : MonoBehaviour
{
    [Tooltip("Reference to the plane that is being tracked. This should be assigned on spawn.")]
    public Transform planeTransform;

    private Vector3 lastPlanePosition;
    private Rigidbody rb;

    void Start()
    {
        if (planeTransform == null)
        {
            Debug.LogError("Plane transform not assigned in PhysicsObjectCorrector!");
            enabled = false;
            return;
        }

        lastPlanePosition = planeTransform.position;
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("No Rigidbody attached to " + gameObject.name);
            enabled = false;
        }
    }

    void FixedUpdate()
    {
        // Calculate the plane's movement delta.
        Vector3 delta = planeTransform.position - lastPlanePosition;
        if (delta != Vector3.zero)
        {
            // Use MovePosition so physics knows about the movement.
            rb.MovePosition(rb.position + delta);
            Debug.Log($"Applied plane delta {delta} to {gameObject.name}");
        }
        lastPlanePosition = planeTransform.position;
    }
}
