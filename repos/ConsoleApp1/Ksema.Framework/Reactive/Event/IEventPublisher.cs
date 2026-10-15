using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;

namespace Ksema.Framework.Reactive
{
    public interface IEventPublisher
    {
        /// <summary>
        /// Publishes the specified event.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <param name="event">The event instance.</param>
        void Publish<TEvent>(TEvent @event);

        /// <summary>
        /// Gets the event for use by subscribers.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <returns></returns>
        IObservable<TEvent> GetEvent<TEvent>();
        IObservable<TEvent> GetWeakEvent<TEvent>();
    }
}
