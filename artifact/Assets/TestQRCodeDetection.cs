using UnityEngine;
using System;
using Microsoft.MixedReality.OpenXR;
using TMPro;
//using Sentry;

public class TestQRCodeDetection : MonoBehaviour
{
    [SerializeField] private GameObject mainText;
    [SerializeField] private ARMarkerManager markerManager;
    [SerializeField] private GameObject baseAnchor;
    [SerializeField] private Transform hologramParent; // Dedicated parent for holograms

    private TextMeshProUGUI m_TextMeshPro;

    private void Start()
    {
        m_TextMeshPro = mainText?.GetComponent<TextMeshProUGUI>();
        if (m_TextMeshPro == null)
        {
            Debug.LogError("TextMeshProUGUI component not found on mainText GameObject.");
            return;
        }

        //SentrySdk.CaptureMessage("Test event");

        UpdateMainText($"[Initialization]\n" +
                       $"Time: {DateTime.Now:HH:mm:ss}\n" +
                       $"Marker Manager is {(markerManager != null ? "assigned" : "NOT assigned")}.");

        if (markerManager == null)
        {
            Debug.LogError("ARMarkerManager is not assigned.");
            return;
        }

        if (baseAnchor == null)
        {
            baseAnchor = new GameObject("SceneAnchor");
            Debug.Log("[Initialization] Created a new SceneAnchor GameObject.");
        }

        if (hologramParent == null)
        {
            hologramParent = new GameObject("HologramParent").transform;
            hologramParent.parent = baseAnchor.transform;
            Debug.Log("[Initialization] Created HologramParent under baseAnchor.");
        }

        markerManager.markersChanged += OnMarkersChanged;
    }

    private void OnMarkersChanged(ARMarkersChangedEventArgs args)
    {
        string diagnosticInfo = $"[Markers Changed at {DateTime.Now:HH:mm:ss}]\n" +
                                $"Added: {args.added.Count}, Updated: {args.updated.Count}, Removed: {args.removed.Count}";
        Debug.Log(diagnosticInfo);

        foreach (var addedMarker in args.added)
        {
            HandleAddedMarker(addedMarker);
        }

        foreach (var updatedMarker in args.updated)
        {
            HandleUpdatedMarker(updatedMarker);
        }

        foreach (var removedMarker in args.removed)
        {
            HandleRemovedMarker(removedMarker);
        }
    }

    private void HandleAddedMarker(ARMarker addedMarker)
    {
        string markerID = addedMarker.trackableId.ToString();
        Debug.Log($"[Added] QR Code Detected! Marker ID: {markerID}");

        string qrCodeString = addedMarker.GetDecodedString();
        string positionInfo = addedMarker.transform != null ?
                              $"Position: {addedMarker.transform.position}" :
                              "Position: N/A";

        if (addedMarker.transform != null)
        {
            baseAnchor.transform.position = addedMarker.transform.position;
            baseAnchor.transform.rotation = Quaternion.identity;
            Debug.Log($"[Base Anchor Updated] Marker {markerID} set as the new origin at position: {baseAnchor.transform.position}");
        }

        string info = $"[Marker Added]\n" +
                      $"Time: {DateTime.Now:HH:mm:ss}\n" +
                      $"Marker ID: {markerID}\n" +
                      $"{positionInfo}\n" +
                      $"QR Code: {qrCodeString}\n" +
                      "[Base Anchor updated]";
        UpdateMainText(info);
    }

    private void HandleUpdatedMarker(ARMarker updatedMarker)
    {
        string markerID = updatedMarker.trackableId.ToString();
        string qrCodeString = updatedMarker.GetDecodedString();
        string positionInfo = updatedMarker.transform != null ?
                              $"Position: {updatedMarker.transform.position}" :
                              "Position: N/A";

        if (updatedMarker.transform != null)
        {
            baseAnchor.transform.position = updatedMarker.transform.position;
            baseAnchor.transform.rotation = Quaternion.identity;
            Debug.Log($"[Base Anchor Updated] Marker {markerID} updated the origin to position: {baseAnchor.transform.position}");
        }

        string info = $"[Marker Updated]\n" +
                      $"Time: {DateTime.Now:HH:mm:ss}\n" +
                      $"Marker ID: {markerID}\n" +
                      $"{positionInfo}\n" +
                      $"QR Code: {qrCodeString}\n" +
                      "[Base Anchor updated (rotation ignored)]";

        Debug.Log(info);
        UpdateMainText(info);
    }

    private void HandleRemovedMarker(ARMarker removedMarker)
    {
        string markerID = removedMarker.trackableId.ToString();
        Debug.Log($"[Removed] QR Code Removed! Marker ID: {markerID}");

        string info = $"[Marker Removed]\n" +
                      $"Time: {DateTime.Now:HH:mm:ss}\n" +
                      $"Marker ID: {markerID}\n" +
                      $"QR Code no longer in view.";
        UpdateMainText(info);
    }

    private void UpdateMainText(string info)
    {
        if (m_TextMeshPro != null)
        {
            m_TextMeshPro.text = info;
        }
    }

    private void OnDrawGizmos()
    {
        if (baseAnchor != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(baseAnchor.transform.position, 0.1f);
        }
    }
}

