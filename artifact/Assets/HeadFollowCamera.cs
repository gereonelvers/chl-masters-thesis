using FishNet.Object;
using UnityEngine;

public class HeadFollowCamera : NetworkBehaviour
{
    [Tooltip("How far from the camera should the head be relative to its pivot?")]
    public Vector3 offset = new Vector3(0f, 0f, -0.01f);

    void LateUpdate()
    {
        // Only update if this is the local player's head object.
        if (!IsOwner)
            return;

        Camera localCamera = Camera.main;
        if (localCamera != null)
        {
            // Update position with the offset applied in the camera's rotation space.
            transform.position = localCamera.transform.position + localCamera.transform.rotation * offset;
            transform.rotation = localCamera.transform.rotation;
        }
    }
}
