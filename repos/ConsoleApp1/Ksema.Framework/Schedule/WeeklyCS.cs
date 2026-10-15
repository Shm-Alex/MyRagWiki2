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
	public class WeeklyCS : CheckSchedule, IWeeklyCS
	{
		int _interval = 1;
		WeekDays _weekDay = WeekDays.Mon;

		public WeeklyCS()
		{
			isGoodDay = new IsGoodDay(WeekdayCheck);
		}

		private IsGoodDay isGoodDay;

		private bool WeekdayCheck(DateTime result)
		{
			DayOfWeek day = result.DayOfWeek;
			if ((WeekDay & WeekDays.Mon) != 0 && day == DayOfWeek.Monday)
				return true;
			if ((WeekDay & WeekDays.Tue) != 0 && day == DayOfWeek.Tuesday)
				return true;
			if ((WeekDay & WeekDays.Wed) != 0 && day == DayOfWeek.Wednesday)
				return true;
			if ((WeekDay & WeekDays.Thu) != 0 && day == DayOfWeek.Thursday)
				return true;
			if ((WeekDay & WeekDays.Fri) != 0 && day == DayOfWeek.Friday)
				return true;
			if ((WeekDay & WeekDays.Sat) != 0 && day == DayOfWeek.Saturday)
				return true;
			if ((WeekDay & WeekDays.Sun) != 0 && day == DayOfWeek.Sunday)
				return true;
			return false;
		}

		protected override DateTime CalculateNextRun(DateTime? nextRun)
		{


			DateTime result;
			DateTime now = DateTime.Now;
			if (nextRun == null)
				result = new DateTime(now.Year, now.Month, now.Day, CheckTime.Hour, CheckTime.Minute, 0);
			else
				result = nextRun.Value;

			while (result < now)
				result = CalculateNextRunStep(result);

			return result;
		}

		public override DateTime GetDatePriorStartingTime(DateTime nearest)
		{
			DateTime result = GetCorrectDate(nearest);
			do
			{
				result = result.AddDays(-1);
				if (result.DayOfWeek == DayOfWeek.Monday && Interval > 1)
					result.AddDays(-(Interval - 1) * 7);
			} while (!isGoodDay(result));
			return result;
		}

		private DateTime GetCorrectDate(DateTime date)
		{
			return new DateTime(date.Year, date.Month, date.Day, CheckTime.Hour, CheckTime.Minute, 0);
		}

		protected override DateTime CalculateNextRunStep(DateTime? nextRun)
		{
		    try
		    {
		        DateTime result = nextRun.Value;
		        DateTime correct = GetCorrectDate(result);
		        if (correct > result && (isGoodDay == null || isGoodDay(correct)))
		            return correct;
		        else
		            result = correct;
		        do
		        {
		            result = result.AddDays(1);
		            if (result.DayOfWeek == DayOfWeek.Monday && Interval > 1)
		                result.AddDays((Interval - 1) * 7);
		        } while (!isGoodDay(result));

		        return result;
		    }
		    catch (Exception e)
		    {
		        Trace.Write("Некоректно задано недельное расписание задачи");
                return DateTime.Now.AddDays(7);
		    }
			
		}

		private static PropertyChangedEventArgs WeekDayArgs = PropertyChangedHelper.CreateArgs<WeeklyCS>(cs => cs.WeekDay);
		[XmlAttribute()]
		public WeekDays WeekDay
		{
			get { return _weekDay; }
			set
			{
				if (value != _weekDay)
				{
					_weekDay = value;
					OnPropertyChanged(WeekDayArgs);
				}
			}
		}

		private static PropertyChangedEventArgs IntervalArgs = PropertyChangedHelper.CreateArgs<WeeklyCS>(cs => cs.Interval);
		[XmlAttribute()]
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
