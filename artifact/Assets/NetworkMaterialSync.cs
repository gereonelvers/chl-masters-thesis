using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class NetworkMaterialSync : NetworkBehaviour
{
    [SerializeField]
    private Renderer targetRenderer;

    [SerializeField]
    private Material[] materials;

    private readonly SyncVar<int> currentMaterialIndex = new SyncVar<int>();

    void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        currentMaterialIndex.OnChange += OnMaterialChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateMaterial(currentMaterialIndex.Value);
    }

    // Call this from client-side to request material change
    public void RequestMaterialChange(int materialIndex)
    {
        if (!IsOwner)
            return;

        ServerChangeMaterial(materialIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerChangeMaterial(int materialIndex)
    {
        if (materialIndex < 0 || materialIndex >= materials.Length)
            return;

        currentMaterialIndex.Value = materialIndex;
    }

    // Callback executed whenever SyncVar changes
    private void OnMaterialChanged(int prev, int next, bool asServer)
    {
        UpdateMaterial(next);
    }

    private void UpdateMaterial(int index)
    {
        if (index >= 0 && index < materials.Length)
        {
            targetRenderer.material = Instantiate(materials[index]);
        }
    }
}
