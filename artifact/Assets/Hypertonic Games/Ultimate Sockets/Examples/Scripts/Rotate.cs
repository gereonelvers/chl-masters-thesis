using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Examples
{
    public class Rotate : MonoBehaviour
    {
        [SerializeField]
        private float _speed = 360f;

        private void Update()
        {
            transform.Rotate(Vector3.up * _speed * Time.deltaTime, Space.Self);
        }
    }
}
