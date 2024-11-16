using System;
using System.Collections.Generic;

namespace Nexus.Core.Rx.Operators
{
    /// <summary>
    /// only emits values when they change from the previous value.
    ///
    /// Supports default equality comparison
    /// Supports custom comparers
    /// Thread-safe operation
    /// Proper error handling
    /// Memory efficient
    /// </summary>
    /// <usage>
    /// // Basic usage
    ///     myObservable
    ///         .DistinctUntilChanged()
    ///         .Subscribe(value => HandleChange(value));
    /// 
    /// // With custom comparer
    ///     positionStream
    ///         .DistinctUntilChanged(new Vector3Comparer(0.1f))
    ///         .Subscribe(pos => UpdatePosition(pos));
    /// 
    /// // State monitoring
    ///     playerStateStream
    ///         .DistinctUntilChanged()
    ///         .Subscribe(state => UpdatePlayerState(state));
    /// </usage>
    public class DistinctUntilChangedObservable<T> : IObservable<T>
    {
        private readonly IObservable<T> source;
        private readonly IEqualityComparer<T> comparer;

        public DistinctUntilChangedObservable(
            IObservable<T> source, 
            IEqualityComparer<T> comparer = null)
        {
            this.source = source;
            this.comparer = comparer ?? EqualityComparer<T>.Default;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return source.Subscribe(new DistinctUntilChangedObserver<T>(observer, comparer));
        }
    }
}