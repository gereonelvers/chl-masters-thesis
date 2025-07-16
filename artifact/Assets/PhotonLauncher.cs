using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PhotonNetworkManager : MonoBehaviourPunCallbacks
{
    public string roomName = "Default";
    public GameObject masterObject; // Assign in the Inspector (set inactive by default)

    void Start()
    {
        PhotonNetwork.LogLevel = PunLogLevel.Full;
        Debug.Log("[PUN] Connecting...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[PUN] Connected to Master. Joining room...");
        PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions { MaxPlayers = 8 }, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[PUN] Joined room: " + roomName);

        // Check if this client is the MasterClient and toggle the masterObject's visibility accordingly
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[PUN] This client is the MasterClient. Enabling masterObject.");
            if (masterObject != null)
                masterObject.SetActive(true);
        }
        else
        {
            // Optionally ensure it's disabled for non-master clients.
            if (masterObject != null)
                masterObject.SetActive(false);
        }
    }
}
