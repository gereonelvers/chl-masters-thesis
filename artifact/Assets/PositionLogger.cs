using UnityEngine;
using System.Collections;

public class PositionLogger : MonoBehaviour
{
    void Start()
    {
        // Start the coroutine that logs position and rotation every second.
        StartCoroutine(LogPositionRotation());
    }

    IEnumerator LogPositionRotation()
    {
        while (true)
        {
            // Log the current position and rotation (in Euler angles).
            NetworkedLogger.Instance.Log("[PositionLogger] P: " + transform.position + " | R: " + transform.rotation.eulerAngles);
            // Wait for 1 second before the next log.
            yield return new WaitForSeconds(1f);
        }
    }
}
