using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Text;

public class SceneNameDisplay : MonoBehaviour
{
    [Tooltip("Drag a GameObject here whose direct active children will be listed below the scene name.")]
    public GameObject childrenParent;

    private TMP_Text textMesh;

    void Start()
    {
        // Get the TMP_Text component attached to this GameObject.
        textMesh = GetComponent<TMP_Text>();
        if (textMesh == null)
        {
            Debug.LogError("SceneNameDisplayWithChildren: No TMP_Text component found on this GameObject.");
            return;
        }

        // Build the display text.
        StringBuilder displayText = new StringBuilder();

        // Get the current scene's name.
        string sceneName = SceneManager.GetActiveScene().name;
        displayText.AppendLine(sceneName);

        // If a children parent GameObject is provided, list its active direct children.
        if (childrenParent != null)
        {
            foreach (Transform child in childrenParent.transform)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    displayText.AppendLine(child.gameObject.name);
                }
            }
        }
        else
        {
            displayText.AppendLine("No children parent assigned.");
        }

        // Set the text on the TextMesh Pro component.
        textMesh.text = displayText.ToString();
    }
}
