using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.ComponentModel;
using System.Windows;
using Framework.Misc;
using System.Collections.ObjectModel;

namespace Ksema.Framework.ModelView
{
    /// <summary>
    /// Базовый класс реализующий DataWrapper и управление доступом к нему
    /// </summary>
    public abstract class AccessUnitModelViewBase : ModelViewBase, IAccessUnitModeProvider, IDataWrapperProvider, IValidationErrorProvider, IValidationWrapperProvider
    {
        protected DelegateAccessUnitCommand GetCommand(string id, Action executeMethod, Func<bool> canExecuteMethod, bool hideDisabled = false)
        {
            IAccessUnit unit;
            Units.TryGetValue(id, out unit);
            if (unit == null || unit is DummyAccesUnit || unit is DelegateAccessUnitCommand)
            {
                var command = unit as DelegateAccessUnitCommand;
                if (command == null)
                {
                    command = CreateCommand(id, executeMethod, canExecuteMethod, hideDisabled);
                }
                return command;
            }
            else
            {
                throw new Exception(String.Format("Access unit {0} already exists", id));
            }
        }

        protected DelegateAccessUnitCommand<T> GetCommand<T>(string id, Action<T> executeMethod, Func<T, bool> canExecuteMethod, bool hideDisabled = false)
        {
            IAccessUnit unit;
            Units.TryGetValue(id, out unit);
            if (unit == null || unit is DummyAccesUnit || unit is DelegateAccessUnitCommand<T>)
            {
                var command = unit as DelegateAccessUnitCommand<T>;
                if (command == null)
                {
                    command = CreateCommand<T>(id, executeMethod, canExecuteMethod, hideDisabled);
                }
                return command;
            }
            else
            {
                throw new Exception(String.Format("Access unit {0} already exists", id));
            }
        }

        DelegateAccessUnitCommand CreateCommand(string id, Action executeMethod, Func<bool> canExecuteMethod, bool hideDisabled = false)
        {
            var com = new DelegateAccessUnitCommand(this, executeMethod, canExecuteMethod) { HideDisabled = hideDisabled };
            SetUnit(id, com);
            return com;
        }

        DelegateAccessUnitCommand<T> CreateCommand<T>(string id, Action<T> executeMethod, Func<T, bool> canExecuteMethod, bool hideDisabled = false)
        {
            var com = new DelegateAccessUnitCommand<T>(this, executeMethod, canExecuteMethod) { HideDisabled = hideDisabled };
            SetUnit(id, com);
            return com;
        }

        public IDataWrapper GetDataWrapper(string property)
        {
            IAccessUnit unit;
            Units.TryGetValue(property, out unit);
            var wrapper = unit as IDataWrapper;
            if (wrapper == null)
            {
                wrapper = CreateDataWrapper(property);
            }
            return wrapper;
        }

        private IDataWrapper CreateDataWrapper(string property)
        {
            List<string> path = property.Split('.').ToList();
            IDataWrapper prevWrapper = null;
            for (int i = 0; i < path.Count; i++)
            {
                var currPath = String.Join(".", path.Take(i + 1).ToArray());
                var currWrapper = GetDataWrapperDirect(currPath) ?? CreateDataWrapperDirect(currPath, prevWrapper);
                prevWrapper = currWrapper;
            }

            return prevWrapper;
        }

        private IDataWrapper GetDataWrapperDirect(string property)
        {
            IAccessUnit unit;
            Units.TryGetValue(property, out unit);
            return unit as IDataWrapper;
        }

        private IDataWrapper CreateDataWrapperDirect(string property, IDataWrapper parent)
        {
            var wrapper = GetDataWrapperDirect(property);

            wrapper = new DataWrapperImpl(property, this);
            wrapper.Parent = parent;

            SetUnit(property, wrapper);
            return wrapper;
        }

        Dictionary<string, IAccessUnit> Units = new Dictionary<string, IAccessUnit>();

        IAccessUnit CreateDummyUnit(string id)
        {
            var unit = new DummyAccesUnit(this);
            Units[id] = unit;
            return unit;
        }

        /// <summary>
        /// Установка юнита в словарь Unit, напрямую нельзя
        /// </summary>
        /// <param name="id"></param>
        /// <param name="unit"></param>
        private void SetUnit(string id, IAccessUnit unit)
        {
            IAccessUnit current = null;
            Units.TryGetValue(id, out current);
            if (current != null && !(current is DummyAccesUnit))
                throw new Exception(String.Format("Access unit {0} already created", id));
            Units[id] = unit;
            if (current != null)
            {
                // Был создан прокси объект, сообщим ему о том что создан настойщий юнит
                var dummy = current as DummyAccesUnit;
                dummy.Source = unit;
            }
        }

        public IAccessUnit GetUnit(string id)
        {
            IAccessUnit unit = null;
            Units.TryGetValue(id, out unit);
            if (unit == null)
            {
                // Создаем заглушку
                return CreateDummyUnit(id);
            }
            return unit;
        }

        Dictionary<IAccessUnit, AccessUnitMode> _unitModes;
        private Dictionary<IAccessUnit, AccessUnitMode> UnitModes
        {
            get
            {
                if (_unitModes == null)
                {
                    _unitModes = new Dictionary<IAccessUnit, AccessUnitMode>();
                }
                return _unitModes;
            }
        }

        public static readonly PropertyChangedEventArgs DefaultModeArgs = PropertyChangedHelper.CreateArgs<AccessUnitModelViewBase>(c => c.DefaultMode);
        private AccessUnitMode _DefaultMode = AccessUnitMode.Edit;

        public AccessUnitMode DefaultMode
        {
            get
            {
                return _DefaultMode;
            }
            protected set
            {
                var oldValue = _DefaultMode;
                _DefaultMode = value;
                if (oldValue != value)
                {
                    OnPropertyChanged(DefaultModeArgs);
                    RenewAll();
                }
            }
        }

        private void RenewAll()
        {
            foreach (var unit in Units.Values)
            {
                OnUnitModeChangedDirect(unit);
            }
        }

        private IEnumerable<AccessUnitMode> GetAllModes(IDataWrapper wrapper)
        {
            yield return GetModeDirect(wrapper);
            IDataWrapper parent = wrapper.Parent;
            while (parent != null)
            {
                yield return GetModeDirect(parent);
                parent = parent.Parent;
            }
            yield break;
        }

        AccessUnitMode GetModeDirect(IAccessUnit unit)
        {
            AccessUnitMode m;
            if (UnitModes.TryGetValue(unit, out m))
            {
                return m.And(DefaultMode);
            }
            return DefaultMode;
        }

        public virtual AccessUnitMode GetMode(IAccessUnit unit)
        {
            if (unit is IDataWrapper)
            {
                return GetAllModes(unit as IDataWrapper).Aggregate<AccessUnitMode, AccessUnitMode>(DefaultMode, (l, r) => l.And(r));
            }
            else
            {
                return GetModeDirect(unit);
            }
        }

        public virtual void SetMode(IAccessUnit unit, AccessUnitMode mode)
        {
            AccessUnitMode? old = null;

            if (UnitModes.ContainsKey(unit))
            {
                old = UnitModes[unit];
            }

            UnitModes[unit] = mode;

            if (old == null || (old != null && old.Value != mode))
            {
                OnUnitModeChanged(unit);
            }
        }

        IEnumerable<IDataWrapper> GetAllChildren(IDataWrapper wrapper)
        {
            Queue<IDataWrapper> q = new Queue<IDataWrapper>();
            q.Enqueue(wrapper);

            while (q.Count > 0)
            {
                var obj = q.Dequeue();
                foreach (var child in obj.Children)
                {
                    q.Enqueue(child);
                    yield return child;
                }
            }
            yield break;
        }

        protected void OnUnitModeChanged(IAccessUnit unit)
        {
            OnUnitModeChangedDirect(unit);
            if(unit is IDataWrapper)
            {
                foreach (var child in GetAllChildren(unit as IDataWrapper))
                {
                    OnUnitModeChangedDirect(child);
                }
            }
        }

        private void OnUnitModeChangedDirect(IAccessUnit unit)
        {
            unit.__RaiseModeChanged();
            if (UnitModeChanged != null)
            {
                UnitModeChanged(this, new AccessUnitModeChangedEventArgs(unit));
            }
        }

        public event AccessUnitModeChangedEventHandler UnitModeChanged;

        public IEnumerable<IValidationError> Errors
        {
            get {
                return Units.Values.Where(u => u is IValidationErrorContainer).Cast<IValidationErrorContainer>()
                    .Concat(ValidationWrappers.Values)
                    .SelectMany(w => w.Errors.Select(er => new ValidationErrorImpl((w.Uri == null ? DefaultUri : w.Uri), er.ErrorContent)));
            }
        }

        #region DefaultUriProperty
		public static readonly PropertyChangedEventArgs DefaultUriArgs = PropertyChangedHelper.CreateArgs<AccessUnitModelViewBase>(c => c.DefaultUri);
		private string _DefaultUri;

		public string DefaultUri
		{
			get
			{
				return _DefaultUri;
			}
			protected set
			{
				var oldValue = DefaultUri;
				_DefaultUri = value;
				if (oldValue != value)
				{
					OnDefaultUriChanged(oldValue, value);
					OnPropertyChanged(DefaultUriArgs);
				}
			}
		}

        protected virtual void OnDefaultUriChanged(string oldValue, string newValue)
        {
        }
        #endregion

        Dictionary<string, IValidationWrapper> _validationWrappers;
        Dictionary<string, IValidationWrapper> ValidationWrappers
        {
            get
            {
                if (_validationWrappers == null)
                {
                    _validationWrappers = new Dictionary<string, IValidationWrapper>();
                }
                return _validationWrappers;
            }
        }

        public IValidationWrapper GetValidationWrapper(string id)
        {
            IValidationWrapper wrapper;
            if (ValidationWrappers.TryGetValue(id, out wrapper))
            {
                return wrapper;
            }
            return CreateValidationDataWrapper(id);
        }

        private IValidationWrapper CreateValidationDataWrapper(string id)
        {
            var wr = new ValidationWrapperImpl(id);
            ValidationWrappers[id] = wr;
            return wr;
        }
    }
}
