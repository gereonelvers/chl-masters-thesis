using UnityEngine;
using TMPro;
using System.Collections;
using Vuforia; // Vuforia namespace for ObserverBehaviour and tracking status

public class FloorGridAlignerMulti : MonoBehaviour
{
    [Header("Tracked Cubes (Assign in Inspector)")]
    // The cube that provides horizontal position (anchor)
    public Transform anchorCube;
    // The cube that provides the directional reference
    public Transform directionCube;

    [Header("Recalibration Settings")]
    // How long a cube must be continuously tracked to be “confirmed”
    public float recalibrationInterval = 5f;
    // Duration over which the grid smoothly interpolates to the new transform
    public float interpolationDuration = 1f;

    [Header("UI (Optional)")]
    // UI texts to display the tracking timers (using TextMeshPro)
    public TMP_Text anchorTimerText;
    public TMP_Text directionTimerText;
    // How frequently to update the UI texts (in seconds)
    public float textUpdateInterval = 0.5f;

    // Internal timers for each cube
    private float anchorTimer = 0f;
    private float directionTimer = 0f;
    private float uiTimer = 0f;

    // Flags and stored positions once each cube has been “confirmed”
    private bool anchorConfirmed = false;
    private bool directionConfirmed = false;
    private Vector3 anchorConfirmedPosition;
    private Vector3 directionConfirmedPosition;

    // Optional flag to skip the very first alignment (if desired)
    private bool firstAlignmentSkipped = false;

    // Cached ObserverBehaviour components (assumes the cubes have them)
    private ObserverBehaviour anchorObserver;
    private ObserverBehaviour directionObserver;

    void Start()
    {
        if (anchorCube != null)
        {
            anchorObserver = anchorCube.GetComponent<ObserverBehaviour>();
            if (anchorObserver == null)
            {
                Debug.LogWarning("AnchorCube does not have an ObserverBehaviour component.");
            }
        }
        if (directionCube != null)
        {
            directionObserver = directionCube.GetComponent<ObserverBehaviour>();
            if (directionObserver == null)
            {
                Debug.LogWarning("DirectionCube does not have an ObserverBehaviour component.");
            }
        }
    }

    // Helper function to check if the given observer is actually tracking its target.
    private bool IsTracked(ObserverBehaviour observer)
    {
        if (observer == null)
            return false;

        // Accept as tracked if the target status is TRACKED or EXTENDED_TRACKED.
        TargetStatus status = observer.TargetStatus;
        return status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED;
    }

    void Update()
    {
        if (anchorCube == null || directionCube == null)
        {
            Debug.LogWarning("Both cubes must be assigned in FloorGridAlignerMulti!");
            return;
        }

        // Only accumulate timer if the cube is truly tracked
        if (anchorObserver != null && IsTracked(anchorObserver))
        {
            anchorTimer += Time.deltaTime;
            if (anchorTimer >= recalibrationInterval)
            {
                anchorConfirmed = true;
                anchorConfirmedPosition = anchorCube.position;
                // Reset the timer so that if the cube remains tracked, we can reconfirm later.
                anchorTimer = 0f;
            }
        }
        else
        {
            anchorTimer = 0f;
        }

        if (directionObserver != null && IsTracked(directionObserver))
        {
            directionTimer += Time.deltaTime;
            if (directionTimer >= recalibrationInterval)
            {
                directionConfirmed = true;
                directionConfirmedPosition = directionCube.position;
                directionTimer = 0f;
            }
        }
        else
        {
            directionTimer = 0f;
        }

        // Update UI texts at a throttled interval (optional)
        uiTimer += Time.deltaTime;
        if (uiTimer >= textUpdateInterval)
        {
            if (anchorTimerText != null)
                anchorTimerText.text = "Anchor Timer: " + anchorTimer.ToString("F1") + " s";
            if (directionTimerText != null)
                directionTimerText.text = "Direction Timer: " + directionTimer.ToString("F1") + " s";
            uiTimer = 0f;
        }

        // Once both cubes have been confirmed (tracked for long enough), perform the alignment.
        if (anchorConfirmed && directionConfirmed)
        {
            if (!firstAlignmentSkipped)
            {
                firstAlignmentSkipped = true;
            }
            else
            {
                StartCoroutine(SmoothFullRecalibration(interpolationDuration));
            }

            // Reset confirmation flags so a new alignment can be triggered later.
            anchorConfirmed = false;
            directionConfirmed = false;
        }
    }

    IEnumerator SmoothFullRecalibration(float duration)
    {
        // Store the starting position and rotation of the grid.
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        // For position:
        // • Use the anchor cube’s X and Z,
        // • And use the average of the two cubes’ Y values for increased precision.
        float avgY = (anchorConfirmedPosition.y + directionConfirmedPosition.y) / 2f;
        Vector3 targetPos = new Vector3(anchorConfirmedPosition.x, avgY, anchorConfirmedPosition.z);

        // For rotation:
        // Compute a horizontal direction vector from the anchor cube to the direction cube.
        Vector3 directionVector = directionConfirmedPosition - anchorConfirmedPosition;
        directionVector.y = 0f; // ignore vertical differences
        if (directionVector == Vector3.zero)
        {
            Debug.LogWarning("Cubes are vertically aligned; cannot determine a valid direction.");
            yield break;
        }
        Quaternion targetRot = Quaternion.LookRotation(directionVector);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
            yield return null;
        }
        transform.position = targetPos;
        transform.rotation = targetRot;
        Debug.Log("Floor grid aligned using averaged height and directional vector.");
    }
}
