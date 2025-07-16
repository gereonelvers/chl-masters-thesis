using Hypertonic.Modules.UltimateSockets.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Hypertonic.Modules.UltimateSockets.Sockets.Stacking
{
    public class StackFillController : MonoBehaviour
    {
        [SerializeField]
        private Image _fillImage;

        [SerializeField]
        private AnimationCurve _animationCurve;

        [SerializeField]
        private float _animationDurationSeconds;

        public void AnimateTo(float fillPercentage)
        {
            TweenFill(fillPercentage);
        }

        private void TweenFill(float targetPercentage)
        {
            Tweener.TweenFloat(_fillImage.fillAmount, targetPercentage, _animationDurationSeconds, value => _fillImage.fillAmount = value, _animationCurve);
        }
    }
}
