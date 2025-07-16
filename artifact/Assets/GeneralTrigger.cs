using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class GeneralTrigger : MonoBehaviour
{
    // Specific tag allowed to trigger the events (optional).
    public string triggerTag = "";
    // Specific GameObject name allowed to trigger the events (optional).
    public string triggerName = "";

    // Callbacks to execute when a valid collider enters/exits the trigger.
    public UnityEvent onTriggerEnterCallback;
    public UnityEvent onTriggerExitCallback;

    // Cooldown time in seconds before the trigger can fire again.
    public float triggerCooldown = 5f;

    // Tracks the last time a trigger event was fired.
    private float lastTriggerTime = -Mathf.Infinity;

    // Reference to the currently running countdown coroutine.
    private Coroutine countdownCoroutine;

    // Checks whether the cooldown period has elapsed.
    private bool CanTrigger()
    {
        return Time.time - lastTriggerTime >= triggerCooldown;
    }

    // Determines if the collider is valid based on tag or name.
    private bool IsValidCollider(Collider other)
    {
        // If both triggerTag and triggerName are empty, allow all colliders.
        if (string.IsNullOrEmpty(triggerTag) && string.IsNullOrEmpty(triggerName))
            return true;

        bool valid = false;
        if (!string.IsNullOrEmpty(triggerTag) && other.CompareTag(triggerTag))
            valid = true;
        if (!string.IsNullOrEmpty(triggerName) && other.gameObject.name.Equals(triggerName))
            valid = true;

        return valid;
    }

    // Logs the countdown every 0.25 seconds.
    private IEnumerator CountdownCoroutine()
    {
        float remaining = triggerCooldown;
        while (remaining > 0f)
        {
            Debug.Log($"{gameObject.name} countdown: {remaining:F2} seconds remaining");
            yield return new WaitForSeconds(0.25f);
            remaining -= 0.25f;
        }
        Debug.Log($"{gameObject.name} countdown: 0.00 seconds remaining");
    }

    // Called when any collider enters this trigger.
    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("Triggered by " + other.gameObject.name);
        if (!IsValidCollider(other))
            return;

        if (CanTrigger())
        {
            lastTriggerTime = Time.time;
            onTriggerEnterCallback?.Invoke();

            // Stop any ongoing countdown before starting a new one.
            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
            }
            countdownCoroutine = StartCoroutine(CountdownCoroutine());
        }
    }

    // Called when any collider exits this trigger.
    private void OnTriggerExit(Collider other)
    {
        if (!IsValidCollider(other))
            return;

        if (CanTrigger())
        {
            lastTriggerTime = Time.time;
            onTriggerExitCallback?.Invoke();

            // Stop any ongoing countdown before starting a new one.
            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
            }
            countdownCoroutine = StartCoroutine(CountdownCoroutine());
        }
    }
}
