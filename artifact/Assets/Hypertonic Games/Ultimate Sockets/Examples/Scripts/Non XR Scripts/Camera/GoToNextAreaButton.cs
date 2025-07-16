using System;
using UnityEngine;
using UnityEngine.UI;

namespace Hypertonic.Modules.UltimateSockets.Examples.NonXRScripts.Camera
{
    public class GoToNextAreaButton : MonoBehaviour
    {
        public static Action OnButtonClicked;

        [SerializeField]
        private Button _button;

        private void Awake()
        {
            if (_button == null)
            {
                Debug.LogError("The button is null", this);
            }
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(HandleButtonClicked);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(HandleButtonClicked);
        }

        private void HandleButtonClicked()
        {
            OnButtonClicked?.Invoke();
        }
    }
}
