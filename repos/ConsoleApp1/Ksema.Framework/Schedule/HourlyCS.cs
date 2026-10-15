using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Framework.Misc;

namespace Framework.Schedule
{
	[Serializable]
	public class HourlyCS : CheckSchedule, IHourlyCS
	{
		int _interval = 1;

		protected override DateTime CalculateNextRun(DateTime? nextRun)
		{
			DateTime result;
			DateTime now = DateTime.Now;
			if (nextRun == null)
				result = now;
			else
				result = nextRun.Value;


			while (result < now)
				result = CalculateNextRunStep(result);

			return result;
		}

		public override DateTime GetDatePriorStartingTime(DateTime nearest)
		{
			//DateTime newTime = new DateTime(nearest.Year, nearest.Month, nearest.Day, CheckTime.Hour, CheckTime.Minute, 0);
			double dInt = Interval;
			DateTime newTime = nearest.AddHours(-dInt / 60); //Теперь в минутах

			return newTime;
		}

		protected override DateTime CalculateNextRunStep(DateTime? nextRun)
		{
		    try
		    {
		        DateTime result;
		        result = nextRun.Value;

		        double dInt = Interval;
		        result = result.AddHours(dInt / 60); //Теперь в минутах

		        return result;
		    }
		    catch (Exception e)
		    {
		        Trace.Write("Некоректно задано часовое расписание");
                return DateTime.Now.AddHours(1);
		    }
			
		}

		private static PropertyChangedEventArgs IntervalArgs = PropertyChangedHelper.CreateArgs<HourlyCS>(cs => cs.Interval);
		[XmlAttribute]
		public int Interval
		{
			get { return _interval; }
			set
			{
				if (value != _interval)
				{
					_interval = value;
					OnPropertyChanged(IntervalArgs);
				}
			}
		}
	}
}
