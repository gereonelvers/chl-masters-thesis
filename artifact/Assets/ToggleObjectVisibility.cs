using UnityEngine;

public class ToggleMultipleObjects : MonoBehaviour
{
    // List of GameObjects to toggle
    [Tooltip("Drag and drop all GameObjects you want to toggle here.")]
    public GameObject[] objectsToToggle;

    /// <summary>
    /// Toggles the active state of each GameObject in the objectsToToggle array.
    /// </summary>
    public void ToggleObjects()
    {
        foreach (GameObject obj in objectsToToggle)
        {
            if (obj != null)
            {
                bool isActive = obj.activeSelf;
                obj.SetActive(!isActive);
            }
            else
            {
                Debug.LogWarning("ToggleMultipleObjects: One of the objects in the list is not assigned.");
            }
        }
    }
}
