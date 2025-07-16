using System.Collections;
using System.Collections.Generic;
using Hypertonic.Modules.UltimateSockets.PlaceableItems;
using Hypertonic.Modules.UltimateSockets.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Hypertonic.Modules.UltimateSockets.Highlighters
{
    public class SocketColorController : MonoBehaviour, ISocketHighlighter
    {
        [SerializeField]
        private SocketHighlighter _socketHighlighter;

        [SerializeField]
        private float _highlightTransitionDurationSeconds = 0.5f;

        [SerializeField]
        private float _unhighlightTransitionDurationSeconds = 0.4f;

        [SerializeField]
        private Image _image;

        [SerializeField]
        private Color _placeableColour = Color.cyan;

        [SerializeField]
        private Color _unplaceableColour = Color.red;

        private Color _unhighlightedColor = Color.white;

        private Tweener _fadeTween;

        private bool _lastPlaceable = false;

        private Coroutine _colourUpdateCoroutine;


        #region Unity Functions

        private void Awake()
        {
            if (_image == null)
            {
                Debug.LogError("The image compnent has not been set");
                return;
            }

            if (_socketHighlighter == null)
            {
                Debug.LogError("The socket highlighter has not been set");
                return;
            }

            _unhighlightedColor = _image.color;
        }

        #endregion Unity Functions

        #region ISocketHighlighter Implementations
        public void StartHighlight(PlaceableItem placeableItem)
        {
            StopFadeIfActive();
            StopCoroutineIfActive();

            _colourUpdateCoroutine = StartCoroutine(ColourUpdateCoroutine(placeableItem));
        }

        public void StopHighlight()
        {
            StopFadeIfActive();
            StopCoroutineIfActive();

            TweenColour(_image.color, _unhighlightedColor, _unhighlightTransitionDurationSeconds);
        }

        public void Setup(SocketHighlighter socketHighlighter)
        {
            _socketHighlighter = socketHighlighter;
        }

        #endregion ISocketHighlighter Implementations

        private IEnumerator ColourUpdateCoroutine(PlaceableItem placeableItem)
        {
            List<string> placementCriteriaNamesToIgnore = new List<string> { typeof(PlaceableItems.PlacementCriterias.NotHoldingItem).Name, typeof(Sockets.PlacementCriterias.NotHoldingItem).Name };

            bool itemIsPlaceable = _socketHighlighter.Socket.CanPlace(placeableItem, placementCriteriaNamesToIgnore);

            TweenColour(_image.color, itemIsPlaceable ? _placeableColour : _unplaceableColour, _highlightTransitionDurationSeconds);

            yield return new WaitForFixedUpdate();

            while (true)
            {
                itemIsPlaceable = _socketHighlighter.Socket.CanPlace(placeableItem, placementCriteriaNamesToIgnore);

                if (itemIsPlaceable && !_lastPlaceable)
                {
                    TweenColour(_image.color, _placeableColour, _highlightTransitionDurationSeconds);
                }
                else if (!itemIsPlaceable && _lastPlaceable)
                {
                    TweenColour(_image.color, _unplaceableColour, _highlightTransitionDurationSeconds);
                }

                _lastPlaceable = itemIsPlaceable;

                yield return new WaitForFixedUpdate();
            }
        }

        private void TweenColour(Color from, Color to, float durationSeconds)
        {
            _fadeTween?.Cancel();

            _fadeTween = Tweener.TweenFloat(0f, 1f, durationSeconds, (val) =>
            {
                _image.color = Color.Lerp(from, to, val);
            });
        }

        private void StopFadeIfActive()
        {
            if (_fadeTween != null)
            {
                _fadeTween.Cancel();
                _fadeTween = null;
            }
        }

        private void StopCoroutineIfActive()
        {
            if (_colourUpdateCoroutine != null)
            {
                StopCoroutine(_colourUpdateCoroutine);
                _colourUpdateCoroutine = null;
            }
        }
    }
}
