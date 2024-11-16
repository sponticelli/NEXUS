using System;
using System.Collections.Generic;
using Nexus.Core.Rx.Operators;

namespace Nexus.Core.Rx
{
    public static class ReactiveOperators
    {
        // Transform values
        public static IObservable<TResult> Select<TSource, TResult>(
            this IObservable<TSource> source,
            Func<TSource, TResult> selector)
        {
            return new SelectObservable<TSource, TResult>(source, selector);
        }

        // Filter values
        public static IObservable<T> Where<T>(
            this IObservable<T> source,
            Func<T, bool> predicate)
        {
            return new WhereObservable<T>(source, predicate);
        }

        // Combine latest values from two observables
        public static IObservable<TResult> CombineLatest<T1, T2, TResult>(
            this IObservable<T1> first,
            IObservable<T2> second,
            Func<T1, T2, TResult> resultSelector)
        {
            return new CombineLatestObservable<T1, T2, TResult>(first, second, resultSelector);
        }

        // Throttle emissions
        public static IObservable<T> Throttle<T>(
            this IObservable<T> source,
            float seconds)
        {
            return new ThrottleObservable<T>(source, seconds);
        }

        // Buffer values over time
        public static IObservable<List<T>> Buffer<T>(
            this IObservable<T> source,
            float seconds)
        {
            return new BufferObservable<T>(source, seconds);
        }

        // Distinct consecutive values
        public static IObservable<T> DistinctUntilChanged<T>(
            this IObservable<T> source)
        {
            return new DistinctUntilChangedObservable<T>(source);
        }

        // Delay emissions
        public static IObservable<T> Delay<T>(
            this IObservable<T> source,
            float seconds)
        {
            return new DelayObservable<T>(source, seconds);
        }

        // Take first n items
        public static IObservable<T> Take<T>(
            this IObservable<T> source,
            int count)
        {
            return new TakeObservable<T>(source, count);
        }

        // Skip first n items
        public static IObservable<T> Skip<T>(
            this IObservable<T> source,
            int count)
        {
            return new SkipObservable<T>(source, count);
        }
        
        public static IObservable<TSource> TakeUntil<TSource, TOther>(
            this IObservable<TSource> source,
            IObservable<TOther> other)
        {
            return new TakeUntilObservable<TSource, TOther>(source, other);
        }
    }
}