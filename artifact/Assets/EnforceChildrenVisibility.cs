using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class EnforceChildrenVisibility : NetworkBehaviour
{
    [SerializeField] private GameObject parentObject;

    // Add a serialized field to set the initial state in the Inspector
    [SerializeField] private bool initiallyVisible = false;

    // Class-based SyncVar for FishNet 4+
    public readonly SyncVar<bool> IsParentVisible = new SyncVar<bool>();

    // This function is called when you want to toggle the parent's visibility
    public void EnforceVisibilityState()
    {
        // Only proceed if we're connected to the server
        if (!IsSpawned)
            return;

        NetworkedLogger.Instance.Log($"Toggling visibility of {parentObject.name} from {IsParentVisible.Value} to {!IsParentVisible.Value}");

        // Call the server RPC to toggle the visibility
        ToggleVisibilityStateServerRpc();
    }

    // Called on the server when a client requests the visibility update
    [ServerRpc(RequireOwnership = false)]
    private void ToggleVisibilityStateServerRpc()
    {
        // Toggle the SyncVar on the server
        IsParentVisible.Value = !IsParentVisible.Value;
    }

    // Set up callback for SyncVar changes and initialize state
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        // Set initial state on server based on the inspector setting
        if (IsServer)
        {
            IsParentVisible.Value = initiallyVisible;
        }

        // Subscribe to SyncVar changes
        IsParentVisible.OnChange += OnVisibilityStateChanged;
    }

    // Clean up subscription when the network object despawns
    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        IsParentVisible.OnChange -= OnVisibilityStateChanged;
    }

    // Callback for when the visibility state changes
    private void OnVisibilityStateChanged(bool previousValue, bool newValue, bool asServer)
    {
        // Only update the visual state on clients or on the host
        if (!asServer || IsClient)
        {
            UpdateVisibilityState(newValue);
        }
    }

    // Updates the local visibility state and appearance
    private void UpdateVisibilityState(bool isVisible)
    {
        // Ensure the GameObject reference is valid
        if (parentObject == null)
            return;

        // Actually set the active state of the parent object
        parentObject.SetActive(isVisible);

        // Update the cube color
        Renderer cubeRenderer = GetComponent<Renderer>();
        if (cubeRenderer != null)
        {
            // Set the shared material to avoid memory leaks
            if (cubeRenderer.material != null)
            {
                cubeRenderer.material.color = isVisible ? Color.green : Color.red;
            }
        }
    }

    // Makes sure clients get the correct state when joining
    public override void OnStartClient()
    {
        base.OnStartClient();

        // Force update the visibility state based on the current SyncVar value
        UpdateVisibilityState(IsParentVisible.Value);
    }
}