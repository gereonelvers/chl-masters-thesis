using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ExpandOnTrigger : MonoBehaviour
{
    // The target scale when the object is triggered
    public Vector3 expandedScale = new Vector3(0.06f, 0.12f, 0.02f);

    // Duration of the scaling animation (in seconds)
    public float scaleDuration = 0.1f;

    // Stores the original scale of the GameObject
    private Vector3 originalScale;

    // A reference to the current scaling coroutine so we can stop it if needed
    private Coroutine scaleCoroutine;

    [Header("Trigger Filtering")]
    [Tooltip("Only trigger if the colliding object's name is in this list. Leave empty to trigger on any object.")]
    public List<string> validTriggerObjectNames = new List<string>();

    private void Start()
    {
        // Record the original scale at startup
        originalScale = transform.localScale;
    }

    // This coroutine handles the smooth scaling animation
    private IEnumerator ScaleOverTime(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float time = 0f;
        while (time < scaleDuration)
        {
            // Lerp from the starting scale to the target scale over the specified duration
            transform.localScale = Vector3.Lerp(startScale, targetScale, time / scaleDuration);
            time += Time.deltaTime;
            yield return null;
        }
        // Ensure the scale is set exactly to the target at the end
        transform.localScale = targetScale;
    }

    // Called when another collider enters this trigger
    private void OnTriggerEnter(Collider other)
    {
        // If the list is not empty, only trigger if the colliding object's name is in the list.
        if (validTriggerObjectNames.Count > 0 && !validTriggerObjectNames.Contains(other.gameObject.name))
        {
            return;
        }

        // Stop any ongoing scaling animation before starting a new one
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        // Start scaling to the expanded scale
        scaleCoroutine = StartCoroutine(ScaleOverTime(expandedScale));
    }

    // Called when another collider exits this trigger
    private void OnTriggerExit(Collider other)
    {
        // If the list is not empty, only trigger if the colliding object's name is in the list.
        if (validTriggerObjectNames.Count > 0 && !validTriggerObjectNames.Contains(other.gameObject.name))
        {
            return;
        }

        // Stop any ongoing scaling animation before starting a new one
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        // Start scaling back to the original scale
        scaleCoroutine = StartCoroutine(ScaleOverTime(originalScale));
    }
}
