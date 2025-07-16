using UnityEngine;

public class SnapZone : MonoBehaviour
{
    // Reference to the parent cube's CubeSnapController.
    public CubeSnapController parentCube;

    [Tooltip("Direction from the snap zone's center to its active contact edge (in local space).\n" +
             "For example, for a top-face snap zone on a base cube, set this to Vector3.up; " +
             "for a bottom-face snap zone on a cube to be stacked on top, set this to Vector3.down.")]
    public Vector3 snapDirection = Vector3.up;

    private void Awake()
    {
        // Auto-assign the parent cube if not manually set.
        if (parentCube == null)
            parentCube = GetComponentInParent<CubeSnapController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        SnapZone otherZone = other.GetComponent<SnapZone>();
        if (otherZone == null)
            return;

        // Ignore zones on the same cube.
        if (otherZone.parentCube == parentCube)
            return;

        // Only add candidates if this cube is held and the other is not.
        if (parentCube != null && parentCube.IsBeingHeld() && !otherZone.parentCube.IsBeingHeld())
        {
            parentCube.AddSnapCandidate(this, otherZone);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        SnapZone otherZone = other.GetComponent<SnapZone>();
        if (otherZone == null)
            return;

        if (parentCube != null && parentCube.IsBeingHeld() && !otherZone.parentCube.IsBeingHeld())
        {
            parentCube.RemoveSnapCandidate(this, otherZone);
        }
    }

    // Draws a line in the Scene view to indicate the snap direction.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        float gizmoLength = 1f;
        Vector3 worldSnapDirection = transform.TransformDirection(snapDirection);
        Vector3 startPoint = transform.position;
        Vector3 endPoint = startPoint + worldSnapDirection * gizmoLength;
        Gizmos.DrawLine(startPoint, endPoint);
        Gizmos.DrawSphere(endPoint, 0.05f);
    }
}
