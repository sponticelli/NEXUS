using Nexus.Core.Rx.Operators;
using UnityEngine;

namespace Nexus.Core.Rx.Unity
{
    public static class UnityRxExtensions
    {
        public static IObservable<Unit> OnDestroyAsObservable(this MonoBehaviour component)
        {
            var subject = new Subject<Unit>();
            var observer = component.gameObject.AddComponent<DestroyObserver>();
            observer.OnDestroyEvent += () => 
            {
                subject.OnNext(Unit.Default);
                subject.OnCompleted();
            };
            return subject;
        }

        public static IObservable<Collision> OnCollisionEnterAsObservable(this MonoBehaviour component)
        {
            var subject = new Subject<Collision>();
            var observer = component.gameObject.AddComponent<CollisionObserver>();
            observer.OnCollisionEnterEvent += subject.OnNext;
            // TakeUntil will complete the sequence when the component is destroyed
            return subject.TakeUntil(component.OnDestroyAsObservable());
        }
        
        public static IObservable<Collision2D> OnCollisionEnter2DAsObservable(this MonoBehaviour component)
        {
            var subject = new Subject<Collision2D>();
            var observer = component.gameObject.AddComponent<Collision2DObserver>();
            observer.OnCollision2DEnterEvent += subject.OnNext;
            // TakeUntil will complete the sequence when the component is destroyed
            return subject.TakeUntil(component.OnDestroyAsObservable());
        }

        public static IObservable<T> Debug<T>(
            this IObservable<T> source,
            string tag = null,
            bool logValues = true,
            bool logErrors = true,
            bool logCompletion = true)
        {
            return Observable.Create<T>(observer =>
            {
                string prefix = string.IsNullOrEmpty(tag) ? "Rx" : $"Rx[{tag}]";

                return source.Subscribe(
                    value =>
                    {
                        if (logValues)
                        {
                            UnityEngine.Debug.Log($"{prefix} Value: {value}");
                        }
                        observer.OnNext(value);
                    },
                    error =>
                    {
                        if (logErrors)
                        {
                            UnityEngine.Debug.LogError($"{prefix} Error: {error}");
                        }
                        observer.OnError(error);
                    },
                    () =>
                    {
                        if (logCompletion)
                        {
                            UnityEngine.Debug.Log($"{prefix} Completed");
                        }
                        observer.OnCompleted();
                    });
            });
        }

        public static IObservable<T> SubscribeToScene<T>(
            this IObservable<T> source, 
            string sceneName)
        {
            return Observable.Create<T>(observer =>
            {
                var subscription = source.Subscribe(observer);
                RxUnityRunner.Instance.RegisterSceneSubscription(sceneName, subscription);
                return subscription;
            });
        }
        
        
    }
}