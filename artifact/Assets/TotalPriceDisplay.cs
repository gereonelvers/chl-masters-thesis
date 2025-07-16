using UnityEngine;
using TMPro;

public class TotalPriceDisplay : MonoBehaviour
{
    public TextMeshProUGUI totalPriceText; // Drag your TextMeshProUGUI component here via the Inspector

    void Update()
    {
        totalPriceText.text = $"Current Price: {ItemAttributes.TotalPrice}";
    }
}
