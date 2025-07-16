using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CubeSnapController : MonoBehaviourPunCallbacks
{
    [Header("Snap Settings")]
    public float snapRotationStep = 90f; // Grid step, so twist is clamped to ±45°
    public float unsnapHoldThreshold = 5f;
    public GameObject unsnapIndicator;
    public float snapAnimationDuration = 0.5f;

    [Header("Preview Settings")]
    [Tooltip("Prefab for the preview (should be a ghost/semi-transparent copy of the cube)")]
    public GameObject previewPrefab;
    private GameObject previewInstance;

    private bool isHeld = false;
    private float unsnapTimer = 0f;
    private SnapZone[] localSnapZones;

    // For gizmo drawing and tracking snap destination.
    private bool snapInProgress = false;
    private Vector3 snapDestination;

    // --- Helper Class for storing snap candidate pairs ---
    private class SnapCandidate
    {
        public SnapZone localZone;
        public SnapZone targetZone;
    }
    private List<SnapCandidate> snapCandidates = new List<SnapCandidate>();

    private void Awake()
    {
        localSnapZones = GetComponentsInChildren<SnapZone>();
        foreach (SnapZone zone in localSnapZones)
        {
            zone.parentCube = this;
        }
    }

    private void SetMainCollidersEnabled(bool enabled)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            if (col.GetComponent<SnapZone>() != null)
                continue;
            col.enabled = enabled;
        }
    }

    public bool IsBeingHeld() => isHeld;

    public void OnGrabStarted()
    {
        photonView.RequestOwnership();
        isHeld = true;
        unsnapTimer = 0f;
        if (unsnapIndicator)
            unsnapIndicator.SetActive(false);

        if (previewPrefab != null && previewInstance == null)
        {
            previewInstance = Instantiate(previewPrefab);
            previewInstance.SetActive(false);
        }

        SetMainCollidersEnabled(false);
    }

    public void OnGrabEnded()
    {
        isHeld = false;

        if (snapCandidates.Count > 0)
        {
            SnapCandidate bestCandidate = null;
            float bestDistance = Mathf.Infinity;
            foreach (SnapCandidate candidate in snapCandidates)
            {
                float distance = Vector3.Distance(candidate.localZone.transform.position, candidate.targetZone.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCandidate = candidate;
                }
            }

            if (bestCandidate != null)
            {
                SnapToCandidate(bestCandidate);
            }
        }
        else
        {
            Debug.Log("No snap candidate found on release for: " + gameObject.name);
        }

        if (unsnapIndicator)
            unsnapIndicator.SetActive(false);
        if (previewInstance != null)
            previewInstance.SetActive(false);

        SetMainCollidersEnabled(true);
    }

    public void AddSnapCandidate(SnapZone localZone, SnapZone targetZone)
    {
        Debug.Log("Add snap candidate " + localZone.parentCube.name + ": " + localZone.name +
                  " and " + targetZone.parentCube.name + ": " + targetZone.name);
        foreach (SnapCandidate candidate in snapCandidates)
        {
            if (candidate.localZone == localZone && candidate.targetZone == targetZone)
                return;
        }
        SnapCandidate newCandidate = new SnapCandidate { localZone = localZone, targetZone = targetZone };
        snapCandidates.Add(newCandidate);
    }

    public void RemoveSnapCandidate(SnapZone localZone, SnapZone targetZone)
    {
        Debug.Log("Remove snap candidate");
        snapCandidates.RemoveAll(c => c.localZone == localZone && c.targetZone == targetZone);
    }

    private void ClearAllSnapCandidates() => snapCandidates.Clear();

    private Vector3 GetSnapZoneTangent(SnapZone zone)
    {
        // For top or bottom faces, use the cube’s forward vector.
        // For left/right or front/back, also use forward or right accordingly.
        if (zone.snapDirection == Vector3.up || zone.snapDirection == Vector3.down)
            return zone.parentCube.transform.forward;
        else if (zone.snapDirection == Vector3.left || zone.snapDirection == Vector3.right)
            return zone.parentCube.transform.forward;
        else
            return zone.parentCube.transform.right;
    }

    /// <summary>
    /// Calculates the snap destination (position and rotation) for this cube.
    /// It starts by quantizing the held cube’s rotation, then computes the minimal
    /// alignment needed to match the target zone. A twist correction is applied and clamped,
    /// and finally the rotation is re-quantized.
    /// </summary>
    private bool CalculateSnapTransform(SnapCandidate candidate, out Vector3 newCubePosition, out Quaternion finalDesiredRotation)
    {
        newCubePosition = Vector3.zero;
        finalDesiredRotation = Quaternion.identity;
        if (candidate == null)
            return false;

        // Start from the cube’s current grid-aligned rotation.
        Quaternion quantizedCurrent = QuantizeRotationEuler(transform.rotation);
        // Compute the world direction of the local snap zone based on this quantized rotation.
        Vector3 movingFaceWorldDir = quantizedCurrent * candidate.localZone.snapDirection;
        // Use the target cube’s grid-aligned rotation as well.
        Quaternion targetCubeQuantized = QuantizeRotationEuler(candidate.targetZone.parentCube.transform.rotation);
        Vector3 targetFaceWorldDir = targetCubeQuantized * candidate.targetZone.snapDirection;

        // Compute the rotation needed to align the held face with the target (flipped).
        Quaternion alignmentRotation = Quaternion.FromToRotation(movingFaceWorldDir, -targetFaceWorldDir);
        float faceAngleDiff = Vector3.Angle(movingFaceWorldDir, -targetFaceWorldDir);
        Debug.LogFormat("Cube: {0}, Candidate: {1} vs {2}, FaceAngleDifference: {3}",
            gameObject.name, candidate.localZone.name, candidate.targetZone.name, faceAngleDiff);
        // If already close enough, skip alignment.
        if (faceAngleDiff < snapRotationStep * 0.5f)
        {
            alignmentRotation = Quaternion.identity;
            Debug.Log("Alignment rotation is negligible; using identity.");
        }

        // Now, apply the alignment correction to the quantized current rotation.
        Quaternion desiredRotation = quantizedCurrent * alignmentRotation;

        // --- Twist Correction for Corner Alignment ---
        Vector3 contactNormal = (-targetFaceWorldDir).normalized;
        Vector3 movingTangent = desiredRotation * GetSnapZoneTangent(candidate.localZone);
        Vector3 targetTangent = targetCubeQuantized * GetSnapZoneTangent(candidate.targetZone);
        float angleDifference = Vector3.SignedAngle(movingTangent, targetTangent, contactNormal);
        Debug.LogFormat("Twist correction: AngleDifference = {0}", angleDifference);
        // Clamp twist to ±snapRotationStep/2 (i.e. ±45° if snapRotationStep is 90°)
        float clampedTwist = Mathf.Clamp(angleDifference, -snapRotationStep * 0.5f, snapRotationStep * 0.5f);
        Debug.LogFormat("Clamped Twist Correction Angle: {0}", clampedTwist);
        Quaternion twistRotation = Quaternion.AngleAxis(clampedTwist, contactNormal);

        finalDesiredRotation = twistRotation * desiredRotation;
        Debug.Log("Desired Rotation Euler BEFORE final quantization: " + finalDesiredRotation.eulerAngles);

        // Quantize the final rotation to ensure grid alignment.
        finalDesiredRotation = QuantizeRotationEuler(finalDesiredRotation);

        // --- Position Calculation ---
        Vector3 movingLocalContactEdge = candidate.localZone.transform.localPosition;
        Vector3 scaledMovingContactEdge = Vector3.Scale(transform.localScale, movingLocalContactEdge);
        Vector3 targetLocalContactEdge = candidate.targetZone.transform.localPosition;
        Vector3 targetContactEdgeWorld = candidate.targetZone.parentCube.transform.TransformPoint(targetLocalContactEdge);
        Vector3 computedOffset = finalDesiredRotation * scaledMovingContactEdge;
        newCubePosition = targetContactEdgeWorld - computedOffset;

        // Quantize position to avoid floating point imprecision.
        newCubePosition.x = Mathf.Round(newCubePosition.x * 10f) / 10f;
        newCubePosition.y = Mathf.Round(newCubePosition.y * 10f) / 10f;
        newCubePosition.z = Mathf.Round(newCubePosition.z * 10f) / 10f;

        return true;
    }

    private float NormalizeAngle(float angle)
    {
        angle = angle % 360f;
        if (angle > 180f)
            angle -= 360f;
        if (angle < -180f)
            angle += 360f;
        return angle;
    }

    /// <summary>
    /// Quantizes the given rotation by converting its Euler angles into the -180 to 180 range
    /// and then rounding each component to the nearest multiple of 90.
    /// </summary>
    private Quaternion QuantizeRotationEuler(Quaternion rotation)
    {
        Vector3 rawEuler = rotation.eulerAngles;
        Debug.Log("Raw Euler angles: " + rawEuler);
        Vector3 normEuler = new Vector3(
            NormalizeAngle(rawEuler.x),
            NormalizeAngle(rawEuler.y),
            NormalizeAngle(rawEuler.z)
        );
        Debug.Log("Normalized Euler angles: " + normEuler);
        Vector3 quantEuler = new Vector3(
            Mathf.Round(normEuler.x / 90f) * 90f,
            Mathf.Round(normEuler.y / 90f) * 90f,
            Mathf.Round(normEuler.z / 90f) * 90f
        );
        Debug.Log("Rounded Euler angles: " + quantEuler);
        Quaternion quantizedRotation = Quaternion.Euler(quantEuler);
        Debug.Log("Final Quantized Rotation Euler: " + quantizedRotation.eulerAngles);
        return quantizedRotation;
    }

    private void SnapToCandidate(SnapCandidate candidate)
    {
        Debug.Log("=== Snapping " + gameObject.name + " === to " +
                  candidate.localZone.parentCube.name + ": " + candidate.localZone.name);
        Vector3 newCubePosition;
        Quaternion finalDesiredRotation;
        if (!CalculateSnapTransform(candidate, out newCubePosition, out finalDesiredRotation))
            return;

        Debug.Log("Computed New Cube Position: " + newCubePosition);
        Debug.Log("Final Desired Rotation (Euler): " + finalDesiredRotation.eulerAngles);

        snapDestination = newCubePosition;
        snapInProgress = true;
        StartCoroutine(AnimateSnap(newCubePosition, finalDesiredRotation, snapAnimationDuration));
    }

    IEnumerator AnimateSnap(Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        bool hadRigidbody = (rb != null);
        if (hadRigidbody)
            rb.isKinematic = true;

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPosition;
        transform.rotation = targetRotation;

        if (hadRigidbody)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        SetMainCollidersEnabled(true);
        Debug.Log("Completed snapping animation for " + gameObject.name);
        snapInProgress = false;
    }

    private void Update()
    {
        if (isHeld)
        {
            unsnapTimer += Time.deltaTime;
            if (unsnapIndicator)
                unsnapIndicator.SetActive(true);

            if (snapCandidates.Count > 0)
            {
                SnapCandidate bestCandidate = null;
                float bestDistance = Mathf.Infinity;
                foreach (SnapCandidate candidate in snapCandidates)
                {
                    float distance = Vector3.Distance(candidate.localZone.transform.position,
                                                      candidate.targetZone.transform.position);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestCandidate = candidate;
                    }
                }
                if (bestCandidate != null)
                {
                    Vector3 snapPos;
                    Quaternion snapRot;
                    if (CalculateSnapTransform(bestCandidate, out snapPos, out snapRot))
                    {
                        if (previewInstance == null && previewPrefab != null)
                        {
                            previewInstance = Instantiate(previewPrefab);
                        }
                        if (previewInstance != null)
                        {
                            previewInstance.SetActive(true);
                            previewInstance.transform.position = snapPos;
                            previewInstance.transform.rotation = snapRot;
                            previewInstance.transform.localScale = transform.localScale;
                        }
                    }
                }
                else if (previewInstance != null)
                {
                    previewInstance.SetActive(false);
                }
            }
            else if (previewInstance != null)
            {
                previewInstance.SetActive(false);
            }

            if (unsnapTimer >= unsnapHoldThreshold)
            {
                StartCoroutine(UnsnapCube());
                unsnapTimer = 0f;
            }
        }
        else
        {
            unsnapTimer = 0f;
            if (previewInstance != null)
                previewInstance.SetActive(false);
        }
    }

    IEnumerator UnsnapCube()
    {
        Debug.Log("Unsnap triggered on " + gameObject.name);
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos; // Cube isn't moved in this example.
        float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = endPos;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (snapInProgress)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(snapDestination, 0.05f);
            Gizmos.DrawLine(transform.position, snapDestination);
        }
        else if (isHeld && snapCandidates != null && snapCandidates.Count > 0)
        {
            SnapCandidate bestCandidate = null;
            float bestDistance = Mathf.Infinity;
            foreach (SnapCandidate candidate in snapCandidates)
            {
                float distance = Vector3.Distance(candidate.localZone.transform.position,
                                                  candidate.targetZone.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCandidate = candidate;
                }
            }
            if (bestCandidate != null)
            {
                Vector3 snapPos;
                Quaternion snapRot;
                if (CalculateSnapTransform(bestCandidate, out snapPos, out snapRot))
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(snapPos, 0.05f);
                    Gizmos.DrawLine(transform.position, snapPos);
                }
            }
        }
    }
#endif

    private void OnDestroy()
    {
        if (previewInstance != null)
            Destroy(previewInstance);
    }
}
