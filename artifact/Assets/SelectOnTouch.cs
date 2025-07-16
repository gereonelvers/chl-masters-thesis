using UnityEngine;

public class SelectOnTap : MonoBehaviour
{
    [Header("Selection Settings")]
    [Tooltip("The prefab to select when the button is tapped.")]
    public GameObject selectablePrefab;

    [Tooltip("Reference to the SelectionManager to use.")]
    public SelectionManager selectionManager;

    private void Start()
    {
        // No feedback UI to initialize.
    }

    // Call this method from your button's OnClick event.
    public void OnSelectButton()
    {
        NetworkedLogger.Instance.Log("Button tapped to select object: " + selectablePrefab.name);

        // Set the selection using the provided SelectionManager if available.
        if (selectionManager != null)
        {
            selectionManager.SetSelection(selectablePrefab);
        }
        else
        {
            NetworkedLogger.Instance.Log("SelectOnTap: No SelectionManager assigned!");
        }

        // Build display text using the ItemAttributes component if present.
        string displayText = selectablePrefab.name;
        ItemAttributes attributes = selectablePrefab.GetComponent<ItemAttributes>();
        if (attributes != null)
        {
            displayText = attributes.itemName;
        }
    }
}
