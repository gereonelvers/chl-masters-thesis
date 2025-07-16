using Photon.Pun;
using UnityEngine;

public class SpawnOnClick : MonoBehaviour
{
    [SerializeField] private Transform spawnLocation;
    [SerializeField] private Transform sceneRoot;  // Assign your SceneRoot here in the Inspector

    public void SpawnPrefab()
    {
        GameObject instance;

        if (spawnLocation != null)
        {
            instance = PhotonNetwork.Instantiate(
                "MultiUserGrabInteractable",
                spawnLocation.position,
                spawnLocation.rotation
            );
        }
        else
        {
            // Use a fallback position/rotation if spawnLocation isn't set
            Vector3 fallbackPos = new Vector3(0.0087f, 1.729f, 1f);
            instance = PhotonNetwork.Instantiate(
                "MultiUserGrabInteractable",
                fallbackPos,
                Quaternion.identity
            );
        }

        // Optionally, set the scene root as a parent if needed:
        if (sceneRoot != null && instance != null)
        {
            instance.transform.SetParent(sceneRoot, true);
        }
    }

    // New function to spawn the BuildingBlock prefab
    public void SpawnBlock()
    {
        GameObject instance;

        if (spawnLocation != null)
        {
            instance = PhotonNetwork.Instantiate(
                "BuildingBlock",
                spawnLocation.position,
                spawnLocation.rotation
            );
        }
        else
        {
            // Use the same fallback position/rotation as in SpawnPrefab
            Vector3 fallbackPos = new Vector3(0.0087f, 1.729f, 1f);
            instance = PhotonNetwork.Instantiate(
                "BuildingBlock",
                fallbackPos,
                Quaternion.identity
            );
        }

        // Optionally, assign this new instance to the sceneRoot:
        //if (sceneRoot != null && instance != null)
        //{
        //    instance.transform.SetParent(sceneRoot, true);
        //}
    }

    public void SpawnSocket()
    {
        GameObject instance;

        if (spawnLocation != null)
        {
            instance = PhotonNetwork.Instantiate(
                "Socket",
                spawnLocation.position,
                spawnLocation.rotation
            );
        }
        else
        {
            // Use the same fallback position/rotation as in SpawnPrefab
            Vector3 fallbackPos = new Vector3(0.0087f, 1.729f, 1f);
            instance = PhotonNetwork.Instantiate(
                "BuildingBlock",
                fallbackPos,
                Quaternion.identity
            );
        }

        // Optionally, assign this new instance to the sceneRoot:
        // if (sceneRoot != null && instance != null)
        // {
        //     instance.transform.SetParent(sceneRoot, true);
        // }
    }
}
