using UnityEngine;

public class ItemAttributes : MonoBehaviour
{
    [Header("Item Attributes")]
    public string itemName = "Big Cube";
    public float price = 3000f;  // in $
    public float weight = 1000f;  // in kg

    // Static variable to hold the running total price
    public static float TotalPrice { get; private set; }

    // When the object is created, add its price to the total.
    private void Awake()
    {
        TotalPrice += price;
    }

    // When the object is destroyed, subtract its price from the total.
    private void OnDestroy()
    {
        TotalPrice -= price;
    }
}
