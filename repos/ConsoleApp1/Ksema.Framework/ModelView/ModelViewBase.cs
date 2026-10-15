using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Framework.Misc;
using Ksema.Framework.Async;
using System.Threading;
using System.Collections.Concurrent;
using Ksema.ObjectShell.WPF;

namespace Ksema.Framework.ModelView
{
    public class MVTreeChildren : ObservableCollection<ModelViewBase>
    {
        protected override void ClearItems()
        {
            new List<ModelViewBase>(this).ForEach(t => Remove(t));
        }

        protected override void InsertItem(int index, ModelViewBase item)
        {
            if (!this.Contains(item))
            {
                base.InsertItem(index, item);
            }
        }
    }

    public class MVProperty<T> : MVProperty
    {
        public MVProperty(bool inherits = false) : base(typeof(T), inherits)
        {
        }
    }

    public class MVProperty
    {
        internal MVProperty(Type propertyType, bool inherits = false)
        {
            PropertyType = propertyType;
            Inherits = inherits;
        }

        public bool Inherits
        {
            get;
            protected set;
        }

        public Type PropertyType
        {
            get;
            protected set;
        }
    }

    public class MVPropertyProvider
    {
        ConcurrentDictionary<MVProperty, object> _localValues;

        ConcurrentDictionary<MVProperty, object> LocalValues
        {
            get
            {
                if (_localValues == null)
                {
                    _localValues = new ConcurrentDictionary<MVProperty, object>();
                }
                return _localValues;
            }
        }

        public bool IsLocalValueSet<T>(MVProperty<T> property)
        {
            return LocalValues.ContainsKey(property);
        }

        public T GetLocalValue<T>(MVProperty<T> property)
        {
            // Для value типов нельзя будет судить есть ли на самом деле значение
            object res;
            if (LocalValues.TryGetValue(property, out res))
            {
                return (T)res;
            }
            return default(T);
        }

        public void SetValue<T>(MVProperty<T> property, T value)
        {
            if (!IsValidType(value, property.PropertyType))
            {
                throw new ArgumentException("Wrong value type");
            }
            LocalValues[property] = value;
        }

        static Type NullableType = typeof(Nullable<>);

        bool IsValidType(object value, Type propertyType)
        {
            if (value == null)
            {
                if (propertyType.IsValueType && (!propertyType.IsGenericType || (propertyType.GetGenericTypeDefinition() != NullableType)))
                {
                    return false;
                }
            }
            else if (!propertyType.IsInstanceOfType(value))
            {
                return false;
            }
            return true;
        }
    }

    public class ModelViewBase : DispatcherPropertyChangedHelper, IMVPropertyProvider
    {
        public ModelViewBase()
        {
            InitializeIsAnyCommandWorking();
        }

        Dictionary<string, IExtendedCommand> _syncCommands;

        Dictionary<string, IExtendedCommand> SyncCommands
        {
            get
            {
                if (_syncCommands == null)
                {
                    _syncCommands = new Dictionary<string, IExtendedCommand>();
                }
                return _syncCommands;
            }
        }

        protected ISyncCommand GetSyncCommand(string id, Action worker, Func<bool> canExecute, Action<ISyncCommand> initialize = null)
        {
            IExtendedCommand c;
            if (!SyncCommands.TryGetValue(id, out c))
            {
                c = new DelegateCommand(worker, canExecute);
                SyncCommands[id] = c;
                if (initialize != null)
                {
                    initialize((ISyncCommand)c);
                }
            }
            return (ISyncCommand)c;
        }

        protected ISyncCommand<T> GetSyncCommand<T>(string id, Action<T> worker, Func<T, bool> canExecute, Action<ISyncCommand<T>> initialize = null)
        {
            IExtendedCommand c;
            if (!SyncCommands.TryGetValue(id, out c))
            {
                c = new DelegateCommand<T>(worker, canExecute);
                SyncCommands[id] = c;
                if (initialize != null)
                {
                    initialize((ISyncCommand<T>)c);
                }
            }
            return (ISyncCommand<T>)c;
        }

        public static readonly PropertyChangedEventArgs IsAnyCommandWorkingArgs = PropertyChangedHelper.CreateArgs<ModelViewBase>(c => c.IsAnyCommandWorking);

        public bool IsAnyCommandWorking
        {
            get
            {
                return AllAsyncCommands.IsWorking;
            }
        }

        void InitializeIsAnyCommandWorking()
        {
            AllAsyncCommands.ToObservable(c => c.IsWorking).Subscribe(() => OnPropertyChanged(IsAnyCommandWorkingArgs));
        }

        AsyncCommandGroup AllAsyncCommands = new AsyncCommandGroup();

        Dictionary<string, IAsyncCommandBase> AsyncCommands = new Dictionary<string, IAsyncCommandBase>();

        void OnAsyncCommandCreated(string id, IAsyncCommandBase command, Action initialize)
        {
            AsyncCommands[id] = command;
            initialize();
            AllAsyncCommands.Add(command);
        }

        protected IAsyncCommand GetAsyncCommand(string id, Action<IAsyncCommand> initialize)
        {
            IAsyncCommandBase c;
            if (!AsyncCommands.TryGetValue(id, out c))
            {
                c = new AsyncCommandVoid();
                OnAsyncCommandCreated(id, c, initialize == null ? null : (Action)(() => initialize((IAsyncCommand)c)));
            }
            return (IAsyncCommand)c;
        }

        protected IAsyncCommand<P1> GetAsyncCommand<P1>(string id, Action<IAsyncCommand<P1>> initialize)
        {
            IAsyncCommandBase c;
            if (!AsyncCommands.TryGetValue(id, out c))
            {
                c = new AsyncCommandImpl<P1>();
                OnAsyncCommandCreated(id, c, initialize == null ? null : (Action)(() => initialize((IAsyncCommand<P1>)c)));
            }
            return (IAsyncCommand<P1>)c;
        }

        protected IAsyncCommand<P1, P2> GetAsyncCommand<P1, P2>(string id, Action<IAsyncCommand<P1, P2>> initialize)
        {
            IAsyncCommandBase c;
            if (!AsyncCommands.TryGetValue(id, out c))
            {
                c = new AsyncCommandImpl<P1,P2>();
                OnAsyncCommandCreated(id, c, initialize == null ? null : (Action)(() => initialize((IAsyncCommand<P1, P2>)c)));
            }
            return (IAsyncCommand<P1, P2>)c;
        }
		protected IAsyncCommand<P1, P2,P3> GetAsyncCommand<P1, P2,P3>(string id, Action<IAsyncCommand<P1, P2,P3>> initialize)
		{
			IAsyncCommandBase c;
			if (!AsyncCommands.TryGetValue(id, out c))
			{
				c = new AsyncCommandImpl<P1, P2, P3>();
				OnAsyncCommandCreated(id, c, initialize == null ? null : (Action)(() => initialize((IAsyncCommand<P1, P2,P3>)c)));
			}
			return (IAsyncCommand<P1, P2,P3>)c;
		}

        protected IAsyncCommand<P1, P2> GetAsyncCommand<P1, P2>(string id, Action initialize)
        {
            Action<IAsyncCommand<P1, P2>> a = null;
            if(initialize != null)
                a = (c) => initialize();
            return GetAsyncCommand<P1, P2>(id, a);
        }
		protected IAsyncCommand<P1, P2,P3> GetAsyncCommand<P1, P2,P3>(string id, Action initialize)
		{
			Action<IAsyncCommand<P1, P2, P3>> a = null;
			if (initialize != null)
				a = (c) => initialize();
			return GetAsyncCommand<P1, P2,P3>(id, a);
		}

        protected IAsyncCommand<P1> GetAsyncCommand<P1>(string id, Action initialize)
        {
            Action<IAsyncCommand<P1>> a = null;
            if (initialize != null)
                a = (c) => initialize();
            return GetAsyncCommand<P1>(id, a);
        }

        protected IAsyncCommand GetAsyncCommand(string id, Action initialize)
        {
            Action<IAsyncCommand> a = null;
            if (initialize != null)
                a = (c) => initialize();
            return GetAsyncCommand(id, a);
        }

        protected T CreateMV<T>() where T : ModelViewBase
        {
            var mv = Activator.CreateInstance<T>();
            mv.MVParent = this;
            return mv;
        }

        public ModelViewBase MVRoot
        {
            get
            {
                var ent = this;
                while (ent.MVParent != null)
                {
                    ent = ent.MVParent;
                }
                return ent;
            }
        }

        private void FixupParent(ModelViewBase previousValue)
        {
            if (previousValue != null && previousValue.MVChildren.Contains(this))
            {
                previousValue.MVChildren.Remove(this);
            }

            if (MVParent != null)
            {
                if (!MVParent.MVChildren.Contains(this))
                {
                    MVParent.MVChildren.Add(this);
                }
            }
        }

        ModelViewBase _parent;

        //TODO: сделать WeakReference
        public ModelViewBase MVParent
        {
            get
            {
                return _parent;
            }
            set
            {
                if (!ReferenceEquals(_parent, value))
                {
                    var previousValue = _parent;
                    _parent = value;

                    FixupParent(previousValue);
                    OnMVParentChanged(previousValue, value);
                    if (_MVParentChangedSubject != null)
                    {
                        _MVParentChangedSubject.OnNext(value);
                    }
                }
            }
        }

        Subject<ModelViewBase> _MVParentChangedSubject;
        private Subject<ModelViewBase> MVParentChangedSubject
        {
            get
            {
                if (_MVParentChangedSubject == null)
                {
                    _MVParentChangedSubject = new Subject<ModelViewBase>();
                }
                return _MVParentChangedSubject;
            }
        }

        protected IObservable<ModelViewBase> MVParentObservable
        {
            get
            {
                if (MVParent != null)
                {
                    var initial = Observable.Return(this.MVParent);
                    return initial.Concat(MVParentChangedSubject);
                }
                else
                {
                    return MVParentChangedSubject;
                }
            }
        }

        protected virtual void OnMVParentChanged(ModelViewBase oldValue, ModelViewBase newValue)
        {
        }

        //TODO: сделать слабое связывание в коллекции
        MVTreeChildren _children;
        public MVTreeChildren MVChildren
        {
            get
            {
                if(_children == null)
                {
                    _children = new MVTreeChildren();
                    _children.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(_children_CollectionChanged);
                }
                return _children;
            }
        }

        void _children_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (ModelViewBase item in e.NewItems)
                {
                    item.MVParent = this;
                }
            }

            if (e.OldItems != null)
            {
                foreach (ModelViewBase item in e.OldItems)
                {
                    if (ReferenceEquals(item.MVParent, this))
                    {
                        item.MVParent = null;
                    }
                }
            }
        }

        /// <summary>
        /// Вызывает проверку CanExecute у всех команд, иногда надо
        /// </summary>
        protected void InvalidateRequerySuggested()
        {
            if (!CheckAccess())
            {
                Dispatcher.BeginInvoke((Action)CommandManager.InvalidateRequerySuggested);
            }
            else
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }


        MVPropertyProvider _MVPropertyProvider;
        MVPropertyProvider MVPropertyProvider
        {
            get
            {
                if (_MVPropertyProvider == null)
                {
                    _MVPropertyProvider = new MVPropertyProvider();
                }
                return _MVPropertyProvider;
            }
        }

        public T GetValue<T>(MVProperty<T> property)
        {
            if (MVPropertyProvider.IsLocalValueSet(property))
            {
                return MVPropertyProvider.GetLocalValue(property);
            }
            if (property.Inherits && MVParent != null)
            {
                return MVParent.GetValue<T>(property);
            }
            return default(T);
        }

        public bool IsLocalValueSet<T>(MVProperty<T> property)
        {
            return MVPropertyProvider.IsLocalValueSet(property);
        }

        public T GetLocalValue<T>(MVProperty<T> property)
        {
            return MVPropertyProvider.GetLocalValue(property);
        }

        public void SetValue<T>(MVProperty<T> property, T value)
        {
            MVPropertyProvider.SetValue(property, value);
        }
    }
}
