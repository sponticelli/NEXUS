using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nexus.Core.Rx.Unity
{
    public static class UnityObservables
    {
// Create an observable from UnityEvent
        public static IObservable<Unit> FromUnityEvent(UnityEvent unityEvent)
        {
            return Observable.Create<Unit>(observer =>
            {
                UnityAction handler = () => observer.OnNext(Unit.Default);
                unityEvent.AddListener(handler);
                return new Subscription(() => unityEvent.RemoveListener(handler));
            });
        }
        
        public static IObservable<long> Interval(float seconds)
        {
            return Observable.Create<long>(observer =>
            {
                var count = 0L;
                var runner = RxUnityRunner.Instance;
                var coroutine = runner.StartCoroutine(TimerRoutine());

                IEnumerator TimerRoutine()
                {
                    var wait = new WaitForSeconds(seconds);
                    while (true)
                    {
                        yield return wait;
                        observer.OnNext(count++);
                    }
                }

                return new Subscription(() => runner.StopCoroutine(coroutine));
            });
        }

        #region Input Handling

        /// <summary>
        /// Creates an observable sequence from mouse position updates
        /// </summary>
        public static IObservable<Vector2> MousePosition()
        {
            return Observable.Create<Vector2>(observer =>
            {
                var runner = RxUnityRunner.Instance;
                var coroutine = runner.StartCoroutine(TrackMouse());

                IEnumerator TrackMouse()
                {
                    while (true)
                    {
                        observer.OnNext(Input.mousePosition);
                        yield return null;
                    }
                }

                return new Subscription(() => runner.StopCoroutine(coroutine));
            });
        }

        /// <summary>
        /// Creates an observable sequence for a specific key press
        /// </summary>
        public static IObservable<Unit> KeyPress(KeyCode key)
        {
            return Observable.Create<Unit>(observer =>
            {
                var runner = RxUnityRunner.Instance;
                var coroutine = runner.StartCoroutine(TrackKey());

                IEnumerator TrackKey()
                {
                    while (true)
                    {
                        if (Input.GetKeyDown(key))
                        {
                            observer.OnNext(Unit.Default);
                        }
                        yield return null;
                    }
                }

                return new Subscription(() => runner.StopCoroutine(coroutine));
            });
        }

        #endregion

        #region UI Events

        /// <summary>
        /// Creates an observable sequence from button clicks with debounce
        /// </summary>
        public static IObservable<Unit> FromButton(Button button, float debounceTime = 0.2f)
        {
            return Observable.Create<Unit>(observer =>
            {
                float lastClickTime = -debounceTime;
                
                UnityAction handler = () =>
                {
                    float currentTime = Time.unscaledTime;
                    if (currentTime - lastClickTime >= debounceTime)
                    {
                        lastClickTime = currentTime;
                        observer.OnNext(Unit.Default);
                    }
                };

                button.onClick.AddListener(handler);
                return new Subscription(() => button.onClick.RemoveListener(handler));
            });
        }

        /// <summary>
        /// Creates an observable sequence from input field changes
        /// </summary>
        public static IObservable<string> FromInputField(InputField inputField)
        {
            return Observable.Create<string>(observer =>
            {
                UnityAction<string> handler = value => observer.OnNext(value);
                inputField.onValueChanged.AddListener(handler);
                
                // Emit initial value
                observer.OnNext(inputField.text);
                
                return new Subscription(() => inputField.onValueChanged.RemoveListener(handler));
            });
        }

        #endregion

        #region Physics and Collisions

        /// <summary>
        /// Creates an observable sequence from physics raycasts
        /// </summary>
        public static IObservable<RaycastHit> ContinuousRaycast(
            Vector3 origin, 
            Vector3 direction, 
            float maxDistance = Mathf.Infinity,
            LayerMask layerMask = default)
        {
            return Observable.Create<RaycastHit>(observer =>
            {
                var runner = RxUnityRunner.Instance;
                var coroutine = runner.StartCoroutine(PerformRaycast());

                IEnumerator PerformRaycast()
                {
                    RaycastHit hit;
                    while (true)
                    {
                        if (Physics.Raycast(origin, direction, out hit, maxDistance, layerMask))
                        {
                            observer.OnNext(hit);
                        }
                        yield return new WaitForFixedUpdate();
                    }
                }

                return new Subscription(() => runner.StopCoroutine(coroutine));
            });
        }

        /// <summary>
        /// Creates an observable sequence from overlap sphere checks
        /// </summary>
        public static IObservable<Collider[]> OverlapSphere(
            Vector3 center,
            float radius,
            LayerMask layerMask = default,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return Observable.Create<Collider[]>(observer =>
            {
                var runner = RxUnityRunner.Instance;
                var coroutine = runner.StartCoroutine(CheckOverlap());

                IEnumerator CheckOverlap()
                {
                    while (true)
                    {
                        var colliders = Physics.OverlapSphere(center, radius, layerMask, queryTriggerInteraction);
                        if (colliders.Length > 0)
                        {
                            observer.OnNext(colliders);
                        }
                        yield return new WaitForFixedUpdate();
                    }
                }

                return new Subscription(() => runner.StopCoroutine(coroutine));
            });
        }

        #endregion
        

        #region Time and Intervals

        /// <summary>
        /// Creates an observable sequence that emits values at specified intervals
        /// with support for both scaled and unscaled time
        /// </summary>
        public static IObservable<long> Interval(
            float seconds, 
            bool useUnscaledTime = false)
        {
            return Observable.Create<long>(observer =>
            {
                var runner = RxUnityRunner.Instance;
                var count = 0L;
                var coroutine = runner.StartCoroutine(EmitInterval());

                IEnumerator EmitInterval()
                {
                    
                    if (useUnscaledTime)
                    {
                        var wait =  new WaitForSecondsRealtime(seconds);
                        while (true)
                        {
                            yield return wait;
                            observer.OnNext(count++);
                        }
                    }
                    else
                    {
                        var wait =  new WaitForSeconds(seconds);
                        while (true)
                        {
                            yield return wait;
                            observer.OnNext(count++);
                        }
                    }
                }

                return new Subscription(() => runner.StopCoroutine(coroutine));
            });
        }

        /// <summary>
        /// Creates an observable sequence that completes after a specified duration
        /// </summary>
        public static IObservable<float> Timer(
            float duration,
            bool useUnscaledTime = false)
        {
            return Observable.Create<float>(observer =>
            {
                var runner = RxUnityRunner.Instance;
                var coroutine = runner.StartCoroutine(RunTimer());

                IEnumerator RunTimer()
                {
                    float startTime = useUnscaledTime ? Time.unscaledTime : Time.time;
                    float endTime = startTime + duration;

                    while (true)
                    {
                        float currentTime = useUnscaledTime ? Time.unscaledTime : Time.time;
                        float progress = (currentTime - startTime) / duration;

                        if (currentTime >= endTime)
                        {
                            observer.OnNext(1f);
                            observer.OnCompleted();
                            yield break;
                        }

                        observer.OnNext(progress);
                        yield return null;
                    }
                }

                return new Subscription(() => runner.StopCoroutine(coroutine));
            });
        }

        #endregion

        #region Scene Loading

        /// <summary>
        /// Creates an observable sequence from scene loading progress
        /// </summary>
        public static IObservable<float> LoadSceneAsync(string sceneName)
        {
            return Observable.Create<float>(observer =>
            {
                var runner = RxUnityRunner.Instance;
                var coroutine = runner.StartCoroutine(LoadScene());

                IEnumerator LoadScene()
                {
                    var operation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
                    operation.allowSceneActivation = false;

                    while (!operation.isDone)
                    {
                        observer.OnNext(operation.progress);
                        
                        if (operation.progress >= 0.9f)
                        {
                            operation.allowSceneActivation = true;
                        }
                        
                        yield return null;
                    }

                    observer.OnNext(1f);
                    observer.OnCompleted();
                }

                return new Subscription(() => runner.StopCoroutine(coroutine));
            });
        }

        #endregion

        #region Helper Components

        private class AnimationEventReceiver : MonoBehaviour
        {
            public event Action<string> OnAnimationEvent;

            // Called by Animation Events
            public void TriggerEvent(string eventName)
            {
                OnAnimationEvent?.Invoke(eventName);
            }
        }

        #endregion
    }
}