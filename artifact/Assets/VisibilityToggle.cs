using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class VisibilityToggle : MonoBehaviourPunCallbacks
{
    [Tooltip("If true, the object will be visible only if this client is the Master Client. " +
             "If false, the object will be visible only for non-master clients.")]
    public bool visibleForMaster = true;

    void Start()
    {
        // Optionally update visibility in Start if already in a room.
        if (PhotonNetwork.InRoom)
        {
            UpdateVisibility();
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[VisibilityToggle] joined room");
        UpdateVisibility();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log("[VisibilityToggle] master client switched");
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        // The object is active if the client's master status matches the desired setting.
        bool shouldBeActive = (PhotonNetwork.IsMasterClient == visibleForMaster);
        gameObject.SetActive(shouldBeActive);
        Debug.Log($"[VisibilityToggle] {gameObject.name} set to active: {shouldBeActive} (IsMasterClient: {PhotonNetwork.IsMasterClient}, visibleForMaster: {visibleForMaster})");
    }
}
