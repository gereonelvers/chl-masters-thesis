using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Photon.Pun;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// This doesn't actually do anything as Ownership transfer is currently handled as "Takeover" in the PhotonView
public class MultiUserGrabXR : XRGrabInteractable
{
    private PhotonView photonView;

    protected override void Awake()
    {
        base.Awake();
        photonView = GetComponent<PhotonView>();
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        Debug.Log("Object selected, checking ownership");

        // Request ownership if we're not already the owner
        if (!photonView.AmOwner)
        {
            Debug.Log("Requesting ownership!");
            photonView.RequestOwnership();
        }
    }
}
