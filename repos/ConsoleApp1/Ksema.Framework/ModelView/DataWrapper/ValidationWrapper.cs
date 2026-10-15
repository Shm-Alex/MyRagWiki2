using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.ComponentModel;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using Framework.Misc;
using System.Diagnostics;
using Ksema.Framework;
using System.Threading;

namespace Ksema.Framework.ModelView
{
    internal class ValidationWrapperImpl : PropertyChangedHelper, IValidationWrapper
    {
        public string Id { get; private set; }

        public ValidationWrapperImpl(string id)
        {
            Id = id;
        }

        ReadOnlyObservableCollection<ValidationRule> _ro_rules = null;
        public ReadOnlyObservableCollection<ValidationRule> Rules
        {
            get
            {
                if (_ro_rules == null)
                {
                    _ro_rules = new ReadOnlyObservableCollection<ValidationRule>(rules);
                }
                return _ro_rules;
            }
        }

        ObservableCollection<ValidationRule> rules = new ObservableCollection<ValidationRule>();

        // http://stackoverflow.com/questions/2789504/get-the-property-as-a-string-from-an-expressionfunctmodel-tproperty/2789606#2789606
        public void AddRule(Func<ValidationResult> validator)
        {
            AddRule(new FuncValidationRule(validator));
        }

        public void AddRule(ValidationRule rule)
        {
            rule.ValidatesOnTargetUpdated = true;
            rule.ValidationStep = ValidationStep.UpdatedValue;
            rules.Add(rule);
        }

        public void OnBeforeAttach(Binding b, DependencyObject target, DependencyProperty property)
        {
            if (Trigger == null)
            {
                Trigger = new FastSubject<Unit>();
            }
            b.ValidatesOnExceptions = true;
            b.NotifyOnValidationError = true;
            b.NotifyOnSourceUpdated = true;
            b.NotifyOnTargetUpdated = true;

            foreach (var rule in rules)
            {
                b.ValidationRules.Add(rule);
            }

            UnsubscribeOnError();
            _currentTargetObject = new WeakReference(target);
            currentTargetProperty = property;
        }

        public void OnAfterAttach(BindingExpressionBase bindingExpression)
        {
            currentBindingExpression = bindingExpression;
            SubscribeOnError();
            OnError(null, null);
        }

        IDisposable TriggerSubscription;

        void SubscribeOnError()
        {
            IInputElement inp = currentTargetObject as IInputElement;
            if (inp != null)
            {
                inp.AddHandler(Validation.ErrorEvent, (RoutedEventHandler)OnError);
                inp.AddHandler(Binding.TargetUpdatedEvent, (RoutedEventHandler)OnTargetUpdated);
                inp.AddHandler(Binding.SourceUpdatedEvent, (RoutedEventHandler)OnSourceUpdated);
            }
            SubscribeTrigger();
        }

        void SubscribeTrigger()
        {
            TriggerSubscription = Trigger.ObserveOnDispatcher().Subscribe(OnTrigger);
        }

        void UnSubscribeTrigger()
        {
            if (TriggerSubscription != null)
            {
                TriggerSubscription.Dispose();
            }
        }

        void UnsubscribeOnError()
        {
            UnSubscribeTrigger();
            IInputElement inp = currentTargetObject as IInputElement;
            if (inp != null)
            {
                inp.RemoveHandler(Validation.ErrorEvent, (RoutedEventHandler)OnError);
                inp.RemoveHandler(Binding.TargetUpdatedEvent, (RoutedEventHandler)OnTargetUpdated);
                inp.RemoveHandler(Binding.SourceUpdatedEvent, (RoutedEventHandler)OnSourceUpdated);
            }
        }

        void OnTrigger(Unit u)
        {
            Validate();
        }

        void OnTargetUpdated(object sender, RoutedEventArgs args)
        {
        }

        void OnSourceUpdated(object sender, RoutedEventArgs args)
        {
        }

        List<ValidationError> LastError = new List<ValidationError>();

        void OnError(object sender, RoutedEventArgs args)
        {
            if (!IsAttached)
            {
                // Байндинг отсоединен, отпишемся от событий
                // будем ждать следующего аттача, ошибку сохраняем
                // иначе в байндинге она очищается
                UnsubscribeOnError();
            }
            else
            {
                // Сохраняем ошибку на случай отсоединения байндинга
                // если UI переключился куда-то ещё, то мы все ещё сможем сообщить об ошибке
                LastError.Clear();
                if (currentBindingExpression.ValidationError != null)
                {
                    LastError.Add(currentBindingExpression.ValidationError);
                }
            }
        }

        public bool IsAttached
        {
            get
            {
                return currentBindingExpression != null &&
                    currentBindingExpression.Status != BindingStatus.Inactive &&
                    currentBindingExpression.Status != BindingStatus.Detached;
            }
        }

        BindingExpressionBase currentBindingExpression = null;
        
        WeakReference _currentTargetObject;
        DependencyObject currentTargetObject
        {
            get
            {
                if (_currentTargetObject != null && _currentTargetObject.IsAlive)
                {
                    return (DependencyObject)_currentTargetObject.Target;
                }
                return null;
            }
        }

        DependencyProperty currentTargetProperty;

        public void UpdateSource()
        {
            if (IsAttached)
            {
                currentBindingExpression.UpdateSource();
            }
        }

        /// <summary>
        /// Выполним валидацию
        /// </summary>
        /// <returns></returns>
        public void Validate()
        {
            if (!IsAttached)
            {
                ValidateExplicit();
            }
            else
            {
                // Тут будем валидацию прогонять так
                currentBindingExpression.UpdateTarget();
            }
        }

        protected IEnumerable<ValidationError> ValidateExplicit()
        {
            LastError.Clear();

            foreach (var rule in Rules)
            {
                var res = rule.Validate(null, Thread.CurrentThread.CurrentUICulture);
                if (res != null && !res.IsValid)
                {
                    var err = new ValidationError(rule, new object(), res.ErrorContent, null);
                    // Сохраняем ошибки
                    LastError.Add(err);
                }
            }
            return LastError;
        }

        /// <summary>
        /// Ошибки с последней валидации
        /// </summary>
        public IEnumerable<ValidationError> Errors
        {
            get
            {
                // Если делать тут явную валидацию - убьем идею instant валидации
                return LastError;
            }
        }

        public bool IsValid
        {
            get
            {
                return LastError == null;
            }
        }

        public object Uri
        {
            get;
            set;
        }

        #region TriggerProperty
        public static readonly PropertyChangedEventArgs TriggerArgs = PropertyChangedHelper.CreateArgs<ValidationWrapperImpl>(c => c.Trigger);
        private IObservable<Unit> _Trigger;

        public IObservable<Unit> Trigger
        {
            get
            {
                return _Trigger;
            }
            set
            {
                var oldValue = Trigger;
                _Trigger = value;
                if (oldValue != value)
                {
                    OnTriggerChanged(oldValue, value);
                    OnPropertyChanged(TriggerArgs);
                }
            }
        }

        protected virtual void OnTriggerChanged(IObservable<Unit> oldValue, IObservable<Unit> newValue)
        {
            if (oldValue != null)
                throw new InvalidOperationException("Trigger already set");
        }
        #endregion

    }
}
