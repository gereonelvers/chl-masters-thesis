using TMPro;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Sockets.Stacking
{
    public class StackItemCountDisplayController : MonoBehaviour
    {
        [SerializeField]
        private Socket _socket;

        [SerializeField]
        private CanvasGroup _countDisplayCanvasGroup;

        [SerializeField]
        private StackFillController _stackFillController;

        [SerializeField]
        private TextMeshProUGUI _itemCountText;

        [SerializeField]
        private bool _useStackFillController = true;

        #region Unity Events

        private void Awake()
        {
            if (_socket == null)
            {
                Debug.LogErrorFormat(this, "No reference to the _socket variable has been set");
                return;
            }

            if (_countDisplayCanvasGroup == null)
            {
                Debug.LogErrorFormat(this, "No reference to the _countDisplayCanvasGroup variable has been set");
                return;
            }

            if (_stackFillController == null)
            {
                Debug.LogErrorFormat(this, "No reference to the _stackFillController variable has been set");
                return;
            }

            _countDisplayCanvasGroup.alpha = 0;
        }

        private void OnEnable()
        {
            _socket.StackableItemController.OnStackSizeChanged += HandleStackSizeChanged;
        }

        private void OnDisable()
        {
            _socket.StackableItemController.OnStackSizeChanged -= HandleStackSizeChanged;
        }

        #endregion Unity Events

        #region Public Function

        public void SetSocket(Socket socket)
        {
            _socket = socket;
        }

        #endregion Public Function

        #region Private Functions

        private void HandleStackSizeChanged()
        {
            UpdateCountUI();
        }

        private void UpdateCountUI()
        {
            if (!_socket.StackableItemController.Settings.Stackable || _socket.StackableItemController.StackSize == 0)
            {
                _countDisplayCanvasGroup.alpha = 0;
            }
            else
            {
                _countDisplayCanvasGroup.alpha = 1;
            }

            _itemCountText.text = _socket.StackableItemController.StackSize.ToString();

            float maxStackSize = _socket.StackableItemController.Settings.MaxStackSize;
            float fillPercentage = _socket.StackableItemController.StackSize / maxStackSize;

            if (_useStackFillController)
            {
                _stackFillController.AnimateTo(fillPercentage);
            }
        }

        #endregion Private Functions
    }
}
