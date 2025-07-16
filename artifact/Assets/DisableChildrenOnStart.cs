using System.Collections.Generic;
using UnityEngine;

public class DisableChildrenOnStart : MonoBehaviour
{
    [Tooltip("List of child Transforms that should remain active.")]
    public List<Transform> exceptionChildren;

    void Start()
    {
        // Loop through every child of this GameObject
        foreach (Transform child in transform)
        {
            // Disable the child only if it's not in the exception list
            if (!exceptionChildren.Contains(child))
            {
                child.gameObject.SetActive(false);
            }
        }
    }
}
