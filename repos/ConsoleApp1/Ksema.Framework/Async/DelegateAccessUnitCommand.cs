using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ksema.Framework.ModelView;
using Ksema.ObjectShell.WPF;
using System.ComponentModel;
using Framework.Misc;

namespace Ksema.Framework
{
    public class DelegateAccessUnitCommand : DelegateCommand, IAccessUnit
    {
        #region HideDisabledProperty
        public static readonly PropertyChangedEventArgs HideDisabledArgs = PropertyChangedHelper.CreateArgs<DelegateAccessUnitCommand>(c => c.HideDisabled);
        private bool _HideDisabled;

        /// <summary>
        /// Автоматически ставить Mode = Hidden, в случае если CanExecute() == false
        /// </summary>
        public bool HideDisabled
        {
            get
            {
                return _HideDisabled;
            }
            set
            {
                var oldValue = HideDisabled;
                _HideDisabled = value;
                if (oldValue != value)
                {
                    OnHideDisabledChanged(oldValue, value);
                    OnPropertyChanged(HideDisabledArgs);
                }
            }
        }

        protected virtual void OnHideDisabledChanged(bool oldValue, bool newValue)
        {
            CalculateMode();
        }

        bool hiddenSet = false;

        void CalculateMode()
        {
            if (HideDisabled)
            {
                if (!IsCanExecute)
                {
                    Mode = AccessUnitMode.Hidden;
                    hiddenSet = true;
                }
                else
                {
                    Mode = AccessUnitMode.Edit;
                }
            }
            else if (Mode == AccessUnitMode.Hidden && hiddenSet)
            {
                // Если мы скрывали команду а теперь автоматическое вычисление режима доступа выключено
                // то возвращаем статус
                // хотя сейчас всегда устанавливается Hidden при инициализации команды
                Mode = AccessUnitMode.Edit;
            }
        }

        protected override void OnIsCanExecuteChanged(bool oldValue, bool newValue)
        {
            if (HideDisabled)
            {
                CalculateMode();
            }
        }
        #endregion

        IAccessUnitModeProvider Provider
        {
            get;
            set;
        }

        public DelegateAccessUnitCommand(IAccessUnitModeProvider _provider, Action executeMethod)
            : base(executeMethod)
        {
            Provider = _provider;
        }

        public DelegateAccessUnitCommand(IAccessUnitModeProvider _provider, Action executeMethod, Func<bool> canExecuteMethod)
            : base(executeMethod, canExecuteMethod)
        {
            Provider = _provider;
        }

        public DelegateAccessUnitCommand(IAccessUnitModeProvider _provider, Action executeMethod, Func<bool> canExecuteMethod, bool isAutomaticRequeryDisabled)
            : base(executeMethod, canExecuteMethod, isAutomaticRequeryDisabled)
        {
            Provider = _provider;
        }

        #region IAccessUnit Members

        public AccessUnitMode Mode
        {
            get
            {
                return Provider.GetMode(this);
            }
            set
            {
                Provider.SetMode(this, value);
            }
        }

        #endregion


        public void __RaiseModeChanged()
        {
            OnPropertyChanged("Mode");
        }
    }

    public class DelegateAccessUnitCommand<T> : DelegateCommand<T>, IAccessUnit
    {
        #region HideDisabledProperty
        public static readonly PropertyChangedEventArgs HideDisabledArgs = PropertyChangedHelper.CreateArgs<DelegateAccessUnitCommand>(c => c.HideDisabled);
        private bool _HideDisabled;

        /// <summary>
        /// Автоматически ставить Mode = Hidden, в случае если CanExecute() == false
        /// </summary>
        public bool HideDisabled
        {
            get
            {
                return _HideDisabled;
            }
            set
            {
                var oldValue = HideDisabled;
                _HideDisabled = value;
                if (oldValue != value)
                {
                    OnHideDisabledChanged(oldValue, value);
                    OnPropertyChanged(HideDisabledArgs);
                }
            }
        }

        protected virtual void OnHideDisabledChanged(bool oldValue, bool newValue)
        {
            CalculateMode();
        }

        bool hiddenSet = false;

        void CalculateMode()
        {
            if (HideDisabled)
            {
                if (!IsCanExecute)
                {
                    Mode = AccessUnitMode.Hidden;
                    hiddenSet = true;
                }
                else
                {
                    Mode = AccessUnitMode.Edit;
                }
            }
            else if (Mode == AccessUnitMode.Hidden && hiddenSet)
            {
                // Если мы скрывали команду а теперь автоматическое вычисление режима доступа выключено
                // то возвращаем статус
                // хотя сейчас всегда устанавливается Hidden при инициализации команды
                Mode = AccessUnitMode.Edit;
            }
        }

        protected override void OnIsCanExecuteChanged(bool oldValue, bool newValue)
        {
            if (HideDisabled)
            {
                CalculateMode();
            }
        }
        #endregion

        IAccessUnitModeProvider Provider
        {
            get;
            set;
        }

        public DelegateAccessUnitCommand(IAccessUnitModeProvider _provider, Action<T> executeMethod)
            : base(executeMethod)
        {
            Provider = _provider;
        }

        public DelegateAccessUnitCommand(IAccessUnitModeProvider _provider, Action<T> executeMethod, Func<T,bool> canExecuteMethod)
            : base(executeMethod, canExecuteMethod)
        {
            Provider = _provider;
        }

        public DelegateAccessUnitCommand(IAccessUnitModeProvider _provider, Action<T> executeMethod, Func<T,bool> canExecuteMethod, bool isAutomaticRequeryDisabled)
            : base(executeMethod, canExecuteMethod, isAutomaticRequeryDisabled)
        {
            Provider = _provider;
        }

        #region IAccessUnit Members

        public AccessUnitMode Mode
        {
            get
            {
                return Provider.GetMode(this);
            }
            set
            {
                Provider.SetMode(this, value);
            }
        }

        #endregion


        public void __RaiseModeChanged()
        {
            OnPropertyChanged("Mode");
        }
    }
}