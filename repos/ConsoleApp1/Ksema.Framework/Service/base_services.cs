using System;
using System.Collections;
using System.IO;

namespace Framework.AppService
{
	using Framework.Utils;

	public class GCWorker : WorkerClassBase
	{
		public GCWorker(IServiceSchedule schedule, IExceptionHandler exHandler) : base(schedule, exHandler, false)
		{
		}

		protected override void runAction()
		{
			GC.Collect();
		}
	}

	public class FixedIntervalSchedule : IServiceSchedule
	{
		TimeSpan _tts;

		public FixedIntervalSchedule(TimeSpan tts)
		{
			_tts = tts;
		}

		public void reset()
		{			
		}

		public TimeSpan getNextTTS(bool actualRun)
		{
			return _tts;
		}

		public bool IsDone
		{
			get { return false; }
		}

		public object createObject(IDictionary data)
		{
			string tts = (string) data["tts"];
			if(Utils.isEmpty(tts))
				throw new ArgumentException("'tts' for schedule not set");
			//ws][-][d.]hh:mm:ss[.ff][ws]
			TimeSpan interval = TimeSpan.Parse(tts);
			return new FixedIntervalSchedule(interval);
		}
	}

	public class FiniteSchedule : IServiceSchedule
	{
		int _count;
		int _maxCount;
		TimeSpan _tts;

		public FiniteSchedule(int maxCount, TimeSpan tts)
		{
			_maxCount = maxCount;
			_tts = tts;
			reset();
		}

		public FiniteSchedule(int maxCount) : this(maxCount, TimeSpan.Zero)
		{
		}

		public FiniteSchedule() : this(1)
		{
		}

		public void reset()
		{
			_count = 0;
		}

		public TimeSpan getNextTTS(bool actualRun)
		{
			if(actualRun)
				_count ++;
			return _tts;
		}

		public bool IsDone
		{
			get 
			{ 
				return _count >= _maxCount;
			}
		}

		public object createObject(IDictionary data)
		{
			string tts = (string) data["tts"];
			string sCount = (string)data["count"];
			int maxCount = Utils.isEmpty(sCount) ? 1 : int.Parse(sCount);
			//ws][-][d.]hh:mm:ss[.ff][ws]
			TimeSpan interval = Utils.isEmpty(tts) ? TimeSpan.Zero : TimeSpan.Parse(tts);
			return new FiniteSchedule(maxCount, interval);
		}
	}

	public class ExceptionHandler : IExceptionHandler
	{
		TimeSpan _exTimeout;
		DateTime _lastErrorTime;
		volatile Exception _lastError;

		public ExceptionHandler(TimeSpan exTimeout)
		{
			_exTimeout = exTimeout;
			reset();
		}

		public void reset()
		{
			lock(this)
			{
				_lastError = null;
				_lastErrorTime = DateTime.MinValue;
			}
		}

		void IExceptionHandler.handleException(Exception error)
		{
			if(error == null)
				return;

			lock(this)
			{
				DateTime time = DateTime.Now;
				TimeSpan interval = time - _lastErrorTime;

				if (LastErrorMessage != error.Message || interval >= _exTimeout)
				{
					_lastError = error;
					_lastErrorTime = time;

					if(HandleError != null)
						HandleError(this, new ErrorEventArgs(error));
				}
			}
		}

		string LastErrorMessage
		{
			get 
			{ 
				return (_lastError == null) ? "" : _lastError.Message; 
			}
		}

		public TimeSpan ExceptionTimeout
		{
			get { return _exTimeout; }
			set { _exTimeout = value; }
		}

		public event ErrorEventHandler HandleError;

		public Exception LastError
		{
			get 
			{ 
				return _lastError; 
			}
		}

		public DateTime LastErrorTime
		{
			get { return _lastErrorTime; }
		}

	}

}