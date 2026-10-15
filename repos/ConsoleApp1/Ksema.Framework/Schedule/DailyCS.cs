using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Framework.Misc;
using Framework.Schedule;

namespace Framework.Schedule
{
	[Serializable]
	public class DailyCS : CheckSchedule, IDailyCS
	{
		int _interval = 1;
		DayTypes _dayType = DayTypes.Everyday;

		private bool WeekdaysCheck(DateTime result)
		{
			DayOfWeek day = result.DayOfWeek;
			if (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday)
				return false;
			else
				return true;
		}
		private bool RestdaysCheck(DateTime result)
		{
			DayOfWeek day = result.DayOfWeek;
			if (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday)
				return true;
			return false;
		}

		IsGoodDay isGoodDay = null;
		int dayInterval = 1;
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
				result = result.AddDays(-dayInterval);
			} while (isGoodDay != null && !isGoodDay(result));
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
		            result = result.AddDays(dayInterval);
		        } while (isGoodDay != null && !isGoodDay(result));

		        return result;
		    }
		    catch (Exception e)
		    {
		        Trace.Write("Некоректно задано дневное расписание задачи");
		        return DateTime.Now.AddDays(1);
            }
			
		}

		private static PropertyChangedEventArgs IntervalArgs = PropertyChangedHelper.CreateArgs<DailyCS>(cs => cs.Interval);

		[XmlAttribute]
		public int Interval
		{
			get { return _interval; }
			set
			{
				_interval = value;
				OnPropertyChanged(IntervalArgs);
			}
		}

		private static PropertyChangedEventArgs DayTypeArgs = PropertyChangedHelper.CreateArgs<DailyCS>(cs => cs.DayType);

		[XmlAttribute]
		public DayTypes DayType
		{
			get { return _dayType; }
			set
			{
				if (value != _dayType)
				{
					_dayType = value;
					switch (DayType)
					{
						case DayTypes.Interval:
							dayInterval = Interval;
							break;
						case DayTypes.Everyday:
							break;
						case DayTypes.Weekdays:
							isGoodDay = new IsGoodDay(WeekdaysCheck);
							break;
						case DayTypes.Restdays:
							isGoodDay = new IsGoodDay(RestdaysCheck);
							break;
						default:
							throw new NotSupportedException("Unknown Day Type - " + DayType);
					}
					OnPropertyChanged(DayTypeArgs);
				}
			}
		}
	}
}
