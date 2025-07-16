using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GlowOnGrab : NetworkBehaviour
{
    private Renderer objectRenderer;
    private XRGrabInteractable grabInteractable;

    // Sync emission state across network
    private readonly SyncVar<bool> isGlowing = new SyncVar<bool>();

    private void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        // Subscribe to SyncVar change event
        isGlowing.OnChange += OnGlowChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateEmission(isGlowing.Value);
    }

    // Called by XR Interaction events (Grab)
    public void OnGrabbed()
    {
        ServerSetGlowState(true);
    }

    // Called by XR Interaction events (Release)
    public void OnReleased()
    {
        ServerSetGlowState(false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerSetGlowState(bool glowState)
    {
        isGlowing.Value = glowState;
    }

    private void OnGlowChanged(bool prev, bool next, bool asServer)
    {
        UpdateEmission(next);
    }

    private void UpdateEmission(bool glow)
    {
        // Always get the current material instance from the renderer
        Material currentMaterial = objectRenderer.material;
        if (currentMaterial != null && currentMaterial.HasProperty("_EmissionColor"))
        {
            if (glow)
                currentMaterial.EnableKeyword("_EMISSION");
            else
                currentMaterial.DisableKeyword("_EMISSION");
        }
    }
}
