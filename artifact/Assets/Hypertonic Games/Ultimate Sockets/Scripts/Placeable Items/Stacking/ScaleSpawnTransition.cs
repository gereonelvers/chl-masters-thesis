using Hypertonic.Modules.UltimateSockets.Sockets;
using Hypertonic.Modules.UltimateSockets.Tweening;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.PlaceableItems.Stacking
{
    public class ScaleSpawnTransition : MonoBehaviour, IStackSpawnTransition
    {
        [SerializeField]
        private Vector3 _startingScale;

        [SerializeField]
        private float _scaleTransitionTimeSeconds = 1f;

        [SerializeField]
        private AnimationCurve _animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private bool _isSpawning = false;

        public bool IsSpawning() => _isSpawning;

        private void OnEnable()
        {
            _isSpawning = false;
        }

        public void Spawn(Socket socket, PlaceableItem placeableItem)
        {
            _isSpawning = true;

            ScaleUp(placeableItem);
        }

        private void ScaleUp(PlaceableItem placeableItem)
        {
            Vector3 targetScale = placeableItem.RootTransform.localScale;

            Tweener.TweenVector3(_startingScale, targetScale, _scaleTransitionTimeSeconds, (scale) =>
            {
                if (placeableItem == null)
                {
                    _isSpawning = false;
                    return;
                }

                placeableItem.RootTransform.localScale = scale;
            },
            _animationCurve,
            onComplete: () => { _isSpawning = false; }
            );
        }
    }
}
