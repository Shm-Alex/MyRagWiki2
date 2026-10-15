using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel;
using Framework.Misc;
using System.Windows.Input;
using Ksema.ObjectShell.WPF;
using System.Collections.ObjectModel;
using System.Windows;
using System.Collections.Specialized;
using System.Collections;
using D = System.Diagnostics.Debug;

namespace Ksema.Framework.Async
{
    public static class AsyncCommand
    {
        public static IAsyncCommand CreateCommand()
        {
            return new AsyncCommandVoid();
        }

        public static IAsyncCommand<T> CreateCommand<T>()
        {
            return new AsyncCommandImpl<T>();
        }

        public static IAsyncCommand<T, P> CreateCommand<T, P>()
        {
            return new AsyncCommandImpl<T,P>();
        }
    }

    public abstract class AsyncCommandBaseImpl : DispatcherPropertyChangedHelper, IAsyncCommandBase
    {
        FastSubject<Unit> _started;
        FastSubject<Unit> _completed;
        
        void OnStarted()
        {
            if (_started != null)
                _started.OnNext(new Unit());
        }

        void OnCompleted()
        {
            if (_completed != null)
                _completed.OnNext(new Unit());
        }

        public IObservable<Unit> Started { get { if(_started == null) _started = new FastSubject<Unit>(); return _started; } }
        public IObservable<Unit> Completed { get { if (_completed == null) _completed = new FastSubject<Unit>(); return _completed; } }

        public IExtendedCommand WaitFor(IExtendedCommand command)
        {
            this.Group.Add(command);
            var c = command as AsyncCommandBaseImpl;
            if(c != null)
            {
                c.Completed.Subscribe(RaiseCanExecuteChanged);
            }
            return this;
        }

        public IExtendedCommand WaitMutually(IExtendedCommand command)
        {
            this.WaitFor(command);
            command.WaitFor(this);
            return this;
        }

        #region ModeProperty
        public static readonly PropertyChangedEventArgs ModeArgs = PropertyChangedHelper.CreateArgs<AsyncCommandBaseImpl>(c => c.Mode);
        private AsyncCommandMode _Mode = AsyncCommandMode.Single;

        public AsyncCommandMode Mode
        {
            get
            {
                return _Mode;
            }
            set
            {
                var oldValue = Mode;
                _Mode = value;
                if (oldValue != value)
                {
                    OnModeChanged(oldValue, value);
                    OnPropertyChanged(ModeArgs);
                }
            }
        }

        protected virtual void OnModeChanged(AsyncCommandMode oldValue, AsyncCommandMode newValue)
        {
        }
        #endregion


        IAsyncCommandGroup _group;
        public IAsyncCommandGroup Group
        {
            get
            {
                if (_group == null)
                {
                    _group = new AsyncCommandGroup() { this };
                }
                return _group;
            }
            set
            {
                if (this._group != null)
                {
                    _group.Remove(this);
                    value.Add(this);
                    _group = value;
                }
            }
        }

        protected virtual bool CanExecuteImpl(object parameter)
        {
            return true;
        }

        protected bool CanExecute(object parameter)
        {
            if (Mode == AsyncCommandMode.Once && runOnce)
            {
                return false;
            }
            else if (Group.IsWorking)
            {
                return false;
            }
            return CanExecuteImpl(parameter);
        }

        protected virtual void Execute(object parameter)
        {
        }

        public bool IsAutomaticRequeryDisabled
        {
            get
            {
                return _isAutomaticRequeryDisabled;
            }
            set
            {
                if (_isAutomaticRequeryDisabled != value)
                {
                    if (value)
                    {
                        CommandManagerHelper.RemoveHandlersFromRequerySuggested(_canExecuteChangedHandlers);
                    }
                    else
                    {
                        CommandManagerHelper.AddHandlersToRequerySuggested(_canExecuteChangedHandlers);
                    }
                    _isAutomaticRequeryDisabled = value;
                }
            }
        }

        internal void RaiseCanExecuteChanged()
        {
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.BeginInvoke((Action)OnCanExecuteChanged);
            }
            else
            {
                OnCanExecuteChanged();
            }
        }

        protected virtual void OnCanExecuteChanged()
        {
            CommandManagerHelper.CallWeakReferenceHandlers(_canExecuteChangedHandlers);
        }

        #region ICommand Members

        /// <summary>
        ///     ICommand.CanExecuteChanged implementation
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (!_isAutomaticRequeryDisabled)
                {
                    CommandManager.RequerySuggested += value;
                }
                CommandManagerHelper.AddWeakReferenceHandler(ref _canExecuteChangedHandlers, value, 2);
            }
            remove
            {
                if (!_isAutomaticRequeryDisabled)
                {
                    CommandManager.RequerySuggested -= value;
                }
                CommandManagerHelper.RemoveWeakReferenceHandler(_canExecuteChangedHandlers, value);
            }
        }

        bool ICommand.CanExecute(object parameter)
        {
            IsCanExecute = CanExecute(parameter);
            return IsCanExecute;
        }

        void ICommand.Execute(object parameter)
        {
            Execute(parameter);
        }

        #endregion

        public static readonly PropertyChangedEventArgs IsCanExecuteArgs = PropertyChangedHelper.CreateArgs<DelegateCommand>(c => c.IsCanExecute);
        private bool _IsCanExecute;

        public bool IsCanExecute
        {
            get
            {
                return _IsCanExecute;
            }
            private set
            {
                var oldValue = _IsCanExecute;
                _IsCanExecute = value;
                if (oldValue != value)
                {
                    OnIsCanExecuteChanged(oldValue, value);
                    OnPropertyChanged(IsCanExecuteArgs);
                }
            }
        }

        protected virtual void OnIsCanExecuteChanged(bool oldValue, bool newValue)
        {
        }


        #region Data

        private bool _isAutomaticRequeryDisabled = false;
        private List<WeakReference> _canExecuteChangedHandlers;

        #endregion

        static TaskCompletionSource<Unit> _AlwaysCompletedTask;
        protected static Task AlwaysCompletedTask
        {
            get
            {
                if (_AlwaysCompletedTask == null)
                {
                    _AlwaysCompletedTask = new TaskCompletionSource<Unit>();
                    _AlwaysCompletedTask.SetResult(new Unit());
                }
                return _AlwaysCompletedTask.Task;
            }
        }

        #region TaskProperty
        public static readonly PropertyChangedEventArgs TaskArgs = PropertyChangedHelper.CreateArgs<AsyncCommandBaseImpl>(c => c.Task);
        private Task _Task;

        public Task Task
        {
            get
            {
                if (_Task == null)
                {
                    return AlwaysCompletedTask;
                }
                return _Task;
            }
            private set
            {
                var oldValue = Task;
                _Task = value;
                if (oldValue != value)
                {
                    OnTaskChanged(oldValue, value);
                    OnPropertyChanged(TaskArgs);
                }
            }
        }

        protected virtual void OnTaskChanged(Task oldValue, Task newValue)
        {
            OnPropertyChanged(IsWorkingArgs);
            if (newValue != null)
            {
                newValue.ContinueWith((t) => OnPropertyChanged(IsWorkingArgs));
            }
        }
        #endregion


        #region IsWorkingProperty
        public static readonly PropertyChangedEventArgs IsWorkingArgs = PropertyChangedHelper.CreateArgs<AsyncCommandBaseImpl>(c => c.IsWorking);
        private bool _IsWorking;

        public bool IsWorking
        {
            get
            {
                return Task != null && !Task.IsCompleted;
            }
        }
        #endregion

        private CancellationTokenSource TokenSource;
        private CancellationToken Token
        {
            get
            {
                if (TokenSource == null)
                    return CancellationToken.None;
                return TokenSource.Token;
            }
        }

        bool runOnce = false;
        protected Task RunImpl(Func<CancellationToken,IEnumerable<IAsync>> run)
        {
            if (Mode == AsyncCommandMode.Once)
            {
                if (runOnce)
                    return Task;
                lock (this)
                {
                    runOnce = true;
                    Task = run(Token).RunThreadPool(Token);

                    Task.ContinueWith((t) =>
                    {
                        OnTaskCompleted(t);
                    });
                    Task.ContinueWith(OnTaskError, Token, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Current);

                    RaiseCanExecuteChanged();
                    OnStarted();
                }
            }
            else if (Mode == AsyncCommandMode.Single)
            {
                if (IsWorking)
                    return Task;
                lock (this)
                {
                    TokenSource = new CancellationTokenSource();
                    Task = run(Token).RunThreadPool(Token);

                    Task.ContinueWith((t) =>
                        {
                            OnTaskCompleted(t);
                        });
                    Task.ContinueWith(OnTaskError, Token, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Current);

                    RaiseCanExecuteChanged();
                    OnStarted();
                }
            }
            else if (Mode == AsyncCommandMode.SingleCancelWaitRun)
            {
                lock (this)
                {
                    if (Task != null && !Task.IsCompleted)
                    {
                        // Отменяем текущую задачу
                        // и ставим в очередь ещё одну
                        NextJob = run;
                        TokenSource.Cancel();
                    }
                    else
                    {
                        // Вместо натурального таска вернем TCS
                        // что бы в случае перезапуска, внешние пользователи видели все как одну выполняющуюся задачу
                        TCS = new TaskCompletionSource<Unit>();
                        Task = TCS.Task;

                        TokenSource = new CancellationTokenSource();
                        var job = run(Token).RunThreadPool(Token);

                        job.ContinueWith((t) =>
                        {
                            OnTaskCompleted(t);
                        });
                        job.ContinueWith(OnTaskError, Token, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Current);

                        RaiseCanExecuteChanged();
                        OnStarted();
                    }
                }
            }
            else
            {
                throw new Exception("Not supported mode " + Mode);
            }
            OnPropertyChanged(IsWorkingArgs);
            return Task;
        }

        void OnTaskError(Task task)
        {
            OnPropertyChanged(IsWorkingArgs);
            RaiseCanExecuteChanged();
            OnCompleted();
            this.Resolve<IDialogService>().ErrorMessage(task.Exception);
        }

        Func<CancellationToken, IEnumerable<IAsync>> NextJob;

        void OnTaskCompleted(Task task)
        {
            if (Mode == AsyncCommandMode.SingleCancelWaitRun)
            {
                lock (this)
                {
                    if (NextJob == null)
                    {
                        // Если очередь пуста, то мы действительно закончили
                        TCS.SetResult(new Unit());
                    }
                    else
                    {
                        // Если надо далее запустить ещё раз
                        // то запускаем
                        TokenSource = new CancellationTokenSource();
                        var job = NextJob(TokenSource.Token).RunThreadPool(TokenSource.Token);
                        NextJob = null;

                        job.ContinueWith((t) =>
                        {
                            OnTaskCompleted(t);
                        });

                        RaiseCanExecuteChanged();
                        return;
                    }
                }
            }

            OnPropertyChanged(IsWorkingArgs);
            RaiseCanExecuteChanged();
            OnCompleted();
        }

        void RaiseCanExecutedChanged(IEnumerable<AsyncCommandBaseImpl> commands)
        {
            foreach (var c in commands)
                c.RaiseCanExecuteChanged();
        }

        TaskCompletionSource<Unit> TCS;

        public void Cancel()
        {
            lock (this)
            {
                if (TokenSource != null)
                {
                    TokenSource.Cancel();
                }
                if (Mode == AsyncCommandMode.SingleCancelWaitRun)
                {
                    NextJob = null;
                }
            }
        }
    }

    public class AsyncCommandVoid : AsyncCommandBaseImpl, IAsyncCommand
    {
        public Func<bool> CanExecuteMethod { get; set; }

        public Func<CancellationToken, IEnumerable<IAsync>> Worker
        {
            get;
            set;
        }

        public Task Run()
        {
            return RunImpl((t) => Worker(t));
        }

        protected override bool CanExecuteImpl(object parameter)
        {
            if (CanExecuteMethod == null)
            {
                return true;
            }
            return CanExecuteMethod();
        }

        protected override void  Execute(object parameter)
        {
            Run();
        }
    }

    class AsyncCommandImpl<P1> : AsyncCommandBaseImpl, IAsyncCommand<P1>
    {
        public Func<P1, bool> CanExecuteMethod { get; set; }

        public Task Run(P1 p1)
        {
            return RunImpl((t) => Worker(p1,t));
        }

        public Func<P1, CancellationToken, IEnumerable<IAsync>> Worker
        {
            get;
            set;
        }

        protected override bool CanExecuteImpl(object parameter)
        {
            if (CanExecuteMethod == null)
            {
                return true;
            }
            else if (parameter == null)
            {
                return CanExecuteMethod(default(P1));
            }
            else if (parameter is P1)
            {
                return CanExecuteMethod((P1)parameter);
            }
            else
            {
                throw new InvalidOperationException("Wrong parameter type " + parameter.GetType().FullName + ", expecting " + typeof(P1));
            }
        }

        protected override void Execute(object parameter)
        {
            if (parameter == null)
            {
                Run(default(P1));
            }
            else if(parameter is P1)
            {
                Run((P1)parameter);
            }
            else
            {
                throw new InvalidOperationException("Wrong parameter type, expecting " + typeof(P1));
            }
        }
    }

    class AsyncCommandMultiParameter : AsyncCommandBaseImpl
    {
        public Func<object, bool> CanExecuteMethod { get; set; }
        public Action<object> ExecuteMethod { get; set; }

        protected override bool CanExecuteImpl(object parameter)
        {
            if (CanExecuteMethod == null)
            {
                throw new InvalidOperationException("CanExecuteMethod is mandatory for multi parameter IAsyncCommand");
            }
            return CanExecuteMethod(parameter);
        }

        protected override void Execute(object parameter)
        {
            if (ExecuteMethod == null)
            {
                throw new InvalidOperationException("ExecuteMethod is mandatory for multi parameter IAsyncCommand");
            }
            ExecuteMethod(parameter);
        }
    }

    class AsyncCommandImpl<P1, P2> : AsyncCommandMultiParameter, IAsyncCommand<P1, P2>
    {
        public Task Run(P1 p1,P2 p2)
        {
            return RunImpl((t) => Worker(p1, p2, t));
        }

        public Func<P1, P2, CancellationToken, IEnumerable<IAsync>> Worker
        {
            get;
            set;
        }
    }

	class AsyncCommandImpl<P1, P2,P3> : AsyncCommandMultiParameter, IAsyncCommand<P1, P2,P3>
	{
		public Task Run(P1 p1, P2 p2,P3 p3)
		{
			return RunImpl((t) => Worker(p1, p2, p3, t));
		}

		public Func<P1, P2,P3, CancellationToken, IEnumerable<IAsync>> Worker
		{
			get;
			set;
		}
	}

    public enum AsyncCommandMode
    {
        /// <summary>
        /// Всего один вызов, для инициализации чего-либо
        /// </summary>
        Once,
        /// <summary>
        /// Только один процесс выполнения. Eсли уже выполняется, то ничего не делаем
        /// </summary>
        Single,
        /// <summary>
        /// !!! Не реализован !!!
        /// Только один процесс выполнения. Eсли уже выполняется, то ждем завершения и запускаемся
        /// </summary>
        SingleWaitRun,
        /// <summary>
        /// Eсли уже выполняется, то отменяем задачу, дожидаемся отмены и запускаемся
        /// </summary>
        SingleCancelWaitRun,
        /// <summary>
        /// !!! Не реализован !!!
        /// Множественные запуски
        /// </summary>
        Multiple
    }

    public interface IExtendedCommand : INotifyPropertyChanged, ICommand
    {
        /// <summary>
        /// Запуск запрещен если другая команда выполняется
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        IExtendedCommand WaitFor(IExtendedCommand command);
        /// <summary>
        /// Обе команды ждут друг друга
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        IExtendedCommand WaitMutually(IExtendedCommand command);

        bool IsWorking { get; }

        IObservable<Unit> Started { get; }
        IObservable<Unit> Completed { get; }
    }

    public interface ISyncCommand : IExtendedCommand
    {
        Action Worker { get; set; }
        Func<bool> CanExecuteMethod { get; set; }
    }

    public interface ISyncCommand<T> : IExtendedCommand
    {
        Action<T> Worker { get; set; }
        Func<T,bool> CanExecuteMethod { get; set; }
    }

    public interface IAsyncCommandBase : IExtendedCommand
    {
        AsyncCommandMode Mode { get; set; }

        void Cancel();
        Task Task { get; }
    }

    public interface IAsyncCommand : IAsyncCommandBase, ICommand
    {
        Func<CancellationToken, IEnumerable<IAsync>> Worker { get; set; }
        Func<bool> CanExecuteMethod { get; set; }
        Task Run();
    }

    public interface IAsyncCommand<P1> : IAsyncCommandBase
    {
        Func<P1,CancellationToken,IEnumerable<IAsync>> Worker { get; set; }
        Func<P1, bool> CanExecuteMethod { get; set; }
        Task Run(P1 p1);
    }

    public interface IAsyncCommandMultiParameter : IAsyncCommandBase
    {
        Action<object> ExecuteMethod { get; set; }
        Func<object, bool> CanExecuteMethod { get; set; }
    }

    public interface IAsyncCommand<P1, P2> : IAsyncCommandMultiParameter
    {
        Func<P1, P2, CancellationToken, IEnumerable<IAsync>> Worker { get; set; }
        Task Run(P1 p1, P2 p2);
    }

    public interface IAsyncCommand<P1, P2, P3> : IAsyncCommandMultiParameter
    {
        Func<P1,P2,P3, CancellationToken, IEnumerable<IAsync>> Worker { get; set; }
        Task Run(P1 p1, P2 p2, P3 p3);
    }

    public interface IAsyncCommand<P1, P2, P3, P4> : IAsyncCommandMultiParameter
    {
        Func<P1,P2,P3,P4, CancellationToken, IEnumerable<IAsync>> Worker { get; set; }
        Task Run(P1 p1, P2 p2, P3 p3, P4 p4);
    }
}
