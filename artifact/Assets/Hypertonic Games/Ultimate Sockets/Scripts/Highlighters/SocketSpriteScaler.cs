using System.Collections;
using System.Collections.Generic;
using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using Hypertonic.Modules.UltimateSockets.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Hypertonic.Modules.UltimateSockets.Highlighters
{
    public class SocketSpriteScaler : MonoBehaviour, ISocketHighlighter
    {
        [SerializeField]
        private Image _backgroundImage;

        [SerializeField]
        private float _placeableScaleMultiplier = 1.2f;

        [SerializeField]
        private float _unplaceableScaleMultiplier = 0.3f;

        [SerializeField]
        private float _placeableScaleDurationSeconds = 1f;

        [SerializeField]
        private float _unplaceableScaleDurationSeconds = 0.1f;

        [SerializeField]
        private float _unhighlightScaleDurationSeconds = 0.2f;

        [SerializeField]
        private AnimationCurve _scaleUpAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [SerializeField]
        private AnimationCurve _scaleDownAnimationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField]
        private SocketHighlighter _socketHighlighter;

        private Tweener _scaleAnimationTweener;

        private Vector3 _defaultImageScale;

        private bool _isHighlighting = false;

        private bool _lastPlaceable = false;

        private Coroutine _scaleUpdateCoroutine;

        #region Unity Functions

        public void Awake()
        {
            if (_backgroundImage == null)
            {
                Debug.LogError("The reference to _backgroundImage has not beed set");
                return;
            }

            if (_socketHighlighter == null)
            {
                Debug.LogError("The reference to _socketHighlighter has not been set");
                return;
            }

            _defaultImageScale = _backgroundImage.transform.localScale;
        }

        #endregion Unity Functions

        #region ISocketHighlighter Implementations

        public void Setup(SocketHighlighter socketHighlighter)
        {
            _socketHighlighter = socketHighlighter;
        }

        public void StartHighlight(PlaceableItem placeableItem)
        {
            if (_isHighlighting)
                return;

            _scaleUpdateCoroutine = StartCoroutine(ScaleUpdateCoroutine(placeableItem));

            _isHighlighting = true;
        }

        public void StopHighlight()
        {
            StopScaleCoroutineIfActive();

            TweenCellSize(_backgroundImage.transform.localScale, _defaultImageScale, _unhighlightScaleDurationSeconds, _scaleDownAnimationCurve);
            _isHighlighting = false;
        }

        #endregion ISocketHighlighter Implementations

        private IEnumerator ScaleUpdateCoroutine(PlaceableItem placeableItem)
        {
            List<string> placementCriteriaNamesToIgnore = new List<string> { typeof(PlaceableItems.PlacementCriterias.NotHoldingItem).Name, typeof(Sockets.PlacementCriterias.NotHoldingItem).Name };

            bool itemIsPlaceable = _socketHighlighter.Socket.CanPlace(placeableItem, placementCriteriaNamesToIgnore);

            TweenCellSize(_backgroundImage.transform.localScale,
                itemIsPlaceable
                ? new Vector3(_placeableScaleMultiplier, _placeableScaleMultiplier, _placeableScaleMultiplier)
                : new Vector3(_unplaceableScaleMultiplier, _unplaceableScaleMultiplier, _unplaceableScaleMultiplier),
                itemIsPlaceable ? _placeableScaleDurationSeconds : _unplaceableScaleDurationSeconds,
                _scaleUpAnimationCurve,
                loop: itemIsPlaceable);

            yield return new WaitForFixedUpdate();

            while (true)
            {
                itemIsPlaceable = _socketHighlighter.Socket.CanPlace(placeableItem, placementCriteriaNamesToIgnore);

                if (itemIsPlaceable && !_lastPlaceable)
                {
                    TweenCellSize(_backgroundImage.transform.localScale,
                    new Vector3(_placeableScaleMultiplier, _placeableScaleMultiplier, _placeableScaleMultiplier),
                    _placeableScaleDurationSeconds,
                    _scaleUpAnimationCurve,
                    loop: true);
                }
                else if (!itemIsPlaceable && _lastPlaceable)
                {
                    TweenCellSize(_backgroundImage.transform.localScale,
                     new Vector3(_unplaceableScaleMultiplier, _unplaceableScaleMultiplier, _unplaceableScaleMultiplier),
                     _unplaceableScaleDurationSeconds,
                     _scaleUpAnimationCurve,
                     loop: false);
                }

                _lastPlaceable = itemIsPlaceable;

                yield return new WaitForFixedUpdate();
            }
        }


        private void TweenCellSize(Vector3 start, Vector3 target, float duration, AnimationCurve animationCurve, bool loop = false)
        {
            _scaleAnimationTweener?.Cancel();

            _scaleAnimationTweener = Tweener.TweenVector3(start, target, duration, (value) => _backgroundImage.transform.localScale = value, animationCurve, loop);
        }

        private void StopScaleCoroutineIfActive()
        {
            if (_scaleUpdateCoroutine != null)
            {
                StopCoroutine(_scaleUpdateCoroutine);
                _scaleUpdateCoroutine = null;
            }
        }
    }
}
