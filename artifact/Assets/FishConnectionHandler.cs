using FishNet;
using FishNet.Transporting;
using UnityEngine;
using FishNet.Connection; // Needed for ClientConnectionStateArgs and LocalConnectionState

public class FishConnectionHandler : MonoBehaviour
{
    // Delay (in seconds) before trying to reconnect.
    [SerializeField]
    private float reconnectDelay = 5f;

    private void Start()
    {
#if UNITY_EDITOR
        Debug.Log("[Fish-net] Running in primary Editor, starting server");
        NetworkedLogger.Instance.Log("[Fish-net] Running in primary Editor, starting server");
        InstanceFinder.ServerManager.StartConnection();
#endif

        Debug.Log("[Fish-net] Starting client");
        NetworkedLogger.Instance.Log("[Fish-net] Starting client");

        // Subscribe to the client connection state events.
        InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;

        // Attempt the initial connection.
        InstanceFinder.ClientManager.StartConnection();
    }

    // This callback is invoked when the client's connection state changes.
    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        // Check if the client has stopped (meaning either it failed to connect or it dropped).
        if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            Debug.Log($"[Fish-net] Client connection stopped. Will attempt reconnect in {reconnectDelay} seconds.");
            NetworkedLogger.Instance.Log($"[Fish-net] Client connection stopped. Will attempt reconnect in {reconnectDelay} seconds.");
            Invoke(nameof(RetryConnection), reconnectDelay);
        }
    }

    // Called after a delay to retry the connection.
    private void RetryConnection()
    {
        // Ensure we aren't already connected before retrying.
        if (!InstanceFinder.ClientManager.Connection.IsActive)
        {
            NetworkedLogger.Instance.Log("[Fish-net] Retrying client connection.");
            InstanceFinder.ClientManager.StartConnection();
        }
    }

    private void OnDestroy()
    {
        if (InstanceFinder.ClientManager != null)
        {
            InstanceFinder.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        }
    }
}
