using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Examples
{
    public class FacePlayer : MonoBehaviour
    {
        private void LateUpdate()
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up);
        }
    }
}
