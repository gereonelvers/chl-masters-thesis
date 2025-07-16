using System;
using System.Collections;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Tweening
{
    public class Tweener
    {
        public class MonoBehaviourInstance : MonoBehaviour { }

        public static MonoBehaviourInstance CoroutineRunner => _instance != null ? _instance : GenerateNewInstance();

        private static MonoBehaviourInstance _instance;
        private static MonoBehaviourInstance GenerateNewInstance()
        {
            GameObject gameObject = new GameObject("MonoBehaviourInstance");
            _instance = gameObject.AddComponent<MonoBehaviourInstance>();
            return _instance;
        }

        public bool IsTweening => _tweenCoroutine != null;

        private Coroutine _tweenCoroutine;
        private Action _onComplete;

        public static Tweener TweenFloat(float from, float to, float duration, Action<float> onUpdate, AnimationCurve curve = null, bool pingPong = false, Action onComplete = null)
        {
            return new Tweener().StartTween(from, to, duration, onUpdate, curve, pingPong, onComplete, TweenCoroutine<float>);
        }

        public static Tweener TweenVector3(Vector3 from, Vector3 to, float duration, Action<Vector3> onUpdate, AnimationCurve curve = null, bool pingPong = false, Action onComplete = null)
        {
            return new Tweener().StartTween(from, to, duration, onUpdate, curve, pingPong, onComplete, TweenCoroutine<Vector3>);
        }

        public static Tweener TweenQuaternion(Quaternion from, Quaternion to, float duration, Action<Quaternion> onUpdate, AnimationCurve curve = null, bool pingPong = false, Action onComplete = null)
        {
            return new Tweener().StartTween(from, to, duration, onUpdate, curve, pingPong, onComplete, TweenCoroutine<Quaternion>);
        }

        private Tweener StartTween<T>(T from, T to, float duration, Action<T> onUpdate, AnimationCurve curve, bool pingPong, Action onComplete, Func<T, T, float, Action<T>, AnimationCurve, IEnumerator> tweenFunc)
        {
            _onComplete = onComplete;
            _tweenCoroutine = CoroutineRunner.StartCoroutine(TweenCoroutine(from, to, duration, onUpdate, curve, pingPong, tweenFunc));
            return this;
        }

        private IEnumerator TweenCoroutine<T>(T from, T to, float duration, Action<T> onUpdate, AnimationCurve curve, bool pingPong, Func<T, T, float, Action<T>, AnimationCurve, IEnumerator> tweenFunc)
        {
            T currentFrom = from;
            T currentTo = to;

            while (true)
            {
                IEnumerator tweenEnumerator = tweenFunc(currentFrom, currentTo, duration, onUpdate, curve);
                yield return tweenEnumerator;

                if (!pingPong)
                    break;

                T temp = currentFrom;
                currentFrom = currentTo;
                currentTo = temp;
            }

            _onComplete?.Invoke();
            _tweenCoroutine = null;
        }


        private static IEnumerator TweenCoroutine<T>(T from, T to, float duration, Action<T> onUpdate, AnimationCurve curve)
        {
            float elapsedTime = 0f;
            Func<T, T, float, T> lerpFunc = GetLerpFunction<T>();

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                float t = Mathf.Clamp01(elapsedTime / duration);
                float curveValue = curve != null ? curve.Evaluate(t) : t;

                T value = lerpFunc(from, to, curveValue);

                onUpdate?.Invoke(value);

                yield return null;
            }

            onUpdate?.Invoke(to);
        }

        private static Func<T, T, float, T> GetLerpFunction<T>()
        {
            Type type = typeof(T);

            switch (type)
            {
                case Type t when t == typeof(float):
                    return (from, to, t) => (T)(object)Mathf.LerpUnclamped((float)(object)from, (float)(object)to, t);
                case Type t when t == typeof(Vector3):
                    return (from, to, t) => (T)(object)Vector3.LerpUnclamped((Vector3)(object)from, (Vector3)(object)to, t);
                case Type t when t == typeof(Quaternion):
                    return (from, to, t) => (T)(object)Quaternion.LerpUnclamped((Quaternion)(object)from, (Quaternion)(object)to, t);
                default:
                    throw new ArgumentException($"Unsupported type: {type}");
            }
        }

        public void Cancel()
        {
            if (_tweenCoroutine != null)
            {
                CoroutineRunner.StopCoroutine(_tweenCoroutine);
                _tweenCoroutine = null;
            }
        }
    }
}
