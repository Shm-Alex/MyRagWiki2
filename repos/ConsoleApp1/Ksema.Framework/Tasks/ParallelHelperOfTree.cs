using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Framework.Utils;

namespace System.Threading.Tasks
{
	public class ParallelHelperOfTree
    {
        private readonly int _concurrencyLevel;
        private readonly int _length;
        private ConcurrentQueue<Action> _actions;
        private readonly int _threadMaxCount;
        private int _threadCount;
        private List<Exception> _errors;
        private event Enqueue EnqueueEvent;
        private delegate void Enqueue();
        private bool _isWaitQueue;
        private int _tryCount;

        public ParallelHelperOfTree(Action[] actions, int concurrencyLevel) : this(actions, concurrencyLevel, false) { }
        public ParallelHelperOfTree(Action[] actions, int concurrencyLevel, bool dynamicQueue)
        {
            if (concurrencyLevel <= 0)
                concurrencyLevel = 1;
            _actions = new ConcurrentQueue<Action>(actions);
            _threadMaxCount = concurrencyLevel;
            _concurrencyLevel = Math.Min(_actions.Count, concurrencyLevel);
            _length = actions.Length;
            _errors = new List<Exception>();
            _isWaitQueue = dynamicQueue;
            _tryCount = 0;

            if (EnqueueEvent == null) EnqueueEvent += OnEnqueueEvent;
        }

        public void AddAcionToQueue(List<Action> actions)
        {
            if (actions == null || actions.Count == 0)
                return;

            actions.ForEach(a => _actions.Enqueue(a));
            EnqueueEvent?.Invoke();
        }
        
        public void StartQueue(ParallelType type)
        {
            if (_length == 0)
                return;
            try
            {
                switch (type)
                {
                    case ParallelType.Thread:
                        var threads = Enumerable.Range(0, _concurrencyLevel).Select(i => new Thread(StartThread)).ToList();
                        _threadCount = threads.Count;
                        threads.ForEach(thread => thread.Start());
                        threads.ForEach(thread => thread.Join());
                        break;
                    //case ParallelType.Task:
                    //    var scheduler = new LimitedConcurrencyLevelTaskScheduler(_concurrencyLevel);
                    //    var factory = new TaskFactory(scheduler);
                    //    var tasks = Enumerable.Range(0, _length)
                    //        .Select(i => factory.StartNew(StartTask, CancellationToken.None, TaskCreationOptions.None, scheduler))
                    //        .ToList();
                    //    tasks.ForEach(task => task.Wait());
                    //    break;
                    //case ParallelType.ParallelInvoke:
                    //    var options = new ParallelOptions();
                    //    options.MaxDegreeOfParallelism = _concurrencyLevel;
                    //    Parallel.Invoke(options, _actions.Select<Action, Action>(i => () => { RunAction(i); }).ToArray());
                    //    break;
                    default:
                        throw new ApplicationException("Unknown parallel task type - " + type.ToString());
                }
            }
            finally
            {
                //TODO: для того, чтобы закрылись все дополнительные потоки
                for (int i = 0; i < 1000; i++)
                {
                    if (_threadCount == 0)
                        break;

                    Thread.Sleep(200);
                }

                if (EnqueueEvent != null) EnqueueEvent -= OnEnqueueEvent;
            }

            if (_threadCount > 0)
                _errors.Add(new Exception("Не все потоки с расчетами завершились"));

            lock (this)
            {
                if (_errors.Count > 0)
                    throw new CompositeException(_errors);
            }
        }

        public void CompleteAdding()
        {
            lock (this)
                _isWaitQueue = false;
        }

        private void StartTask()
        {
            var action = GetNextAction();
            if (action != null)
            {
                RunAction(action);
            }
        }

        private void StartThread()
        {
            while (true)
            {
                var action = GetNextAction();
                if (action == null)
                {
                    //сделать доп. счетчкик для выхода?
                    if (!_isWaitQueue || _tryCount > _threadMaxCount * 100)
                    {
                        Interlocked.Decrement(ref _threadCount);

                        return;
                    }

                    Interlocked.Increment(ref _tryCount);

                    // TODO: для недопущения закрытия потоков до завершения наполнения очереди на выполнение через CompleteAdding()
                    Thread.Sleep(1000);
                }
                else
                {
                    RunAction(action);

                    if (_tryCount == 0)
                        continue;

                    lock (this)
                    {
                        _tryCount = 0;
                    }
                }
            }
        }

        private void RunAction(Action action)
        {
            try
            {
                action();
            }
            catch (Exception error)
            {
                lock (this)
                {
                    _errors.Add(error);
                }
            }
        }
        
        private Action GetNextAction()
        {
            try
            {
                if (_actions.TryDequeue(out var action))
                    return action;
            }
            catch (Exception)
            {
                lock (this)
                    _errors.Add(new Exception("Не получилось получить следующий элемент очереди на выполенение"));
            }

            return null;
        }

        private void OnEnqueueEvent()
        {
            // чтобы не плодить лишние потоки - формирование новых будет только если в очереди будет более 2 элементов
            var actionsCount = _actions.Count;
            if (_threadCount >= _threadMaxCount || _threadCount > actionsCount)
                return;

            var diff = _threadMaxCount - _threadCount;
            var addThread = actionsCount - _threadCount > diff
                ? diff
                : actionsCount - _threadCount;

            lock (this)
            {
                _threadCount += addThread;
            }

            var addThreads = Enumerable.Range(0, addThread).Select(t => new Thread(StartThread)).ToArray();
            addThreads.ForEach(thread => thread.Start());
        }
    }
}
