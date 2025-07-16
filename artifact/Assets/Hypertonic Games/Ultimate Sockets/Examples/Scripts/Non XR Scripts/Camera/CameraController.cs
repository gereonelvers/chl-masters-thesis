using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Examples.NonXRScripts.Camera
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField]
        private List<Transform> _positions = new List<Transform>();

        private int _positionIndex = 0;

        private void OnEnable()
        {
            GoToNextAreaButton.OnButtonClicked += MoveToNextPosition;
            GoToPreviousAreaButton.OnButtonClicked += MoveToPreviousPosition;
        }

        private void OnDisable()
        {
            GoToNextAreaButton.OnButtonClicked -= MoveToNextPosition;
            GoToPreviousAreaButton.OnButtonClicked -= MoveToPreviousPosition;
        }

        private void Start()
        {
            GoToPosition(_positionIndex);
        }

        public void MoveToNextPosition()
        {
            _positionIndex++;

            if (_positionIndex >= _positions.Count)
            {
                _positionIndex = 0;
            }

            GoToPosition(_positionIndex);
        }

        public void MoveToPreviousPosition()
        {
            _positionIndex--;

            if (_positionIndex < 0)
            {
                _positionIndex = _positions.Count - 1;
            }

            GoToPosition(_positionIndex);
        }

        private void GoToPosition(int positionIndex)
        {
            if (positionIndex >= _positions.Count)
            {
                Debug.LogError("The position index is out of range");
                return;
            }

            transform.position = new Vector3(transform.position.x, transform.position.y, _positions[positionIndex].position.z);
        }
    }
}
