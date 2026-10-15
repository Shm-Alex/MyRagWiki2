using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Framework.Utils;
using System.Threading.Tasks;

namespace System.Threading.Tasks
{
	public enum ParallelType
	{
		Thread,
		Task,
		ParallelInvoke
	}

	public class ParallelHelper
	{
		private int concurrencyLevel;
		private int position = -1;
		private int length;
		private Action[] actions;
		private List<Exception> errors;
		public ParallelHelper(Action[] actions, int concurrencyLevel)
		{
		    if (concurrencyLevel <= 0)
		        concurrencyLevel = 1;
            this.actions = actions;
			this.concurrencyLevel = concurrencyLevel;
			length = actions.Length;
			errors = new List<Exception>();
		}

		public void StartQueue(ParallelType type)
		{
			if (length == 0)
				return;
			if (type == ParallelType.Thread)
			{
				var count = Math.Min(concurrencyLevel, length);
				var threads = Enumerable.Range(0, count).Select(i => new Thread(new ThreadStart(Start))).ToList();
				threads.ForEach(thread => thread.Start());
				threads.ForEach(thread => thread.Join());
			}
			else if (type == ParallelType.Task)
			{
				var scheduler = new LimitedConcurrencyLevelTaskScheduler(concurrencyLevel);
				var factory = new TaskFactory(scheduler);
				var tasks = Enumerable.Range(0, length).Select(i => factory.StartNew(StartTask, CancellationToken.None, TaskCreationOptions.None, scheduler)).ToList();
				tasks.ForEach(task => task.Wait());
			}
			else if (type == ParallelType.ParallelInvoke)
			{
				var options = new ParallelOptions();
				options.MaxDegreeOfParallelism = concurrencyLevel;
				Parallel.Invoke(options, actions.Select<Action, Action>(i => () => { RunAction(i); }).ToArray());
			}
			else
				throw new ApplicationException("Unknown parallel task type - " + type.ToString());
			if (errors.Count > 0)
				throw new CompositeException(errors);
		}

		private void StartTask()
		{
			var action = GetNextAction();
			if (action != null)
			{
				RunAction(action);
			}
		}

		private void Start()
		{
			while (true)
			{
				var action = GetNextAction();
				if (action == null)
					return;
				RunAction(action);
			}
		}

		private void RunAction(Action action)
		{
			try
			{
				action();
			}
			catch(Exception error)
			{
				lock (this)
				{
					errors.Add(error);
				}
			}
		}

		private Action GetNextAction()
		{
			int i;
			lock (this)
			{
				position++;
				i = position;
				if (i >= length)
					return null;
			}
			var action = actions[i];
			return action;
		}
	}
}
