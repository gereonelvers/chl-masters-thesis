using UnityEngine;

public class HideOnPlatform : MonoBehaviour
{
    // When true, the GameObject will be hidden when running in the Editor.
    public bool hideInEditor = false;

    // When true, the GameObject will be hidden when running on a real device.
    public bool hideOnDevice = true;

    void Start()
    {
        // Check if we're running in the Editor or on a device
        if (Application.isEditor)
        {
            if (hideInEditor)
            {
                // Hide the GameObject in the Editor if toggle is on.
                gameObject.SetActive(false);
            }
        }
        else
        {
            if (hideOnDevice)
            {
                // Hide the GameObject on real devices if toggle is on.
                gameObject.SetActive(false);
            }
        }
    }
}
