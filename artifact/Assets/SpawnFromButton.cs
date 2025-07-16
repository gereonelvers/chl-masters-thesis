using System.Collections;
using UnityEngine;
using Photon.Pun;
using TMPro;
using FishNet;
using FishNet.Object;
using FishNet.Connection;

public class SpawnFromButton : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Optional: A transform that defines where the object should spawn. If left empty, spawns at this object's position.")]
    public Transform spawnLocation;

    [Tooltip("Optional: A transform that the spawned object will be parented to.")]
    public Transform spawnParent;

    [Tooltip("Cooldown time (in seconds) between spawns.")]
    public float spawnCooldown = 1f;

    [Header("UI Feedback Settings")]
    [Tooltip("TextMeshProUGUI component that displays spawn zone feedback.")]
    public TextMeshProUGUI cooldownText;

    [Tooltip("Default text when ready to spawn.")]
    public string idleText = "Spawn";

    [Tooltip("Interval (in seconds) at which the cooldown text is updated.")]
    public float cooldownTextUpdateInterval = 0.25f;

    [Header("Hold-to-Spawn Settings")]
    [Tooltip("Required hold time (in seconds) in the spawn zone.")]
    public float requiredHoldTime = 0.5f; // Shorter than selection hold time

    [Tooltip("Interval (in seconds) for updating the hold countdown.")]
    public float holdTextUpdateInterval = 0.1f;

    [Header("Trigger Filtering")]
    [Tooltip("Only trigger if the colliding object has this name. Leave empty for any object.")]
    public string validTriggerObjectName = "";

    [Header("Selection Manager")]
    [Tooltip("Reference to the SelectionManager to retrieve the currently selected prefab.")]
    public SelectionManager selectionManager;

    private float lastSpawnTime = -Mathf.Infinity;
    private static int objectCounter = 0;

    private MeshRenderer meshRenderer;
    private Coroutine cooldownCoroutine;
    private Coroutine holdCoroutine;

    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (cooldownText != null)
        {
            cooldownText.text = idleText;
        }
    }

    private void OnDisable()
    {
        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }
        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = null;
        }
    }

    private void OnEnable()
    {
        float timeSinceLastSpawn = Time.time - lastSpawnTime;
        if (timeSinceLastSpawn < spawnCooldown)
        {
            float remainingCooldown = spawnCooldown - timeSinceLastSpawn;
            cooldownCoroutine = StartCoroutine(CooldownFeedback(remainingCooldown));
        }
        else
        {
            if (cooldownText != null)
            {
                cooldownText.text = idleText;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Filter out triggers that don't match the valid object name (if specified).
        if (!string.IsNullOrEmpty(validTriggerObjectName) && other.gameObject.name != validTriggerObjectName)
        {
            return;
        }

        // Start the hold-to-spawn if not in cooldown.
        if (Time.time - lastSpawnTime >= spawnCooldown && holdCoroutine == null)
        {
            holdCoroutine = StartCoroutine(WaitForHold());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Filter out triggers that don't match the valid object name (if specified).
        if (!string.IsNullOrEmpty(validTriggerObjectName) && other.gameObject.name != validTriggerObjectName)
        {
            return;
        }

        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
            if (cooldownText != null)
            {
                cooldownText.text = idleText;
            }
        }
    }

    private IEnumerator WaitForHold()
    {
        float timer = 0f;
        while (timer < requiredHoldTime)
        {
            float remaining = requiredHoldTime - timer;
            if (cooldownText != null)
            {
                cooldownText.text = remaining.ToString("0.0");
            }
            yield return new WaitForSeconds(holdTextUpdateInterval);
            timer += holdTextUpdateInterval;
        }

        // Once held long enough, spawn the currently selected object.
        lastSpawnTime = Time.time;
        SpawnObject();

        // Start cooldown.
        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
        }
        cooldownCoroutine = StartCoroutine(CooldownFeedback(spawnCooldown));
        holdCoroutine = null;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnOnServer(GameObject selectedPrefab, Vector3 spawnPos, Quaternion spawnRot, NetworkConnection conn = null)
    {
        // The server calls the observers RPC so that all clients update their visibility.
        GameObject spawnedObject = Instantiate(selectedPrefab, spawnPos, spawnRot);
        InstanceFinder.ServerManager.Spawn(spawnedObject, InstanceFinder.ClientManager.Connection);
        spawnedObject.name = $"{selectedPrefab.name}_{objectCounter}";
    }

    private void SpawnObject()
    {
        // Retrieve the currently selected prefab from the provided SelectionManager.
        GameObject selectedPrefab = selectionManager != null ? selectionManager.SelectedPrefab : null;
        if (selectedPrefab != null)
        {
            Vector3 spawnPos = spawnLocation ? spawnLocation.position : transform.position;
            Quaternion spawnRot = spawnLocation ? spawnLocation.rotation : transform.rotation;

            // Instantiate the selected object using PhotonNetwork.
            //GameObject spawnedObject = PhotonNetwork.Instantiate(selectedPrefab.name, spawnPos, spawnRot);
            NetworkedLogger.Instance.Log($"Spawned {selectedPrefab.name} at {spawnPos} / {spawnRot}");
            SpawnOnServer(selectedPrefab, spawnPos, spawnRot);
            objectCounter++;

            //if (spawnParent != null)
            //{
            //    spawnedObject.transform.SetParent(spawnParent);
            //}
        }
        else
        {
            NetworkedLogger.Instance.Log("SpawnFromButton: No object has been selected to spawn!");
        }
    }

    private IEnumerator CooldownFeedback(float initialRemainingTime)
    {
        float remaining = initialRemainingTime;
        while (remaining > 0f)
        {
            if (cooldownText != null)
            {
                float displayTime = Mathf.Ceil(remaining / cooldownTextUpdateInterval) * cooldownTextUpdateInterval;
                cooldownText.text = displayTime.ToString("0.00");
            }
            yield return new WaitForSeconds(cooldownTextUpdateInterval);
            remaining -= cooldownTextUpdateInterval;
        }
        if (cooldownText != null)
        {
            cooldownText.text = idleText;
        }
    }

    // Exposed method to spawn an object from a UI button press.
    public void Spawn()
    {
        if (Time.time - lastSpawnTime >= spawnCooldown)
        {
            lastSpawnTime = Time.time;
            SpawnObject();

            if (cooldownCoroutine != null)
            {
                StopCoroutine(cooldownCoroutine);
            }
            cooldownCoroutine = StartCoroutine(CooldownFeedback(spawnCooldown));
        }
        else
        {
            NetworkedLogger.Instance.Log("SpawnFromButton: Still in cooldown period.");
        }
    }
}