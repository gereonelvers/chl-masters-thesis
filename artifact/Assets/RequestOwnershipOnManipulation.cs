using Photon.Pun;
using UnityEngine;
using Microsoft.MixedReality.OpenXR;

public class RequestOwnershipOnManipulation : MonoBehaviourPun
{
    // Called when manipulation starts (i.e. when the user begins moving the object)
    public void OnManipulationStarted()
    {
        if (photonView == null) {
            Debug.Log("photonview missing, cant check ownership");
        }
        if (!photonView.IsMine)
        {
            Debug.Log("Requesting ownership");
            // Request ownership so that your movement updates are sent over the network.
            photonView.RequestOwnership();
        }
        else {
            Debug.Log("Cube already mine");
        }
    }

}
