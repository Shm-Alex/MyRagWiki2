using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using System.Windows.Threading;
using System.Diagnostics;
using System.Concurrency;
using Ksema.Framework;
using System.Xml.Serialization;
using Ksema.Framework.Misc;

namespace System
{
    // TODO: Сделать Run который бы выполнял в том же потоке где он был запущен
    // TODO: Сделать поддержку исключений: yield return (exception) => { exception handler }
    public static class AsyncHelper
    {
        //public static IDisposable Subscribe<T>(this IObservable<T> source, Func<T, IEnumerable<IAsync>> onNext)
        //{
        //    return source.Subscribe(v => onNext(v).Run());
        //}

        public static Task OnNotRanToCompletion(this Task task, Action<Task> onError)
        {
            return task.ContinueWith((t) => onError(t), TaskContinuationOptions.NotOnRanToCompletion);
        }

        public static IResult<T> AsResult<T>(this T obj)
        {
            return new AsyncResult<T>(obj);
        }

        public static Task Run(this IEnumerable<IAsync> en, Dispatcher dispatcher)
        {
            if (dispatcher == Dispatcher.CurrentDispatcher)
            {
                // Мы в диспетчере - шедулить не надо
                return Run(en);
            }
            else
            {
                var syncContext = new DispatcherSynchronizationContext(dispatcher);
                return Run(en, syncContext);
            }
        }

        public static Task Run(this IEnumerable<IAsync> en, SynchronizationContext context = null)
        {
            return RunImpl<Nope>(en, context);
        }

        public static Task<T> Run<T>(this IEnumerable<IAsync> en)
        {
            return RunImpl<T>(en, null);
        }

        /// <summary>
        /// Запускает в текущем контексте синхронизации или в заданном
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="en"></param>
        /// <param name="scheduler">Если null и запущено в UI, то будет использован TaskScheduler.FromCurrentSynchronizationContext()</param>
        /// <returns></returns>
        private static Task<T> RunImpl<T>(IEnumerable<IAsync> en, SynchronizationContext context)
        {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
            bool needSchedule = context != null;

            if (context == null)
            {
                context = SynchronizationContext.Current;
            }
            var state = new RunState<T>()
            {
                Enumerator = en.GetEnumerator(),
                CompletionSource = tcs,
                SynchronizationContext = context
            };

            //Всегда сразу возвращаем управление
            if (context != null)
            {
                context.Post((s) =>
                {
                    RunWorker(state);
                }, null);
            }
            else
            {
                // Мы в пуле
                ThreadPool.QueueUserWorkItem((s) => { RunWorker(state); });
            }

            return tcs.Task;
        }

        private static Task<T> RunImpl<T>(IEnumerable<IAsync> en)
        {
            return RunImpl<T>(en, CancellationToken.None);
        }
        /// <summary>
        /// Запускает в ThreadPool
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="en"></param>
        /// <returns></returns>
        private static Task<T> RunImpl<T>(IEnumerable<IAsync> en, CancellationToken token)
        {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
            var state = new RunState<T>()
            {
                Enumerator = en.GetEnumerator(),
                CompletionSource = tcs,
                SynchronizationContext = null,
                Token = token
            };

            // Запихиваем обработку в пул, если запустить через Таск то возможна ситуация когда Таск быстро отменяют
            // и до RunWorker ничего не доходит
            ThreadPool.QueueUserWorkItem((s) => { RunWorker(state); });
            // Сразу отдаем управление
            return tcs.Task;
        }

        public static Task RunThreadPool(this IEnumerable<IAsync> en)
        {
            return RunImpl<Nope>(en);
        }

        public static Task RunThreadPool(this IEnumerable<IAsync> en, CancellationToken token)
        {
            return RunImpl<Nope>(en, token);
        }

        public static Task<T> RunThreadPool<T>(this IEnumerable<IAsync> en)
        {
            return RunImpl<T>(en);
        }

        private class RunState<T>
        {
            public CancellationToken Token;
            public IEnumerator<IAsync> Enumerator;
            public TaskCompletionSource<T> CompletionSource;
            //public TaskScheduler Scheduler;
            public SynchronizationContext SynchronizationContext;

            public PerfTimer _timer;
            public PerfTimer Timer
            {
                get
                {
                    if (_timer == null)
                    {
                        _timer = new PerfTimer();
                    }
                    return _timer;
                }
            }
        }

        private static void RunWorker<T>(RunState<T> state)
        {
            var en = state.Enumerator;
            var context = state.SynchronizationContext;
            var tcs = state.CompletionSource;

        StartAgain:
            bool res;
            try
            {
                if (!state.Timer.IsStarted)
                {
                    // Если таймер был остановлен, значит либо был тротлинг
                    // либо просто управление отадавалось
                    // запускаем его
                    state.Timer.Start();
                }
                res = en.MoveNext();
                // По идее далее можно переключаться в ThreadPool, если мы в контексте синхронизации
            }
            catch (Exception e)
            {
                en.Dispose();
                if (e is OperationCanceledException)
                {
                    tcs.SetCanceled();
                }
                else
                {
                    tcs.SetException(e);
                }
                return;
            }
            if (!res)
            {
                try
                {
                    tcs.SetResult(default(T));
                    return;
                }
                finally
                {
                    en.Dispose();
                }
            }

            if (en.Current is Throttle)
            {
                var currentWorkTime = state.Timer.GetCurrentDuration();
                var throttle = en.Current as Throttle;
                if (currentWorkTime >= throttle.IdealWorkTimeMilliseconds)
                {
                    // Тротлинг сработал - сбросим таймер
                    state.Timer.Stop();

                    // Бывает что в UI работы мало а в очереди диспетчера её оказывается много
                    // что делать в этом случае непонятно
                    // Можно Throttle.FromMilliseconds(0) - но непонятно как часто надо вызывать что бы и
                    // часто не дергать диспетчера и что бы UI все же не лочился

                    // Обманем всех - нам нужен самый низкий приоритет, что бы все могли отработать
                    // выполнение продолжиться в UI потоке, но вроде как в не UI потоке Throttle вызывать и незачем
                    Application.Current.Dispatcher.BeginInvoke((Action)(() => { RunWorker(state); }), DispatcherPriority.Background);
                    return;
                }
                else
                {
                    goto StartAgain;
                }
            }
            // Далее управление будет отдано
            // таймер останавливаем
            state.Timer.Stop();
            if (en.Current is AsyncContextSwitch)
            {
                var sw = en.Current as AsyncContextSwitch;
                state.SynchronizationContext = sw.Context;
                if (state.SynchronizationContext != null)
                {
                    sw.Context.Post((s) => { RunWorker(state); }, null);
                }
                else
                {
                    // Переключаемся в ThreadPool
                    Task.Factory.StartNew(() => { RunWorker(state); }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
                }
            }
            else if (en.Current is AsyncTask)
            {
                var asyncTask = en.Current as AsyncTask;
                if (asyncTask.Task == null)
                {
                    // Будем считать что таск завершен
                    goto StartAgain;
                }
                if (asyncTask.Task.Status == TaskStatus.Created)
                {
                    asyncTask.Task.Start();
                }

                Action<Task> continuation = (t) =>
                {
                    if (t.Exception != null)
                    {
                        tcs.SetException(t.Exception);
                        en.Dispose();
                        return;
                    }
                    if (t.IsCanceled)
                    {
                        tcs.SetCanceled();
                        en.Dispose();
                        return;
                    }

                    if (state.SynchronizationContext != null)
                    {
                        // Мы в ThreadPool, а у нас задан шедулер
                        state.SynchronizationContext.Post((s) => { RunWorker(state); }, null);
                    }
                    else
                    {
                        RunWorker(state);
                    }
                };

                // Обрабатываем все в ThreadPool, в continuation переключимся куда надо
                asyncTask.Task.ContinueWith(continuation, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            }
            else if (en.Current is IResult<T>)
            {
                var taskResult = en.Current as IResult<T>;
                tcs.SetResult(taskResult.Result);
                en.Dispose();
            }
            return;
        }
    }

    public class Throttle : IAsync
    {
        public Throttle(int idealWorkTimeMilliseconds)
        {
            this.IdealWorkTimeMilliseconds = idealWorkTimeMilliseconds;
        }

        public static IAsync FromMilliseconds(int workTime)
        {
            return new Throttle(workTime);
        }

        public static IAsync Yield
        {
            get
            {
                return new Throttle(0);
            }
        }

        public int IdealWorkTimeMilliseconds
        {
            get;
            private set;
        }
    }

    public class AsyncContextSwitch : IAsync
    {
        public static readonly AsyncContextSwitch ThreadPool = new AsyncContextSwitch();

        internal AsyncContextSwitch()
        {
        }

        public AsyncContextSwitch(SynchronizationContext context)
        {
            Context = context;
        }

        public AsyncContextSwitch(Dispatcher dispatcher)
        {
            Context = new DispatcherSynchronizationContext(dispatcher);
        }

        [XmlIgnore]
        public SynchronizationContext Context
        {
            get;
            private set;
        }
    }

    public class AsyncTask : IAsync
    {
        public static implicit operator AsyncTask(Task task)
        {
            return new AsyncTask(task);
        }

        public AsyncTask(Task task)
        {
            Task = task;
        }

        public Task Task
        {
            get;
            private set;
        }
    }

    public class AsyncResult<T> : IResult<T>
    {
        public AsyncResult(T result)
        {
            _result = result;
        }

        T _result;

        public override T Result
        {
            get { return _result; }
        }
    }

    public abstract class IResult<T> : IResult
    {
        public abstract T Result { get; }

        public override object ObjectResult
        {
            get { return Result; }
        }
    }

    public abstract class IResult : IAsync
    {
        public abstract object ObjectResult { get; }
    }

    public abstract class IAsync
    {
        public static implicit operator IAsync(Dispatcher dispatcher)
        {
            return new AsyncContextSwitch(dispatcher);
        }

        public static implicit operator IAsync(SynchronizationContext context)
        {
            return new AsyncContextSwitch(context);
        }

        public static implicit operator IAsync(Task t)
        {
            return new AsyncTask(t);
        }
    }

    sealed class Nope { }
}
