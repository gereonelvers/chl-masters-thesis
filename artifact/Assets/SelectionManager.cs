using UnityEngine;
using TMPro;

public class SelectionManager : MonoBehaviour
{
    // Holds the currently selected prefab.
    public GameObject SelectedPrefab { get; private set; }

    [Header("UI Texts for Attributes")]
    [Tooltip("TextMeshProUGUI component that displays the selected item name.")]
    public TextMeshProUGUI selectedNameText;

    [Tooltip("TextMeshProUGUI component that displays the selected item price.")]
    public TextMeshProUGUI selectedPriceText;

    [Tooltip("TextMeshProUGUI component that displays the selected item weight.")]
    public TextMeshProUGUI selectedWeightText;

    /// <summary>
    /// Sets the current selection and updates the UI texts with item attributes.
    /// </summary>
    public void SetSelection(GameObject prefab)
    {
        SelectedPrefab = prefab;
        if (prefab != null)
        {
            // Try to retrieve the attributes component.
            ItemAttributes attributes = prefab.GetComponent<ItemAttributes>();
            if (attributes != null)
            {
                if (selectedNameText != null)
                    selectedNameText.text = attributes.itemName;
                if (selectedPriceText != null)
                    selectedPriceText.text = "Price: $" + attributes.price;
                if (selectedWeightText != null)
                    selectedWeightText.text = "Weight: " + attributes.weight + "kg";
            }
            else
            {
                // Fallback if the prefab does not have an ItemAttributes component.
                if (selectedNameText != null)
                    selectedNameText.text = prefab.name;
                if (selectedPriceText != null)
                    selectedPriceText.text = "";
                if (selectedWeightText != null)
                    selectedWeightText.text = "";
            }
        }
        else
        {
            // If no prefab is selected, clear or reset the texts.
            if (selectedNameText != null)
                selectedNameText.text = "None";
            if (selectedPriceText != null)
                selectedPriceText.text = "";
            if (selectedWeightText != null)
                selectedWeightText.text = "";
        }
    }
}
