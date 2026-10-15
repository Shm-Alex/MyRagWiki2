using System;
using System.Collections;
using System.Threading;

namespace Framework.AppService
{
	public interface IServiceSchedule : IObjectFactory
	{
		void reset();
		TimeSpan getNextTTS(bool actualRun);
		bool IsDone{get;}
	}

	public interface IObjectFactory
	{
		object createObject(IDictionary data);
	}

	public interface IExceptionHandler
	{
		void reset();
		void handleException(Exception error);
	}

	public interface IServiceHost
	{
		void start();
		void stop();
		void pause();
		void resume();
		void join();
		bool IsStarted{get;}
		bool IsSuspended{get;}
	}

	public class ServiceHost : IServiceHost
	{
		WorkerClassBase _worker;
		Thread _workerThread;
		bool _started = false;

		public ServiceHost(WorkerClassBase worker, ThreadPriority threadPriority)
		{
			if(worker == null)
				throw new ArgumentNullException("worker");
			_worker = worker;
			_workerThread = new Thread(new ThreadStart(_worker.executeLoop));
			_workerThread.IsBackground = true;
			_workerThread.Priority = threadPriority;
		}

		public ServiceHost(WorkerClassBase worker) : this(worker, ThreadPriority.Normal)
		{
		}

		public void start()
		{			
			_workerThread.Start();
			_started = true;
		}

		public void join()
		{
			_workerThread.Join();
			_started = false;
		}

		public bool IsStarted
		{
			get { return _started; }
		}

		public bool IsSuspended
		{
			get { return _worker.IsSuspended; }
		}

		public void stop()
		{
			_workerThread.Abort();
			_workerThread.Join();

			_started = false;
		}

		public void pause()
		{
			_worker.pause();
		}

		public void resume()
		{
			_worker.resume();
		}

		static IServiceHost __emptyService = new EmptyServiceHost();
		public static IServiceHost EmptyService
		{
			get 
			{  
				return __emptyService;
			}
		}
	}

	class EmptyServiceHost : IServiceHost
	{
		public void start()
		{
			
		}

		public void stop()
		{
		}

		public void pause()
		{
		}

		public void resume()
		{
		}

		public void join()
		{
		}

		public bool IsStarted
		{
			get { return false; }
		}

		public bool IsSuspended
		{
			get { return false;; }
		}

	}

	public abstract class WorkerClassBase
	{
		IServiceSchedule _schedule;
		IExceptionHandler _exHandler;
		volatile bool _suspended = false;
		bool _breakOnError;

		public WorkerClassBase(IServiceSchedule schedule, IExceptionHandler exHandler, bool breakOnError)
		{
			if(schedule == null)
				throw new ArgumentNullException("schedule");

			_schedule = schedule;
			_exHandler = exHandler;
			_breakOnError = breakOnError;
		}

		protected abstract void runAction();

		public void executeLoop()
		{
			bool mustRun, alwaysBreak = true;
			_suspended = false;
			_schedule.reset();
			_exHandler.reset();

			while(true)
			{
				mustRun = !_suspended;
				alwaysBreak = true;

				try
				{
					if(_schedule.IsDone)
						break;
					TimeSpan tts = _schedule.getNextTTS(mustRun);
					if(tts.Ticks != 0)
						Thread.Sleep(tts);

					alwaysBreak = false;
					if(mustRun)
						runAction();
				}
				catch(ThreadAbortException)
				{
					break;
				}
				catch(Exception ex)
				{
					if(_exHandler != null)
						_exHandler.handleException(ex);
					if(alwaysBreak || _breakOnError)
						break;
				}
			}
		}

		public void pause()
		{
			_suspended = true;
		}

		public void resume()
		{
			_suspended = false;
		}

		public bool IsSuspended
		{
			get { return _suspended; }
		}

		protected IServiceSchedule Schedule
		{
			get { return _schedule; }
			set { _schedule = value; }
		}
	}

}
