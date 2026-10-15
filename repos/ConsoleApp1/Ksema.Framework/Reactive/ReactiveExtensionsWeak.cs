using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Disposables;

namespace System.Linq
{
    public static class ReactiveExtensionsWeak
    {
        /// <summary>
        /// Returns an observable sequence that contains the values
        /// of the underlying .NET event, does not hold the strong
        /// reference to the subscribing observers and automatically
        /// unsubscribes from the event when the subscription (returned
        /// <see cref="IDisposable"/>) is garbage collected.</summary>
        /// <remarks>
        /// If unsubscription is invoked during garbage collection,
        /// it be invoked in the finalizer thread.</remarks>
        public static IObservable<IEvent<TEventArgs>> FromWeakEventAuto<TEventArgs>(
          Action<EventHandler<TEventArgs>> addHandler,
          Action<EventHandler<TEventArgs>> removeHandler)
          where TEventArgs : EventArgs
        {
            if (addHandler == null)
                throw new ArgumentNullException("addHandler");
            if (removeHandler == null)
                throw new ArgumentNullException("removeHandler");

            return Observable.CreateWithDisposable<IEvent<TEventArgs>>(ob =>
            {
                // do not hold the strong reference
                var wr = new WeakReference(ob);

                EventHandler<TEventArgs> handler = (sender, args) =>
                {
                    // check the observer alive each event invokation
                    var o = (IObserver<IEvent<TEventArgs>>)wr.Target;
                    if (o != null)
                    {
                        o.OnNext(Event.Create(sender, args));
                    }
                };

                addHandler(handler); // connect the handler

                // return the disposable which will unsubscribe at GC
                return new FinalizerDisposable(() => removeHandler(handler));
            });
        }

        /// <summary>
        /// Returns an observable sequence that contains the values
        /// of the underlying .NET event and does not hold the strong
        /// reference to the subscribing observers. If the subscription
        /// and observer is garbage collected and event fires,
        /// the handler will be automatically unsubscribed.</summary>
        /// <remarks>
        /// If observer will be garbage collected and event never fires
        /// again the subscription handler will consume a bit of memory.</remarks>
        public static IObservable<IEvent<TEventArgs>> FromWeakEvent<TEventArgs>(
          Action<EventHandler<TEventArgs>> addHandler,
          Action<EventHandler<TEventArgs>> removeHandler)
          where TEventArgs : EventArgs
        {
            if (addHandler == null)
                throw new ArgumentNullException("addHandler");
            if (removeHandler == null)
                throw new ArgumentNullException("removeHandler");

            return Observable.Create<IEvent<TEventArgs>>(ob =>
            {
                // do not hold the strong reference
                var wr = new WeakReference(ob);

                EventHandler<TEventArgs> handler = null;
                handler = (sender, args) =>
                {
                    // check the observer alive each event invokation
                    var o = (IObserver<IEvent<TEventArgs>>)wr.Target;
                    if (o != null)
                    {
                        o.OnNext(Event.Create(sender, args));
                    }
                    else
                    {
                        // clean yourself if the observer is collected
                        removeHandler(handler);
                    }
                };

                addHandler(handler);
                return () => removeHandler(handler);
            });
        }

        /// <summary>
        /// Creates an observable that does not hold the strong reference to the
        /// subscribing observers and automatically unsubscribes when the subscription
        /// (returned <see cref="IDisposable"/>) is garbage collected.</summary>
        /// <remarks>
        /// If unsubscription is invoked during garbage collection,
        /// it be invoked in the finalizer thread.</remarks>
        public static IObservable<T> AsWeakObservable<T>(this IObservable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return Observable.CreateWithDisposable<T>(o =>
            {
                var observer = new ObserverWeakWrapper<T>(o);

                // subscribes to the underlying observable
                var subscription = source.Subscribe(observer);

                // pass the subscription to the observer
                observer.SetSubscription(subscription);

                // returns the 
                return new FinalizerDisposable(subscription.Dispose);
            });
        }
    }

    sealed class ObserverWeakWrapper<T> : IObserver<T>
    {
        readonly MutableDisposable subscription;
        readonly WeakReference observer;

        public ObserverWeakWrapper(
          IObserver<T> underlying)
        {
            this.observer = new WeakReference(underlying);
            this.subscription = new MutableDisposable();
        }

        IObserver<T> Observer
        {
            get { return (IObserver<T>)this.observer.Target; }
        }

        public void OnNext(T value)
        {
            var o = Observer;
            if (o != null) o.OnNext(value);
            else this.subscription.Dispose();
        }

        public void OnError(Exception exception)
        {
            var o = Observer;
            if (o != null) o.OnError(exception);
            else this.subscription.Dispose();
        }

        public void OnCompleted()
        {
            var o = Observer;
            if (o != null) o.OnCompleted();
            else this.subscription.Dispose();
        }

        public void SetSubscription(IDisposable disposable)
        {
            this.subscription.Disposable = disposable;
        }
    }

    sealed class FinalizerDisposable : IDisposable
    {
        readonly Action action;

        public FinalizerDisposable(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            this.action = action;
        }

        public void Dispose()
        {
            this.action();
            GC.SuppressFinalize(this);
        }

        ~FinalizerDisposable()
        {
            this.action();
        }
    }
}
