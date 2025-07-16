using FishNet.Object;
using UnityEngine;
using System.Collections.Generic;

public class RemoveInstances : NetworkBehaviour
{
    [SerializeField]
    private List<GameObject> prefabsToRemove = new List<GameObject>();  // List of prefabs to remove

    /// <summary>
    /// Called by clients to request removal of all instances.
    /// If running on the server, it directly performs removal.
    /// Otherwise, it calls a ServerRpc to execute the logic on the server.
    /// </summary>
    public void RequestRemoveAllInstances()
    {
        if (base.IsServer)
        {
            RemoveAllInstances();
        }
        else
        {
            RemoveAllInstancesServerRpc();
        }
    }

    // Called on the server when a client requests removal.
    // RequireOwnership = false allows any client to request this action.
    [ServerRpc(RequireOwnership = false)]
    private void RemoveAllInstancesServerRpc()
    {
        RemoveAllInstances();
    }

    /// <summary>
    /// Removes all instances of the specified prefabs from the scene.
    /// Only runs on the server.
    /// </summary>
    private void RemoveAllInstances()
    {
        if (prefabsToRemove == null || prefabsToRemove.Count == 0)
        {
            NetworkedLogger.Instance.Log("No prefabs assigned to remove instances.");
            return;
        }

        // Find all active GameObjects in the scene.
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

        // Iterate through each prefab to remove.
        foreach (GameObject prefab in prefabsToRemove)
        {
            if (prefab == null)
            {
                NetworkedLogger.Instance.Log("One of the prefabs in the list is null and will be skipped.");
                continue;
            }

            // Iterate through all active GameObjects and remove instances matching the prefab.
            foreach (GameObject obj in allObjects)
            {
                // Here we compare by name (similar to the original implementation).
                if (obj.name.Contains(prefab.name))
                {
                    // Check if the object is networked.
                    NetworkObject netObj = obj.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        // Despawn networked objects via FishNet.
                        netObj.Despawn();
                    }
                    else
                    {
                        // For non-networked objects, simply destroy.
                        Destroy(obj);
                    }
                }
            }

            NetworkedLogger.Instance.Log($"All instances of {prefab.name} have been removed.");
        }
    }
}
