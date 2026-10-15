using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Windows;
using Ksema.Framework.Reactive;

namespace System.Linq
{
    public static class EventPublisherExtensions
    {
        public static void Publish<T>(this IEventPublisher events)
        {
            events.Publish<T>(Activator.CreateInstance<T>());
        }

        public static void BeginPublish<T>(this IEventPublisher events, DispatcherPriority priority = DispatcherPriority.Background)
        {
            events.BeginPublish<T>(Activator.CreateInstance<T>(), priority);
        }

        public static void BeginPublish<TEvent>(this IEventPublisher events, TEvent sampleEvent, DispatcherPriority priority = DispatcherPriority.Background)
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.BeginInvoke((Action)( () => { events.Publish<TEvent>(sampleEvent); }), priority);
            }
            else
            {
                events.Publish<TEvent>(sampleEvent);
            }
        }
    }
}
