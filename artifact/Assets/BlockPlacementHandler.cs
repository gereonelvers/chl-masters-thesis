using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BlockPlacementHandler : XRGrabInteractable
{
    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        //base.OnSelectExited(args);
        Debug.Log($"{gameObject.name} was released.");
        //SnapAndPlaceBlock();
    }

    void SnapAndPlaceBlock()
    {
        Vector3 originalPosition = transform.position;
        Debug.Log($"{gameObject.name} original position: {originalPosition}");

        bool placed = true;
        if (placed)
        {
            Debug.Log($"{gameObject.name} placed successfully.");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} placement failed. Returning to previous position.");
            // Optionally, reset position or handle failed placement
            // Example: Disable the block or return it to a spawn point
            // transform.position = originalPosition;
        }
    }
}
