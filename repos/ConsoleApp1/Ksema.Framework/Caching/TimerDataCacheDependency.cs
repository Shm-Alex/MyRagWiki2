using System.Timers;
using System;
namespace Ksema.Framework.Caching
{
	public class TimerDataCacheDependency : DataCacheDependency
	{
		readonly Timer _timer;

		public TimerDataCacheDependency(TimeSpan invalidatePeriod)
		{
			_timer = new Timer(invalidatePeriod.TotalMilliseconds);
			_timer.AutoReset = true;
			_timer.Elapsed += _timer_Elapsed;
			_timer.Start();
		}

		void _timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			Invalidate();
		}

		public Timer Timer
		{
			get { return _timer; }
		}
	}
}