using System;

namespace Nexus.Core.Rx.Operators
{
    /// <summary>
    /// Take the first n elements from an observable
    /// Takes first N items
    /// Completes after N items
    /// </summary>
    /// <usage>
    /// // Basic usage
    ///     source
    ///         .Take(5)
    ///         .Subscribe(value => HandleValue(value));
    /// 
    /// // Combo system
    ///     hitEvents
    ///         .Take(3)
    ///         .Subscribe(
    ///             hit => AddToCombo(hit),
    ///             () => ExecuteCombo()
    ///         );
    /// 
    /// // Limited collection
    ///     collectibles
    ///         .Take(10)
    ///         .Subscribe(
    ///             item => AddToInventory(item),
    ///             () => InventoryFull()
    ///         );
    /// </usage>
    public class TakeObservable<T> : IObservable<T>
    {
        private readonly IObservable<T> source;
        private readonly int count;

        public TakeObservable(IObservable<T> source, int count)
        {
            this.source = source;
            this.count = count;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return source.Subscribe(new TakeObserver<T>(observer, count));
        }
    }
}