using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace Ksema.Framework.Reactive
{
    /// <summary>
    /// The default implementation of <see cref="IEventPublisher"/>.
    /// </summary>
    public class DefaultEventPublisher : IEventPublisher
    {
        readonly Dictionary<object, object> weakObservable = new Dictionary<object, object>();
        readonly Dictionary<Type, object> subjects = new Dictionary<Type, object>();
        readonly object @lock = new object();

        /// <summary>
        /// Gets the event for use by subscribers.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <returns></returns>
        public virtual IObservable<TEvent> GetEvent<TEvent>()
        {
            object subject;
            var eventType = typeof(TEvent);

            if (!subjects.TryGetValue(eventType, out subject))
            {
                lock (@lock)
                {
                    if (!subjects.TryGetValue(eventType, out subject))
                    {
                        subjects[eventType] = subject = new Subject<TEvent>();
                    }
                }
            }

            return ((ISubject<TEvent>)subject).AsObservable();
        }

        public virtual IObservable<TEvent> GetWeakEvent<TEvent>()
        {
            var subj = GetEvent<TEvent>();
            return GetWeakEvent(subj);
        }

        protected IObservable<TEvent> GetWeakEvent<TEvent>(IObservable<TEvent> subj)
        {
            object observable;
            if (!weakObservable.TryGetValue(subj, out observable))
            {
                observable = subj.AsWeakObservable();
                weakObservable[subj] = observable;
            }
            return (IObservable<TEvent>)observable;
        }
        /// <summary>
        /// Publishes the specified sample event.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <param name="sampleEvent">The sample event.</param>
        public virtual void Publish<TEvent>(TEvent sampleEvent)
        {
            object subject;

            var baseType = typeof(TEvent);
            while (baseType != null)
            {
                if (subjects.TryGetValue(baseType, out subject))
                {
                    subject.GetType().GetMethod("OnNext").Invoke(subject, new object[] { sampleEvent });
                    //((ISubject<TEvent>)subject).OnNext(sampleEvent);
                }
                baseType = baseType.BaseType;
            }
            foreach (var intf in typeof(TEvent).GetInterfaces())
            {
                if (subjects.TryGetValue(intf, out subject))
                {
                    subject.GetType().GetMethod("OnNext").Invoke(subject, new object[] { sampleEvent });
                }
            }
        }
    }
}