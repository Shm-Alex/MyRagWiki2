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
	public class MonthlyCS : CheckSchedule, IMonthlyCS
	{
		int _day = 1;
		Months _month = Months.Jan;

		public MonthlyCS()
		{
			isGoodDay = new IsGoodDay(MonthCheck);
			CheckTime = new DateTime(DateTime.Now.Year, 1, 1);
		}

		private bool MonthCheck(DateTime result)
		{
			int month = result.Month;
			if ((Month & Months.Jan) != 0 && month == 1)
				return true;
			if ((Month & Months.Feb) != 0 && month == 2)
				return true;
			if ((Month & Months.Mar) != 0 && month == 3)
				return true;
			if ((Month & Months.Apr) != 0 && month == 4)
				return true;
			if ((Month & Months.May) != 0 && month == 5)
				return true;
			if ((Month & Months.Jun) != 0 && month == 6)
				return true;
			if ((Month & Months.Jul) != 0 && month == 7)
				return true;
			if ((Month & Months.Aug) != 0 && month == 8)
				return true;
			if ((Month & Months.Sep) != 0 && month == 9)
				return true;
			if ((Month & Months.Oct) != 0 && month == 10)
				return true;
			if ((Month & Months.Nov) != 0 && month == 11)
				return true;
			if ((Month & Months.Dec) != 0 && month == 12)
				return true;
			return false;
		}
		private DateTime GetCorrectDate(DateTime result)
		{
			int day = Day;
			if (result.Day != day)
			{
				int limit = DateTime.DaysInMonth(result.Year, result.Month);
				if (day > limit)
					day = limit;
				if (result.Day != day)
					result = new DateTime(result.Year, result.Month, day, CheckTime.Hour, CheckTime.Minute, 0);
			}
			if (result.Hour != CheckTime.Hour || result.Minute != CheckTime.Minute)
			{
				result = new DateTime(result.Year, result.Month, result.Day, CheckTime.Hour, CheckTime.Minute, 0);
			}
			return result;
		}

		private IsGoodDay isGoodDay;
		protected override DateTime CalculateNextRun(DateTime? nextRun)
		{
			DateTime result;
			DateTime now = DateTime.Now;
			if (nextRun == null)
				result = GetCorrectDate(new DateTime(now.Year, now.Month, 1, CheckTime.Hour, CheckTime.Minute, 0));
			else
				result = nextRun.Value;

			while (result < now)
				result = CalculateNextRunStep(result);

			return result;
		}

		public override DateTime GetDatePriorStartingTime(DateTime nearest)
		{
			DateTime result = GetCorrectDate(new DateTime(nearest.Year, nearest.Month, 1, CheckTime.Hour, CheckTime.Minute, 0));
			do
			{
				result = GetCorrectDate(result.AddMonths(-1));
			} while (!isGoodDay(result));
			return result;
		}

		protected override DateTime CalculateNextRunStep(DateTime? nextRun)
		{
		    try
		    {
		        DateTime result = nextRun.Value;
		        DateTime correct = GetCorrectDate(result);
		        if (correct > result && isGoodDay(correct))
		            return correct;
		        else
		            result = correct;
		        do
		        {
		            result = GetCorrectDate(result.AddMonths(1));
		        } while (!isGoodDay(result));

		        return result;
		    }
		    catch (Exception e)
		    {
                Trace.Write("Некоректно задано расписание на месяц");
                return DateTime.Now.AddMonths(1);
		    }
			
		}

		private static PropertyChangedEventArgs MonthArgs = PropertyChangedHelper.CreateArgs<MonthlyCS>(cs => cs.Month);
		[XmlAttribute()]
		public Months Month
		{
			get { return _month; }
			set
			{
				if (value != _month)
				{
					_month = value;
					OnPropertyChanged(MonthArgs);
				}
			}
		}

		private static PropertyChangedEventArgs DayArgs = PropertyChangedHelper.CreateArgs<MonthlyCS>(cs => cs.Day);
		[XmlAttribute()]
		public int Day
		{
			get { return _day; }
			set
			{
				if (value != _day)
				{
					_day = value;
					OnPropertyChanged(DayArgs);
				}
			}
		}
	}
}
