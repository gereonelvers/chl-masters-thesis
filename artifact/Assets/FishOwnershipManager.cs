using FishNet;
using FishNet.Object;
using FishNet.Connection;
using UnityEngine;

public class OwnershipController : NetworkBehaviour
{
    // Call this method from the client that wants ownership
    public void RequestOwnership()
    {
        // Only non-owners should request ownership
        if (!IsOwner)
        {
            RequestOwnershipServerRpc();
            //Debug.Log("Dumb stupid hack called");
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        else {
            NetworkedLogger.Instance.Log($"[FishOwnershipManager] Already owner of {NetworkObject.name}");
        }
    }

    // Server RPC that will run on the server
    [ServerRpc(RequireOwnership = false)]
    private void RequestOwnershipServerRpc(NetworkConnection networkConnection = null)
    {
        // The networkConnection parameter is automatically filled with the sender's connection
        if (networkConnection != null)
        {
            // Give ownership to the requesting client
            NetworkObject.GiveOwnership(networkConnection);
            NetworkedLogger.Instance.Log($"[FishOwnershipManager] Requesting ownership for {NetworkObject.name} for {networkConnection}");
        }
        else {
            NetworkedLogger.Instance.Log("[FishOwnershipManager] NetworkConnection is null");
        }
    }

    public void ReleaseOwnership()
    {
        if (IsOwner)
        {
            NetworkedLogger.Instance.Log($"[FishOwnershipManager] Trying to release ownership of {NetworkObject.name} back to server");
            ReleaseOwnershipServerRpc();
        }
        else
        {
            NetworkedLogger.Instance.Log($"[FishOwnershipManager] Cannot release ownership. Not the owner of {NetworkObject.name}");
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void ReleaseOwnershipServerRpc(NetworkConnection networkConnection = null)
    {
        NetworkObject.GiveOwnership(InstanceFinder.ClientManager.Connection);
        NetworkedLogger.Instance.Log("Giving back to "+ InstanceFinder.ClientManager.Connection.ToString());
        NetworkedLogger.Instance.Log($"[FishOwnershipManager] Released ownership of {NetworkObject.name} back to server");
    }
}