using UnityEngine;
using TMPro;

public class InstructionManager : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Assign the TextMeshProUGUI component where instructions will be displayed.")]
    public TMP_Text instructionsText;

    [Tooltip("List of instruction strings to display.")]
    [TextArea(3, 10)]
    public string[] Instructions;

    [Header("GameObject Activation")]
    [Tooltip("Assign the GameObject whose visibility will be toggled on load.")]
    public GameObject targetGameObject;

    [Header("Activation Settings")]
    [Tooltip("Set to true to activate the target GameObject on start.")]
    public bool activateOnStart = true;

    // Start is called before the first frame update
    void Start()
    {
        // Set the instructions text
        if (instructionsText != null)
        {
            instructionsText.text = string.Join("\n", Instructions);
        }
        else
        {
            Debug.LogWarning("Instructions TextMeshPro component is not assigned.");
        }

        // Activate the target GameObject if required
        if (activateOnStart)
        {
            ActivateTargetGameObject();
        }
    }

    /// <summary>
    /// Activates the assigned GameObject by setting it active.
    /// </summary>
    public void ActivateTargetGameObject()
    {
        if (targetGameObject == null)
        {
            Debug.LogError("Target GameObject is not assigned.");
            return;
        }

        // Turn on the GameObject if it is not already active
        if (!targetGameObject.activeSelf)
        {
            targetGameObject.SetActive(true);
        }
    }
}
