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
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Diagnostics;
using System.Concurrency;

namespace Ksema.Framework.ModelView
{
    public class FuncValidationRule : ValidationRule
    {
        public Func<ValidationResult> Validator { get; private set; }

        public FuncValidationRule(Func<ValidationResult> validator)
        {
            Validator = validator;
        }

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            return Validator();
        }
    }

    public class FuncValueConverter : IValueConverter
    {
        public FuncValueConverter(Func<object, object> convert = null, Func<object, object> convertBack = null)
        {
            this.Convert = convert;
            this.ConvertBack = convertBack;
        }

        public Func<object, object> Convert { get; set; }
        public Func<object, object> ConvertBack { get; set; }

        object IValueConverter.Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (Convert != null)
            {
                return Convert(value);
            }
            return value;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (ConvertBack != null)
            {
                return ConvertBack(value);
            }
            return value;
        }
    }

    public class DataWrapperChildren : ObservableCollection<IDataWrapper>
    {
        protected override void ClearItems()
        {
            new List<IDataWrapper>(this).ForEach(t => Remove(t));
        }

        protected override void InsertItem(int index, IDataWrapper item)
        {
            if (!this.Contains(item))
            {
                base.InsertItem(index, item);
            }
        }
    }

    internal class DataWrapperImpl : DependencyObject, IDataWrapper
    {
        ObservableCollection<Func<CancellationToken, ValidationResult>> _asyncRules;

        /// <summary>
        /// Правила валидации которые должны быть выполнены асинхронно
        /// </summary>
        ObservableCollection<Func<CancellationToken, ValidationResult>> AsyncRules
        {
            get
            {
                if (_asyncRules == null)
                {
                    _asyncRules = new ObservableCollection<Func<CancellationToken, ValidationResult>>();
                    // Если это первое обращение инициализируем весь механизм
                    InitAsyncValidation();
                }
                return _asyncRules;
            }
        }

        /// <summary>
        /// Добавить асинхронное правило валидации
        /// </summary>
        /// <param name="validator"></param>
        public void AddAsyncRule(Func<CancellationToken, ValidationResult> validator)
        {
            AsyncRules.Add(validator);
        }

        /// <summary>
        /// Вызывается в начале процесса валидации байндинга
        /// </summary>
        /// <returns></returns>
        ValidationResult OnValidationBegin()
        {
            if (asyncResultSet != null)
            {
                // Происходит цикл установки ошибки от асинхронной валидации
                // вернем ранее полученный результат, далее правила валидации проверяться не будут
                return asyncResultSet;
            }
            // Запрос на валидацию
            if (ValidationReq != null)
            {
                //System.Diagnostics.Debug.WriteLine("ValiationReq");
                ValidationReq.OnNext(new Unit());
            }
            lock (this)
            {
                if (ValidationTask == null && AsyncRules.Count > 0)
                {
                    ValidationTask = new TaskCompletionSource<Unit>();
                }
            }
            return ValidationResult.ValidResult;
        }

        /// <summary>
        /// Вызывается байндингом последним в списке правил
        /// </summary>
        /// <returns></returns>
        ValidationResult OnValidationEnd()
        {
            //lock (this)
            //{
            //    if (asyncResultSet != null)
            //    {
            //        // Идет завершающий цикл асинхронной валидации
            //        // валидация заканчивается
            //        OnValidationCompleted();
            //    }
            //}
            return ValidationResult.ValidResult;
        }

        /// <summary>
        /// Асинхронная валидация завершена, завершим и таск валидации
        /// </summary>
        void OnValidationCompleted()
        {
            if (ValidationTask != null)
            {
                ValidationTask.SetResult(new Unit());
                ValidationTask = null;
            }
        }

        /// <summary>
        /// Правило валидации, которое отслеживает начало валидации, требуется для ручного создания ValidationError
        /// </summary>
        ValidationRule AsyncValidationRule;

        void InitValidation()
        {
            AsyncValidationRule = new FuncValidationRule(OnValidationBegin) { ValidationStep = ValidationStep.RawProposedValue, ValidatesOnTargetUpdated = true };
            rules.Add(AsyncValidationRule);
            rules.Add(new FuncValidationRule(OnValidationEnd) { ValidationStep = ValidationStep.CommittedValue, ValidatesOnTargetUpdated = true });
        }

        /// <summary>
        /// Только при наличии асинхронных правил инициализируем механизм
        /// </summary>
        void InitAsyncValidation()
        {
            ValidationReq = new FastSubject<Unit>();

            // Запросы на валидацию буферизуем
            ValidationReq.BufferWithTimeAfterValue(TimeSpan.FromMilliseconds(10))
                .ObserveOn(Scheduler.ThreadPool)
                .Subscribe(OnValidateAsync);
        }

        CancellationTokenSource currentAsyncToken;
        IDisposable currentAsyncSubscription;
        IDisposable currentAsyncValidationEndSubscription;

        /// <summary>
        /// Производим асинхронную валидацию, вызывается после буферизации запросов на валидацию
        /// </summary>
        void OnValidateAsync()
        {
            //System.Diagnostics.Debug.WriteLine("OnValidateAsync");
            lock (this)
            {
                if (currentAsyncToken != null)
                {
                    currentAsyncValidationEndSubscription.Dispose();
                    currentAsyncSubscription.Dispose();
                    currentAsyncToken.Cancel();
                }
                currentAsyncToken = new CancellationTokenSource();
                var tasks = AsyncRules.Select((r) => Task<ValidationResult>.Factory.StartNew(() => r(currentAsyncToken.Token)).ToObservable()).ToList();

                var allResults = tasks.Merge();
                currentAsyncValidationEndSubscription = allResults.Subscribe((r) => { }, OnAllAsyncValidationTasksEnd);

                var result = allResults.Where(r => !r.IsValid).Take(1);
                AsyncValidationTask = result.ToTaskLast();
                currentAsyncSubscription = result./*ObserveOn(this.currentTargetObject.Dispatcher).*/Subscribe(OnAsyncError);
            }
        }

        /// <summary>
        /// Завершились все таски асинхронной валидации, ошибок асинхронной валидации нет
        /// иначе вызова не будет, он будет отменен
        /// </summary>
        void OnAllAsyncValidationTasksEnd()
        {
            //System.Diagnostics.Debug.WriteLine("OnAllAsyncValidationTasksEnd");

            // Валидация завершена
            OnValidationCompleted();
        }

        Task<ValidationResult> AsyncValidationTask;

        /// <summary>
        /// Результат асинхронной валидации, если не нал, значит идет процесс установки результата
        /// асинхронной валидации в байндинг
        /// </summary>
        ValidationResult asyncResultSet = null;

        /// <summary>
        /// Вызывается в случае получения ошибки(именно ошибки) от асинхронной валидации
        /// </summary>
        /// <param name="result"></param>
        void OnAsyncError(ValidationResult result)
        {
            //System.Diagnostics.Debug.WriteLine("OnAsyncError");

            // Асинхронная валидация дала ошибку, отменим вызов завершения всех тасков
            currentAsyncValidationEndSubscription.Dispose();

            if (LastError.Any())
            {
                // Уже есть какая-то ошибка, не будем её менять
                // что бы не смущать пользователя
                OnValidationCompleted();
                //System.Diagnostics.Debug.WriteLine("OnAsyncError LastError");

                return;
            }
            // Асинхронная валидация вернула ошибку
            // нам надо просто установить её

            // сразу сохраним её у нас
            SetAsyncErrorExplicit(result.ErrorContent);
            // валидация в общем закончена
            // не будет ожидать установки ошибки в UI
            OnValidationCompleted();

            if (IsAttached)
            {
                //System.Diagnostics.Debug.WriteLine("OnAsyncError IsAttached");
                // Байндинг ещё работает, установим ошибку в него
                // Запустим новый цикл валидации, но обработаем его по особому
                // Так же UpdateSource() необходимо выполнять только в UI потоке
                currentTargetObject.Dispatcher.BeginInvoke((Action<ValidationResult>)SetAsyncError, DispatcherPriority.Send, new object[] { result });
            }
        }

        void SetAsyncErrorExplicit(object errorContent)
        {
            LastError.Clear();
            LastError.Add(new ValidationError(AsyncValidationRule, new object(), errorContent, null));
        }

        /// <summary>
        /// Установка результата асинхронной валидации в байндинг
        /// </summary>
        /// <param name="result"></param>
        void SetAsyncError(ValidationResult result)
        {
            //System.Diagnostics.Debug.WriteLine("SetAsyncError");
            if (IsAttached)
            {
                //System.Diagnostics.Debug.WriteLine("SetAsyncError IsAttached");
                lock (this)
                {
                    try
                    {
                        asyncResultSet = result;
                        currentBindingExpression.UpdateSource();
                    }
                    finally
                    {
                        asyncResultSet = null;
                    }
                }
            }
            // Если байндинг отсоединился пока нас вызывали - ничего не делаем, ошибку уже сохранили в OnAsyncError
        }

        /// <summary>
        /// Сигналы о начале валидации
        /// обычный Subject 
        /// </summary>
        FastSubject<Unit> ValidationReq;

        private void FixupParent(IDataWrapper previousValue)
        {
            if (previousValue != null && previousValue.Children.Contains(this))
            {
                previousValue.Children.Remove(this);
            }

            if (Parent != null)
            {
                if (!Parent.Children.Contains(this))
                {
                    Parent.Children.Add(this);
                }
            }
        }

        IDataWrapper _parent;
        public IDataWrapper Parent
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
                    OnParentChanged(previousValue, value);
                }
            }
        }

        protected virtual void OnParentChanged(IDataWrapper oldValue, IDataWrapper newValue)
        {
        }

        DataWrapperChildren _Dependent;
        public ObservableCollection<IDataWrapper> Dependent
        {
            get
            {
                if (_Dependent == null)
                {
                    _Dependent = new DataWrapperChildren();
                    _Dependent.CollectionChanged += (s, e) =>
                    {
                        if (e.NewItems != null)
                        {
                            foreach (IDataWrapper item in e.NewItems)
                            {
                                item.DependsOn.Add(this);
                            }
                        }

                        if (e.OldItems != null)
                        {
                            foreach (IDataWrapper item in e.OldItems)
                            {
                                item.DependsOn.Remove(this);
                            }
                        }
                    };
                }
                return _Dependent;
            }
        }

        DataWrapperChildren _DependsOn;
        public ObservableCollection<IDataWrapper> DependsOn
        {
            get
            {
                if (_DependsOn == null)
                {
                    _DependsOn = new DataWrapperChildren();
                    _DependsOn.CollectionChanged += (s, e) =>
                    {
                        if (e.NewItems != null)
                        {
                            foreach (IDataWrapper item in e.NewItems)
                            {
                                item.Dependent.Add(this);
                            }
                        }

                        if (e.OldItems != null)
                        {
                            foreach (IDataWrapper item in e.OldItems)
                            {
                                item.Dependent.Remove(this);
                            }
                        }
                    };

                }
                return _DependsOn;
            }
        }

        DataWrapperChildren _children;
        public ObservableCollection<IDataWrapper> Children
        {
            get
            {
                if (_children == null)
                {
                    _children = new DataWrapperChildren();
                    _children.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(_children_CollectionChanged);
                }
                return _children;
            }
        }

        void _children_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (IDataWrapper item in e.NewItems)
                {
                    item.Parent = this;
                }
            }

            if (e.OldItems != null)
            {
                foreach (IDataWrapper item in e.OldItems)
                {
                    if (ReferenceEquals(item.Parent, this))
                    {
                        item.Parent = null;
                    }
                }
            }
        }

        IAccessUnitModeProvider accessProvider;

        public string Id { get; private set; }

        public DataWrapperImpl(string id, IAccessUnitModeProvider _accessProvider)
        {
            InitValidation();
            // Берем на себя ошибки родителей которые не прикреплены в UI
            AddRule(new FuncValidationRule(this.ValidateNotAttachedParent));
            accessProvider = _accessProvider;
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

        public Func<object, object> Convert
        {
            get;
            set;
        }

        public Func<object, object> ConvertBack
        {
            get;
            set;
        }

        public void AddRule(ValidationRule rule)
        {
            rule.ValidatesOnTargetUpdated = true;
            rule.ValidationStep = ValidationStep.UpdatedValue;
            rules.Add(rule);
        }

        public void OnBeforeAttach(Binding b, DependencyObject target, DependencyProperty property)
        {
            if (Convert != null || ConvertBack != null)
            {
                b.Converter = new FuncValueConverter() { Convert = Convert, ConvertBack = ConvertBack };
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

        void SubscribeOnError()
        {
            IInputElement inp = currentTargetObject as IInputElement;
            if (inp != null)
            {
                inp.AddHandler(Validation.ErrorEvent, (RoutedEventHandler)OnError);
                inp.AddHandler(Binding.TargetUpdatedEvent, (RoutedEventHandler)OnTargetUpdated);
                inp.AddHandler(Binding.SourceUpdatedEvent, (RoutedEventHandler)OnSourceUpdated);
            }
        }

        void UnsubscribeOnError()
        {
            IInputElement inp = currentTargetObject as IInputElement;
            if (inp != null)
            {
                inp.RemoveHandler(Validation.ErrorEvent, (RoutedEventHandler)OnError);
                inp.RemoveHandler(Binding.TargetUpdatedEvent, (RoutedEventHandler)OnTargetUpdated);
                inp.RemoveHandler(Binding.SourceUpdatedEvent, (RoutedEventHandler)OnSourceUpdated);
            }
        }

        void OnTargetUpdated(object sender, RoutedEventArgs args)
        {
            if (!IsAttached)
            {
                UnsubscribeOnError();
            }
            else
            {
                ValidateDependent();
                //ValidateParent();
            }
        }

        void OnSourceUpdated(object sender, RoutedEventArgs args)
        {
            if (!IsAttached)
            {
                UnsubscribeOnError();
            }
            else
            {
                ValidateDependent();
            }
        }

        protected void ValidateDependent()
        {
            foreach (var wr in Dependent)
            {
                wr.Validate();
            }
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
        /// Делает валидацию родителей которые не прицеплены в UI
        /// проходит вверх пока не встретит прицепленного родителя
        /// прицепленный сам прицепит следующих родителей
        /// </summary>
        /// <returns></returns>
        protected ValidationResult ValidateNotAttachedParent()
        {
            var parent = this.Parent;
            while (parent != null)
            {
                if (!parent.IsAttached)
                {
                    parent.Validate();
                    var err = parent.Errors.FirstOrDefault();
                    if (err != null)
                        return new ValidationResult(false, err.ErrorContent);
                }
                else
                {
                    return ValidationResult.ValidResult;
                }
                parent = parent.Parent;
            }
            return ValidationResult.ValidResult;
        }

        TaskCompletionSource<Unit> ValidationTask;

        /// <summary>
        /// Выполним валидацию
        /// </summary>
        /// <returns></returns>
        public Task Validate()
        {
            lock (this)
            {
                if (ValidationTask != null)
                {
                    // Идет асинхронная валидация
                    return ValidationTask.Task;
                }
                // Для соблюдения лучших традиций метод по идее должен быть асинхронным,
                // тоесть запускать валидацию и все
                // А ошибки уже должны быть синхронно доступны в Errors
                if (!IsAttached)
                {
                    // пока что синхронно сделаем валидацию
                    ValidateExplicit();
                    var tcs = new TaskCompletionSource<Unit>();
                    tcs.SetResult(new Unit());
                    return tcs.Task;
                }
                else
                {
                    // байндинг умный - и лишний раз валидацию не дергает
                    // а нам надо заставить его, что бы в случае чего ошибка подцепилась в UI

                    // таким образом мы запускаем валидацию в байндинге,
                    // но асинхронная валидация будет идти асинхронноы
                    currentBindingExpression.UpdateSource();
                    if (AsyncRules.Count == 0)
                    {
                        // Асинхронной валидации нет, все сделано
                        var tcs = new TaskCompletionSource<Unit>();
                        tcs.SetResult(new Unit());
                        return tcs.Task;
                    }
                    // Создаем таск через который можно подождать окончания валидации
                    ValidationTask = new TaskCompletionSource<Unit>();
                }
                // Ждем асинхронную валидацию
                return ValidationTask.Task;
            }
        }

        protected IEnumerable<ValidationError> ValidateExplicit()
        {
            // Метод ну учитывает асинхронные проверки
            // работает только синхронно
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

                foreach (var e in LastError)
                    yield return e;
                if (AsyncValidationTask != null && !AsyncValidationTask.IsCompleted)
                {
                    // Если идет асинхронная валидация
                    // то дожидаемся её синхронно
                    AsyncValidationTask.Wait();
                    var res = AsyncValidationTask.Result;
                    if (res != null && !res.IsValid)
                    {
                        // И сразу возвращаем ошибку
                        // тк мы скорее всего в UI потоке, и ошибку в LastError записать не смогут
                        // пока мы держим управление
                        yield return new ValidationError(AsyncValidationRule, new object(), res.ErrorContent, null);
                    }
                }

                yield break;
            }
        }

        public bool IsValid
        {
            get
            {
                return LastError == null;
            }
        }

        #region IAccessUnit Members

        public void __RaiseModeChanged()
        {
            if (PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs("Mode"));
            }
        }

        public AccessUnitMode Mode
        {
            get
            {
                return accessProvider.GetMode(this);
            }
            set
            {
                accessProvider.SetMode(this, value);
            }
        }

        #endregion

        public object Uri
        {
            get;
            set;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public System.Globalization.CultureInfo CurrentThread { get; set; }
    }
}
