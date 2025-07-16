using UnityEngine;
using UnityEngine.EventSystems;
using FishNet.Object;
using System.Collections;

public class Trashable : NetworkBehaviour, IPointerDownHandler, IPointerUpHandler
{
    // Audio settings for the destruction sound
    public AudioClip destroySoundEffect; // Assign this in the Inspector.
    public float soundVolume = 1.0f;

    // Flag to track if the cube is currently being held by a player
    private bool isBeingHeld = false;

    // Flag to track if the cube is currently overlapping the trashcan
    private bool isInTrashZone = false;

    // Reference to the trash countdown coroutine
    private Coroutine trashCountdownCoroutine;

    // Called when the pointer (e.g., a hand ray or mouse) is pressed down on this cube.
    public void OnPointerDown(PointerEventData eventData)
    {
        isBeingHeld = true;
        // Debug.Log($"{gameObject.name}: Pointer Down - Object being held");
    }

    // Called when the pointer is released from this cube.
    public void OnPointerUp(PointerEventData eventData)
    {
        isBeingHeld = false;
        // Debug.Log($"{gameObject.name}: Pointer Up - Object released");
    }

    // Detect when the cube enters a trigger zone.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Trashcan"))
        {
            isInTrashZone = true;
            Debug.Log($"{gameObject.name}: Entered Trashcan zone.");

            if (base.IsServer)
            {
                // If a countdown is already running, cancel it.
                if (trashCountdownCoroutine != null)
                {
                    StopCoroutine(trashCountdownCoroutine);
                }
                trashCountdownCoroutine = StartCoroutine(TrashCountdown());
            }
        }
    }

    // Detect when the cube exits a trigger zone.
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Trashcan"))
        {
            isInTrashZone = false;
            Debug.Log($"{gameObject.name}: Exited Trashcan zone.");

            if (base.IsServer && trashCountdownCoroutine != null)
            {
                StopCoroutine(trashCountdownCoroutine);
                trashCountdownCoroutine = null;
            }
        }
    }

    // Coroutine that waits for 5 seconds before destroying the object if it remains in the trash zone.
    private IEnumerator TrashCountdown()
    {
        float timer = 0f;
        while (timer < 5f)
        {
            if (!isInTrashZone)
            {
                yield break; // Exit early if the object leaves the trash zone.
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // After 5 seconds, if still in the trash zone, destroy the object.
        if (isInTrashZone)
        {
            Debug.Log($"{gameObject.name}: In Trash Zone for 5 seconds. Destroying the cube.");
            DestroyObject();
        }
    }

    // Server RPC to handle the destruction request from clients
    [ServerRpc(RequireOwnership = false)]
    private void DestroyObjectServerRpc()
    {
        NetworkedLogger.Instance.Log("[Trashable] Destorying " + gameObject.name);
        DestroyObject();
    }

    // Common destruction logic
    private void DestroyObject()
    {
        // Only the server should actually despawn objects.
        if (base.IsServer)
        {
            // Trigger sound effect on all clients before despawning.
            PlaySoundEffectRpc();

            NetworkObject netObj = gameObject.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Despawn();
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: Cannot destroy object because it does not have a NetworkObject component.");
            }
        }
    }

    // Observers RPC to play the sound effect for all players.
    [ObserversRpc]
    private void PlaySoundEffectRpc()
    {
        if (destroySoundEffect != null)
        {
            // Play the sound at the object's position, creating a temporary audio source.
            AudioSource.PlayClipAtPoint(destroySoundEffect, transform.position, soundVolume);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: Destroy sound effect is not assigned.");
        }
    }
}
