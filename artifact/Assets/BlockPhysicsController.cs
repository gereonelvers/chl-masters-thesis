using UnityEngine;
using System.Collections;
using System.Globalization;

[RequireComponent(typeof(Rigidbody))]
public class BlockPhysicsController : MonoBehaviour
{
    [Header("Settings")]
    public float supportCheckMargin = 0.1f; // Margin for raycast support check

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.useGravity = true;
        rb.isKinematic = false;

        Log("Awake - RigidBody configured.");
    }

    public void OnReleased()
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        if (GridManager.Instance != null)
        {
            Vector3 targetPos = GridManager.Instance.SnapToGrid(transform.position);
            Quaternion targetRot = GridManager.Instance.SnapRotation(transform.rotation);
            Log($"Smooth snap: {startPos} -> {targetPos}");

            // Disable physics to smoothly transition without interference.
            rb.useGravity = false;
            rb.isKinematic = true;

            StartCoroutine(SmoothSnapCoroutine(startPos, targetPos, startRot, targetRot));
        }
        else
        {
            rb.useGravity = true;
            rb.WakeUp();
        }
    }

    private IEnumerator SmoothSnapCoroutine(Vector3 startPos, Vector3 endPos, Quaternion startRot, Quaternion endRot)
    {
        float duration = 0.1f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(startPos, endPos, t);
            transform.rotation = Quaternion.Lerp(startRot, endRot, t);
            yield return null;
        }
        // Ensure final position and rotation are exactly at the snapped values.
        transform.position = endPos;
        transform.rotation = endRot;

        // Re-enable physics.
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.WakeUp();

        // Call ReleaseOwnership() from the OwnershipController.
        OwnershipController ownership = GetComponent<OwnershipController>();
        if (ownership != null)
        {
            ownership.ReleaseOwnership();
        }
    }

    // Helper method for consistent logging with a prefix.
    private void Log(string message)
    {
        NetworkedLogger.Instance.Log("BlockPhysicsController [" + gameObject.name + "]: " + message);
    }
}
