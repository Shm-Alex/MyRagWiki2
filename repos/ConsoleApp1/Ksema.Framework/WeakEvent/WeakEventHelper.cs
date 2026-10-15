/// All code in this source file is based on the article located at
/// http://diditwith.net/PermaLink,guid,aacdb8ae-7baa-4423-a953-c18c1c7940ab.aspx 
/// http://agsmith.wordpress.com/2008/04/07/propertydescriptor-addvaluechanged-alternative/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

namespace Ksema.Framework
{
    #region Types

    public delegate void UnregisterCallback<E>(EventHandler<E> eventHandler)
      where E : EventArgs;

    public delegate void UnregisterCallback(EventHandler eventHandler);

    public delegate void PropertyChangedUnregisterCallback(PropertyChangedEventHandler eventHandler);

    public interface IWeakEventHandler<E> where E : EventArgs
    {
        EventHandler<E> Handler { get; }
    }

    public interface IPropertyChangedWeakEventHandler
    {
        PropertyChangedEventHandler Handler { get; }
    }

    public interface IWeakEventHandler
    {
        EventHandler Handler { get; }
    }
    #endregion

    /// <summary>
    /// Provides methods for creating WeakEvent handlers
    /// </summary>
    /// <typeparam name="T">The type of the event source</typeparam>
    /// <typeparam name="E">The EventArgs</typeparam>
    public class WeakEventHandler<T, E> : IWeakEventHandler<E>
        where T : class
        where E : EventArgs
    {
        #region Data
        private delegate void OpenEventHandler(T @this, object sender, E e);
        private WeakReference m_TargetRef;
        private OpenEventHandler m_OpenHandler;
        private EventHandler<E> m_Handler;
        private UnregisterCallback<E> m_Unregister;
        #endregion

        #region Ctor
        /// <summary>
        /// Constructs a new WeakEventHandler
        /// </summary>
        /// <param name="eventHandler">The Event handler</param>
        /// <param name="unregister">Unregister delegate</param>
        public WeakEventHandler(EventHandler<E> eventHandler, UnregisterCallback<E> unregister)
        {
            m_TargetRef = new WeakReference(eventHandler.Target);
            m_OpenHandler = (OpenEventHandler)Delegate.CreateDelegate(typeof(OpenEventHandler),
              null, eventHandler.Method);
            m_Handler = Invoke;
            m_Unregister = unregister;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Invokes the event handler if the source is still alive
        /// </summary>
        public void Invoke(object sender, E e)
        {
            T target = (T)m_TargetRef.Target;

            if (target != null)
                m_OpenHandler.Invoke(target, sender, e);
            else if (m_Unregister != null)
            {
                m_Unregister(m_Handler);
                m_Unregister = null;
            }
        }

        public EventHandler<E> Handler
        {
            get { return m_Handler; }
        }

        public static implicit operator EventHandler<E>(WeakEventHandler<T, E> weh)
        {
            return weh.m_Handler;
        }
        #endregion
    }

    public class WeakEventHandler<T> : IWeakEventHandler
        where T : class
    //where E : EventArgs
    {
        #region Data
        private delegate void OpenEventHandler(T @this, object sender, EventArgs e);
        private WeakReference m_TargetRef;
        private OpenEventHandler m_OpenHandler;
        private EventHandler m_Handler;
        private UnregisterCallback m_Unregister;
        #endregion

        #region Ctor
        /// <summary>
        /// Constructs a new WeakEventHandler
        /// </summary>
        /// <param name="eventHandler">The Event handler</param>
        /// <param name="unregister">Unregister delegate</param>
        public WeakEventHandler(EventHandler eventHandler, UnregisterCallback unregister)
        {
            m_TargetRef = new WeakReference(eventHandler.Target);
            m_OpenHandler = (OpenEventHandler)Delegate.CreateDelegate(typeof(OpenEventHandler),
              null, eventHandler.Method);
            m_Handler = Invoke;
            m_Unregister = unregister;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Invokes the event handler if the source is still alive
        /// </summary>
        public void Invoke(object sender, EventArgs e)
        {
            T target = (T)m_TargetRef.Target;

            if (target != null)
                m_OpenHandler.Invoke(target, sender, e);
            else if (m_Unregister != null)
            {
                m_Unregister(m_Handler);
                m_Unregister = null;
            }
        }

        public EventHandler Handler
        {
            get { return m_Handler; }
        }

        public static implicit operator EventHandler(WeakEventHandler<T> weh)
        {
            return weh.m_Handler;
        }
        #endregion
    }

    public class PropertyChangedWeakEventHandler<T> : IPropertyChangedWeakEventHandler
        where T : class
    //where E : EventArgs
    {
        #region Data
        private delegate void OpenEventHandler(T @this, object sender, PropertyChangedEventArgs e);
        private WeakReference m_TargetRef;
        private OpenEventHandler m_OpenHandler;
        private PropertyChangedEventHandler m_Handler;
        private PropertyChangedUnregisterCallback m_Unregister;
        #endregion

        #region Ctor
        /// <summary>
        /// Constructs a new WeakEventHandler
        /// </summary>
        /// <param name="eventHandler">The Event handler</param>
        /// <param name="unregister">Unregister delegate</param>
        public PropertyChangedWeakEventHandler(PropertyChangedEventHandler eventHandler, PropertyChangedUnregisterCallback unregister)
        {
            m_TargetRef = new WeakReference(eventHandler.Target);
            m_OpenHandler = (OpenEventHandler)Delegate.CreateDelegate(typeof(OpenEventHandler),
              null, eventHandler.Method);
            m_Handler = Invoke;
            m_Unregister = unregister;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Invokes the event handler if the source is still alive
        /// </summary>
        public void Invoke(object sender, PropertyChangedEventArgs e)
        {
            T target = (T)m_TargetRef.Target;

            if (target != null)
                m_OpenHandler.Invoke(target, sender, e);
            else if (m_Unregister != null)
            {
                m_Unregister(m_Handler);
                m_Unregister = null;
            }
        }

        public PropertyChangedEventHandler Handler
        {
            get { return m_Handler; }
        }

        public static implicit operator PropertyChangedEventHandler(PropertyChangedWeakEventHandler<T> weh)
        {
            return weh.m_Handler;
        }
        #endregion
    }

    /// <summary>
    /// Позволяет получать нотификации о изменении значения DependencyProperty,
    /// вместо DependencyPropertyDescriptor.AddValueChanged - который бяка
    /// Оригинальная идея http://agsmith.wordpress.com/2008/04/07/propertydescriptor-addvaluechanged-alternative/
    /// </summary>
    public class PropertyChangeNotifier : DependencyObject, IDisposable
    {
        public static readonly DependencyProperty PinnedProperty = DependencyProperty.RegisterAttached("Pinned", typeof(HashSet<PropertyChangeNotifier>), typeof(PropertyChangeNotifier), new FrameworkPropertyMetadata());

        public static void SetPinned(DependencyObject obj, HashSet<PropertyChangeNotifier> value)
        {
            obj.SetValue(PinnedProperty, value);
        }

        public static HashSet<PropertyChangeNotifier> GetPinned(DependencyObject obj)
        {
            return (HashSet<PropertyChangeNotifier>)obj.GetValue(PinnedProperty);
        }

        #region Member Variables
        private WeakReference _propertySource;
        #endregion // Member Variables

        static bool PinToSourceDefault = true;

        #region Constructor

        public PropertyChangeNotifier(DependencyObject propertySource, string path)
            : this(propertySource, new PropertyPath(path), PinToSourceDefault)
        {
        }
        public PropertyChangeNotifier(DependencyObject propertySource, DependencyProperty property)
            : this(propertySource, new PropertyPath(property), PinToSourceDefault)
        {
        }

        public PropertyChangeNotifier(DependencyObject propertySource, PropertyPath property)
            : this(propertySource, property, PinToSourceDefault)
        {
        }

        public PropertyChangeNotifier(DependencyObject propertySource, string path, bool pinToSource)
            : this(propertySource, new PropertyPath(path), pinToSource)
        {
        }

        public PropertyChangeNotifier(DependencyObject propertySource, DependencyProperty property, bool pinToSource)
            : this(propertySource, new PropertyPath(property), pinToSource)
        {
        }

        bool pinnedToSource;

        /// <summary>
        /// Helper class for receiving DependencyProperty change notifications without memory leaks
        /// </summary>
        /// <param name="propertySource">The DependencyObject wich propertys change we need to listen to</param>
        /// <param name="property">The DependencyProperty wich change we need to listen to</param>
        /// <param name="pinToSource">Pin the lifetime of our object to the propertySource object</param>
        public PropertyChangeNotifier(DependencyObject propertySource, PropertyPath property, bool pinToSource)
        {
            if (null == propertySource)
                throw new ArgumentNullException("propertySource");
            if (null == property)
                throw new ArgumentNullException("property");

            pinnedToSource = pinToSource;

            this._propertySource = new WeakReference(propertySource);
            Binding binding = new Binding();
            binding.Path = property;
            binding.Mode = BindingMode.OneWay;
            binding.Source = propertySource;
            BindingOperations.SetBinding(this, ValueProperty, binding);

            if (pinToSource)
            {
                PinTo(propertySource);
            }
        }
        #endregion // Constructor

        private void PinTo(DependencyObject obj)
        {
            var pinned = GetPinned(obj);
            if (pinned == null)
            {
                pinned = new HashSet<PropertyChangeNotifier>();
                SetPinned(obj, pinned);
            }
            pinned.Add(this);
        }

        private void UnPin(DependencyObject obj)
        {
            var pinned = GetPinned(obj);
            if (pinned != null)
            {
                pinned.Remove(this);
            }
        }

        #region PropertySource
        public DependencyObject PropertySource
        {
            get
            {
                try
                {
                    // note, it is possible that accessing the target property
                    // will result in an exception so i’ve wrapped this check
                    // in a try catch
                    return this._propertySource.IsAlive
                    ? this._propertySource.Target as DependencyObject
                    : null;
                }
                catch
                {
                    return null;
                }
            }
        }
        #endregion // PropertySource

        #region Value
        /// <summary>
        /// Identifies the <see cref=”Value”/> dependency property
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value",
        typeof(object), typeof(PropertyChangeNotifier), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnPropertyChanged)));

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PropertyChangeNotifier notifier = (PropertyChangeNotifier)d;
            if (null != notifier._valueChanged)
                notifier._valueChanged(notifier, EventArgs.Empty);
        }

        /// <summary>
        /// Returns/sets the value of the property
        /// </summary>
        /// <seealso cref=”ValueProperty”/>
        [Description("Returns/sets the value of the property")]
        [Category("Behavior")]
        [Bindable(true)]
        public object Value
        {
            get
            {
                return (object)this.GetValue(PropertyChangeNotifier.ValueProperty);
            }
            set
            {
                this.SetValue(PropertyChangeNotifier.ValueProperty, value);
            }
        }
        #endregion //Value

        #region Events
        private EventHandler _valueChanged;
        public event EventHandler ValueChanged
        {
            add
            {
                _valueChanged += value;
            }
            remove
            {
                _valueChanged -= value;
            }
        }
        #endregion // Events

        #region IDisposable Members
        public void Dispose()
        {
            if (pinnedToSource && PropertySource != null)
            {
                var dispObj = PropertySource as DispatcherObject;
                if (dispObj != null && !dispObj.Dispatcher.CheckAccess())
                {
                    dispObj.Dispatcher.BeginInvoke((Action)(() => DisposeImpl()), DispatcherPriority.Send);
                }
                else
                {
                    DisposeImpl();
                }
            }
        }

        void DisposeImpl()
        {
            UnPin(PropertySource);
            BindingOperations.ClearBinding(this, ValueProperty);
        }
        #endregion
    }

    /// <summary>
    /// Provides extension method for EventHandler&lt;E&gt;
    /// </summary>
    /// <example>
    /// <![CDATA[
    /// 
    ///    //SO DECLARE LISTENERS LIKE
    ///    workspace.CloseWorkSpace +=
    ///        new EventHandler<EventArgs>(OnCloseWorkSpace).
    ///           MakeWeak(eh => workspace.CloseWorkSpace -= eh);
    ///           
    ///    private void OnCloseWorkSpace(object sender, EventArgs e)
    ///    {
    ///
    ///    }
    ///    
    ///    //OR YOU COULD CREATE ACTUAL EVENTS LIKE
    ///    public class EventProvider
    ///    {
    ///         private EventHandler<EventArgs> closeWorkSpace;
    ///         public event EventHandler<EventArgs> CloseWorkSpace
    ///         {
    ///             add
    ///             {
    ///                 closeWorkSpace += value.MakeWeak(eh => closeWorkSpace -= eh);
    ///             }
    ///             remove
    ///             {
    ///             }
    ///         }
    ///    }
    /// ]]>
    /// </example>
    public static class EventHandlerUtils
    {
        public static IDisposable AddValueChangedWeak(this DependencyProperty prop, DependencyObject component, EventHandler handler)
        {
            var not = new PropertyChangeNotifier(component, prop, true);
            not.ValueChanged += handler.MakeWeak(h => not.ValueChanged -= h);
            return not;
        }

        public static IDisposable AddValueChanged(this DependencyProperty prop, DependencyObject component, EventHandler handler)
        {
            var not = new PropertyChangeNotifier(component, prop, true);
            not.ValueChanged += handler;
            return not;
        }

        public static IDisposable AddValueChangedWeak(this DependencyPropertyDescriptor dpd, DependencyObject component, EventHandler handler)
        {
            return dpd.DependencyProperty.AddValueChangedWeak(component, handler);
        }

        #region EventHandler<E> extensions
        /// <summary>
        /// Sxtesion method for EventHandler<E>
        /// </summary>
        /// <typeparam name="E">The type</typeparam>
        /// <param name="eventHandler">The EventHandler</param>
        /// <param name="unregister">EventHandler unregister delegate</param>
        /// <returns>An EventHandler</returns>
        public static EventHandler<E> MakeWeak<E>(this EventHandler<E> eventHandler,
            UnregisterCallback<E> unregister) where E : EventArgs
        {
            if (eventHandler == null)
                throw new ArgumentNullException("eventHandler");

            if (eventHandler.Method.IsStatic || eventHandler.Target == null)
                throw new ArgumentException("Only instance methods are supported.", "eventHandler");

            Type wehType = typeof(WeakEventHandler<,>).MakeGenericType(
                eventHandler.Method.DeclaringType, typeof(E));

            ConstructorInfo wehConstructor =
                wehType.GetConstructor(new Type[] { typeof(EventHandler<E>),
                    typeof(UnregisterCallback<E>) });

            IWeakEventHandler<E> weh = (IWeakEventHandler<E>)wehConstructor.Invoke(
              new object[] { eventHandler, unregister });

            return weh.Handler;
        }
        #endregion

        public static EventHandler MakeWeak(this EventHandler eventHandler, UnregisterCallback unregister)
        {
            if (eventHandler == null)
                throw new ArgumentNullException("eventHandler");

            if (eventHandler.Method.IsStatic || eventHandler.Target == null)
                throw new ArgumentException("Only instance methods are supported.", "eventHandler");

            Type wehType = typeof(WeakEventHandler<>).MakeGenericType(
                eventHandler.Method.DeclaringType);

            ConstructorInfo wehConstructor =
                wehType.GetConstructor(new Type[] { typeof(EventHandler),
                    typeof(UnregisterCallback) });

            IWeakEventHandler weh = (IWeakEventHandler)wehConstructor.Invoke(
              new object[] { eventHandler, unregister });

            return weh.Handler;
        }

        public static PropertyChangedEventHandler MakeWeak(this PropertyChangedEventHandler eventHandler,
            PropertyChangedUnregisterCallback unregister)
        {
            if (eventHandler == null)
                throw new ArgumentNullException("eventHandler");

            if (eventHandler.Method.IsStatic || eventHandler.Target == null)
                throw new ArgumentException("Only instance methods are supported.", "eventHandler");

            Type wehType = typeof(PropertyChangedWeakEventHandler<>).MakeGenericType(
                eventHandler.Method.DeclaringType);

            ConstructorInfo wehConstructor =
                wehType.GetConstructor(new Type[] { typeof(PropertyChangedEventHandler),
                    typeof(PropertyChangedUnregisterCallback) });

            IPropertyChangedWeakEventHandler weh = (IPropertyChangedWeakEventHandler)wehConstructor.Invoke(
              new object[] { eventHandler, unregister });

            return weh.Handler;
        }
    }
}
