using UnityEngine;
using Vuforia;
using TMPro; // Needed for TextMesh Pro
using System.Collections;
using System.Collections.Generic; // Needed for List<>

[System.Serializable]
public class CustomTrackingTargetData
{
    [Tooltip("The tracked object with an ObserverBehaviour (e.g., from Vuforia)")]
    public GameObject targetObject;

    [Tooltip("The intended world position for this target (ignored, now assumed to be on the floor)")]
    public Vector3 intendedPosition;

    [Tooltip("Optional GameObject containing a TextMesh Pro component for showing the countdown timer")]
    public GameObject countdownTextObject;

    // Cached TMP_Text component for countdown display.
    [System.NonSerialized] public TMP_Text countdownTMPText;

    [Tooltip("Optional GameObject for displaying the checkmark when the target is locked.")]
    public GameObject checkmarkObject;

    // Internal fields used at runtime.
    [System.NonSerialized] public float trackingTimer = 0f;
    [System.NonSerialized] public ObserverBehaviour observer;

    // For sequential scanning: once locked, we store the detected position.
    [System.NonSerialized] public bool locked = false;
    [System.NonSerialized] public Vector3 lockedPosition;
}

public class TrackingAlignmentManager : MonoBehaviour
{
    [Header("Tracking Targets")]
    [Tooltip("List of targets to track and calibrate. For three-marker calibration, provide three targets.")]
    public CustomTrackingTargetData[] trackingTargets;

    [Header("Tracking Settings")]
    [Tooltip("Time (in seconds) each target must be continuously tracked before locking in its position.")]
    public float trackingTimeThreshold = 5f;

    [Header("Countdown Settings")]
    [Tooltip("Countdown update interval (in seconds).")]
    public float countdownInterval = 0.25f;

    [Header("Camera Transition Settings")]
    [Tooltip("Duration (in seconds) over which the camera transitions to the new transform.")]
    public float cameraTransitionDuration = 1f;

    // Called when the scene starts.
    void Start()
    {
        // Cache ObserverBehaviour and TMP_Text components (if provided) for each target.
        foreach (var target in trackingTargets)
        {
            if (target.targetObject != null)
            {
                target.observer = target.targetObject.GetComponent<ObserverBehaviour>();
                if (target.observer == null)
                {
                    NetworkedLogger.Instance.Log("Target '" + target.targetObject.name + "' does not have an ObserverBehaviour component.");
                }
            }
            else
            {
                NetworkedLogger.Instance.Log("A tracking target is missing a target object reference.");
            }

            target.trackingTimer = 0f;
            target.locked = false;

            // Initialize the countdown text if provided.
            if (target.countdownTextObject != null)
            {
                target.countdownTMPText = target.countdownTextObject.GetComponent<TMP_Text>();
                if (target.countdownTMPText == null)
                {
                    NetworkedLogger.Instance.Log("Countdown Text GameObject '" + target.countdownTextObject.name + "' does not have a TMP_Text component.");
                }
                else
                {
                    target.countdownTMPText.text = trackingTimeThreshold.ToString("F2");
                }
            }

            // Ensure checkmark object is hidden initially.
            if (target.checkmarkObject != null)
            {
                target.checkmarkObject.SetActive(false);
            }
        }
    }

    // Called once per frame.
    void Update()
    {
        // Process each tracking target for locking and countdown updates.
        foreach (var target in trackingTargets)
        {
            // Only update if the target is not locked.
            if (!target.locked)
            {
                if (target.targetObject == null || target.observer == null)
                    continue;

                // If currently tracked, update timer and countdown.
                if (IsTracked(target.observer))
                {
                    target.trackingTimer += Time.deltaTime;
                    if (target.countdownTMPText != null)
                    {
                        float timeLeft = Mathf.Max(0f, trackingTimeThreshold - target.trackingTimer);
                        float displayTime = Mathf.Ceil(timeLeft / countdownInterval) * countdownInterval;
                        target.countdownTMPText.text = displayTime.ToString("F2");
                    }

                    // Lock the target if the threshold is reached.
                    if (target.trackingTimer >= trackingTimeThreshold)
                    {
                        target.locked = true;
                        // Use the target object's position instead of the observer's transform.
                        target.lockedPosition = target.targetObject.transform.position;
                        NetworkedLogger.Instance.Log("Locked target '" + target.targetObject.name + "' at position: " + target.lockedPosition);

                        // Toggle checkmark visibility and hide countdown text.
                        if (target.checkmarkObject != null)
                        {
                            // Position the checkmark at the locked position.
                            target.checkmarkObject.transform.position = target.lockedPosition;
                            target.checkmarkObject.SetActive(true);
                        }
                        if (target.countdownTextObject != null)
                        {
                            target.countdownTextObject.SetActive(false);
                        }
                    }
                }
                else
                {
                    // Reset the timer and countdown if tracking is lost.
                    target.trackingTimer = 0f;
                    if (target.countdownTMPText != null)
                    {
                        target.countdownTMPText.text = trackingTimeThreshold.ToString("F2");
                    }
                }
            }
        }

        // Collect locked targets.
        List<CustomTrackingTargetData> lockedTargets = new List<CustomTrackingTargetData>();
        foreach (var target in trackingTargets)
        {
            if (target.locked)
                lockedTargets.Add(target);
        }

        // When three markers are locked, compute calibration based on their positions.
        if (lockedTargets.Count >= 3)
        {
            // Compute the detected center as the average of all three locked positions.
            Vector3 detectedCenter = Vector3.zero;
            foreach (var target in lockedTargets)
            {
                detectedCenter += target.lockedPosition;
            }
            detectedCenter /= lockedTargets.Count;

            // Determine a forward direction by selecting the marker with the greatest horizontal (XZ) distance from the centroid.
            Vector2 detectedForward = Vector2.zero;
            float maxDistance = 0f;
            foreach (var target in lockedTargets)
            {
                Vector2 offset = new Vector2(target.lockedPosition.x - detectedCenter.x, target.lockedPosition.z - detectedCenter.z);
                if (offset.magnitude > maxDistance)
                {
                    maxDistance = offset.magnitude;
                    detectedForward = offset.normalized;
                }
            }

            // Assume the intended forward direction is along positive Z (i.e. (0,1) in XZ space).
            Vector2 intendedForward = new Vector2(0f, 1f);
            float angle = Vector2.SignedAngle(detectedForward, intendedForward);
            Quaternion rotationOffset = Quaternion.Euler(0f, angle, 0f);

            // Compute translation in local space using full 3D centers.
            Vector3 detectedLocal;
            Vector3 intendedLocal;
            if (transform.parent != null)
            {
                detectedLocal = transform.parent.InverseTransformPoint(detectedCenter);
                intendedLocal = transform.parent.InverseTransformPoint(Vector3.zero);
            }
            else
            {
                detectedLocal = detectedCenter;
                intendedLocal = Vector3.zero;
            }
            intendedLocal.y -= 0.03f; // Manual correction: The tracked objects usually appear slightly too high

            // Calculate the local translation offset so that:
            //     rotationOffset * detectedLocal + translationOffsetLocal == intendedLocal (which is zero)
            Vector3 translationOffsetLocal = intendedLocal - (rotationOffset * detectedLocal);

            // Get the current local offset of this CameraOffset object.
            Vector3 currentLocal = transform.localPosition;
            // Compute the new local position by adding the correction.
            Vector3 newLocalPosition = currentLocal + (Quaternion.Inverse(rotationOffset) * translationOffsetLocal);

            // Also update the local rotation (apply the inverse of our computed rotation offset).
            Quaternion currentLocalRotation = transform.localRotation;
            Quaternion newLocalRotation = Quaternion.Inverse(rotationOffset) * currentLocalRotation;

            NetworkedLogger.Instance.Log("Calibration computed:" +
                      "\n  angle offset: " + angle.ToString("F2") +
                      "\n  translationOffsetLocal: " + translationOffsetLocal +
                      "\n  currentLocal: " + currentLocal +
                      "\n  newLocalPosition: " + newLocalPosition +
                      "\n  newLocalRotation: " + newLocalRotation.eulerAngles);

            // Transition the CameraOffset's local transform.
            StartCoroutine(TransitionCameraLocal(newLocalPosition, newLocalRotation, cameraTransitionDuration));

            // Reset all targets so that calibration can re-run.
            foreach (var target in trackingTargets)
            {
                target.locked = false;
                target.trackingTimer = 0f;
                if (target.countdownTMPText != null)
                {
                    target.countdownTMPText.text = trackingTimeThreshold.ToString("F2");
                }
                // Re-enable countdown text and hide checkmark.
                if (target.countdownTextObject != null)
                {
                    target.countdownTextObject.SetActive(true);
                }
                if (target.checkmarkObject != null)
                {
                    target.checkmarkObject.SetActive(false);
                }
            }
        }
    }

    // Checks whether the given ObserverBehaviour indicates that the target is being tracked.
    private bool IsTracked(ObserverBehaviour observer)
    {
        if (observer == null)
            return false;

        TargetStatus status = observer.TargetStatus;
        return status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED;
    }

    // Coroutine that gradually transitions the object's local position and rotation.
    private IEnumerator TransitionCameraLocal(Vector3 targetLocalPosition, Quaternion targetLocalRotation, float duration)
    {
        Vector3 startLocalPosition = transform.localPosition;
        Quaternion startLocalRotation = transform.localRotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localPosition = Vector3.Lerp(startLocalPosition, targetLocalPosition, t);
            transform.localRotation = Quaternion.Slerp(startLocalRotation, targetLocalRotation, t);
            yield return null;
        }

        transform.localPosition = targetLocalPosition;
        transform.localRotation = targetLocalRotation;

        NetworkedLogger.Instance.Log("Transition complete.");
        NetworkedLogger.Instance.Log("Camera Offset final world position: " + transform.position + " rotation: " + transform.rotation.eulerAngles);

        // Log the Main Camera's transform (assuming it's the first child)
        if (transform.childCount > 0)
        {
            Transform mainCamera = transform.GetChild(0);
            NetworkedLogger.Instance.Log("Main Camera final local position: " + mainCamera.localPosition + " local rotation: " + mainCamera.localRotation.eulerAngles);
            NetworkedLogger.Instance.Log("Main Camera final world position: " + mainCamera.position + " rotation: " + mainCamera.rotation.eulerAngles);
        }
        else
        {
            NetworkedLogger.Instance.Log("No child found for Main Camera logging.");
        }
    }
}
