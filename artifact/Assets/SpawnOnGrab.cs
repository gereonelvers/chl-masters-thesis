using System.Collections;
using UnityEngine;
using Photon.Pun;
using TMPro; // Make sure you have the TextMeshPro package imported

public class SpawnOnTouch : MonoBehaviourPunCallbacks
{
    [Header("Cube Spawn Settings")]
    [Tooltip("The networked prefab of the cube to spawn (ensure it's registered with Photon).")]
    public GameObject cubePrefab;

    [Tooltip("Optional: A transform that defines where the cube should spawn. If left empty, spawns at this object's position.")]
    public Transform spawnLocation;

    [Tooltip("Optional: A transform that the spawned cube will be parented to.")]
    public Transform spawnParent;

    [Tooltip("Cooldown time in seconds between spawns to prevent multiple spawns on rapid touches.")]
    public float spawnCooldown = 1f;

    [Header("UI Feedback Settings")]
    [Tooltip("The material used when the spawn is available.")]
    public Material activeMaterial;

    [Tooltip("The material used during cooldown (e.g., a greyed-out version).")]
    public Material cooldownMaterial;

    [Tooltip("Optional: A TextMeshPro object that displays feedback. When not in cooldown or hold, it will show the idle text.")]
    public TextMeshPro cooldownText;

    [Tooltip("The default text to display when the cube can be spawned (e.g., \"Cube\").")]
    public string idleText = "Cube";

    [Tooltip("Interval (in seconds) at which the cooldown text is updated.")]
    public float cooldownTextUpdateInterval = 0.25f;

    [Header("Hold-to-Spawn Settings")]
    [Tooltip("The required time (in seconds) that the user must continue touching the object to spawn a cube.")]
    public float requiredHoldTime = 1f;

    [Tooltip("Interval (in seconds) at which the hold countdown text is updated (always 0.1s increments).")]
    public float holdTextUpdateInterval = 0.1f;

    // Tracks the time of the last spawn.
    private float lastSpawnTime = -Mathf.Infinity;

    private static int cubeCounter = 0;


    // Cache the MeshRenderer (on the same GameObject) so we can change its material.
    private MeshRenderer meshRenderer;

    // Coroutine handles for managing cooldown and hold timers.
    private Coroutine cooldownCoroutine;
    private Coroutine holdCoroutine;

    private void Start()
    {
        // Cache the MeshRenderer on this GameObject.
        meshRenderer = GetComponent<MeshRenderer>();

        // If activeMaterial is not set in the Inspector, default to the MeshRenderer's material.
        if (activeMaterial == null && meshRenderer != null)
        {
            activeMaterial = meshRenderer.material;
        }

        // Initialize the text to the idle state.
        if (cooldownText != null)
        {
            cooldownText.text = idleText;
        }
    }

    private void OnDisable()
    {
        //Debug.Log("SpawnOnTouch: OnDisable called. Stopping any active coroutines.");

        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
            //Debug.Log("SpawnOnTouch: holdCoroutine stopped.");
        }
        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = null;
            //Debug.Log("SpawnOnTouch: cooldownCoroutine stopped.");
        }
    }

    private void OnEnable()
    {
        //Debug.Log("SpawnOnTouch: OnEnable called.");
        float timeSinceLastSpawn = Time.time - lastSpawnTime;
        if (timeSinceLastSpawn < spawnCooldown)
        {
            float remainingCooldown = spawnCooldown - timeSinceLastSpawn;
            //Debug.Log("SpawnOnTouch: Resuming cooldown with " + remainingCooldown + " seconds remaining.");
            cooldownCoroutine = StartCoroutine(CooldownFeedback(remainingCooldown));
        }
        else
        {
            // Reset the UI and material to the idle state.
            if (cooldownText != null)
            {
                cooldownText.text = idleText;
            }
            if (meshRenderer != null && activeMaterial != null)
            {
                meshRenderer.material = activeMaterial;
            }
            //Debug.Log("SpawnOnTouch: No cooldown active. UI and material reset to idle state.");
        }
    }

    // When something enters the trigger.
    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("Trigger entered by: " + other.gameObject.name + " with tag: " + other.gameObject.tag);
        if (other.gameObject.name != "AttachTransform")
            return;
        // Optional: Filter by tag if necessary.
        // if (!other.CompareTag("Player")) return;

        // Only start the hold countdown if we're not in cooldown.
        if (Time.time - lastSpawnTime >= spawnCooldown)
        {
            // Start the hold countdown coroutine if it's not already running.
            if (holdCoroutine == null)
            {
                //Debug.Log("SpawnOnTouch: Starting hold-to-spawn coroutine.");
                holdCoroutine = StartCoroutine(WaitForHold());
            }
        }
    }

    // When something exits the trigger.
    private void OnTriggerExit(Collider other)
    {
        // If the user stops touching before the required hold time, cancel the hold.
        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
            //Debug.Log("SpawnOnTouch: User stopped touching before required hold time. Hold coroutine cancelled.");

            // Reset the text to the idle text.
            if (cooldownText != null)
            {
                cooldownText.text = idleText;
            }
        }
    }

    // Coroutine to handle the hold-to-spawn mechanism.
    private IEnumerator WaitForHold()
    {
        float timer = 0f;
        while (timer < requiredHoldTime)
        {
            float remainingHoldTime = requiredHoldTime - timer;
            if (cooldownText != null)
            {
                // Update the text to show the remaining hold time in 0.1s increments.
                cooldownText.text = remainingHoldTime.ToString("0.0");
            }
            yield return new WaitForSeconds(holdTextUpdateInterval);
            timer += holdTextUpdateInterval;
        }

        //Debug.Log("SpawnOnTouch: Required hold time reached. Spawning cube.");
        // Once the required hold time is reached, spawn the cube.
        lastSpawnTime = Time.time;
        SpawnCube();

        // Start the cooldown feedback.
        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
            //Debug.Log("SpawnOnTouch: Previous cooldown coroutine stopped before starting new one.");
        }
        cooldownCoroutine = StartCoroutine(CooldownFeedback(spawnCooldown));

        // Clear the hold coroutine handle.
        holdCoroutine = null;
    }

    private void SpawnCube()
    {
        if (cubePrefab != null)
        {
            // Determine spawn position and rotation.
            Vector3 spawnPos = spawnLocation ? spawnLocation.position : transform.position;
            Quaternion spawnRot = spawnLocation ? spawnLocation.rotation : transform.rotation;

            // Instantiate the cube using PhotonNetwork.
            GameObject spawnedCube = PhotonNetwork.Instantiate(cubePrefab.name, spawnPos, spawnRot);

            // Increment the counter and update the name.
            cubeCounter++;
            spawnedCube.name = $"{cubePrefab.name}_{cubeCounter}";

            // Parent the spawned cube if a parent transform is specified.
            if (spawnParent != null)
            {
                spawnedCube.transform.SetParent(spawnParent);
            }

            // (Optional) Set up any additional components on the spawned cube.
            var corrector = spawnedCube.GetComponent<PhysicsObjectCorrector>();
            if (corrector != null)
            {
                corrector.planeTransform = spawnParent;
            }
            else
            {
                // Debug.LogWarning("SpawnOnTouch: Spawned cube does not have a PhysicsObjectCorrector component!");
            }
        }
        else
        {
            // Debug.LogWarning("SpawnOnTouch: Cube prefab is not assigned in SpawnOnTouch on " + gameObject.name);
        }
    }


    // Modified CooldownFeedback that accepts an initial remaining cooldown time.
    private IEnumerator CooldownFeedback(float initialRemainingTime)
    {
        // Change to the cooldown (greyed-out) material if one is provided.
        if (meshRenderer != null && cooldownMaterial != null)
        {
            meshRenderer.material = cooldownMaterial;
        }

        float remaining = initialRemainingTime;
        //Debug.Log("SpawnOnTouch: Cooldown started for " + remaining + " seconds.");
        while (remaining > 0f)
        {
            if (cooldownText != null)
            {
                // Update the cooldown text using the specified update interval.
                float displayTime = Mathf.Ceil(remaining / cooldownTextUpdateInterval) * cooldownTextUpdateInterval;
                cooldownText.text = displayTime.ToString("0.00");
            }
            yield return new WaitForSeconds(cooldownTextUpdateInterval);
            remaining -= cooldownTextUpdateInterval;
        }

        // Reset the UI and material to the idle state.
        if (cooldownText != null)
        {
            cooldownText.text = idleText;
        }
        if (meshRenderer != null && activeMaterial != null)
        {
            meshRenderer.material = activeMaterial;
        }
        //Debug.Log("SpawnOnTouch: Cooldown ended. UI and material reset to idle state.");
    }

}

