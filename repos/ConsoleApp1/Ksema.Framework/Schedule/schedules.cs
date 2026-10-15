using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Framework.Misc;

namespace Framework.Schedule
{
    [Flags]
    public enum Months { Jan = 1, Feb = 2, Mar = 4, Apr = 8, May = 16, Jun = 32, Jul = 64, Aug = 128, Sep = 256, Oct = 512, Nov = 1024, Dec = 2048 }

    [Flags]
    public enum WeekDays { Mon = 1, Tue = 2, Wed = 4, Thu = 8, Fri = 16, Sat = 32, Sun = 64 }

    public enum DayTypes { Everyday = 1, Weekdays = 2, Restdays = 4, Interval = 8 }

    #region Interface

    public interface ICheckSchedule
    {
        DateTime? NextRun { get; set; }

        void UpdateNextRun();
    }

    public interface IDailyCS : ICheckSchedule
    {
        int Interval { get; set; }
        DayTypes DayType { get; set; }
    }

    public interface IHourlyCS : ICheckSchedule
    {
        int Interval { get; set; }
    }

    public interface IMonthlyCS : ICheckSchedule
    {
        Months Month { get; set; }
        int Day { get; set; }
    }
    public interface IWeeklyCS : ICheckSchedule
    {
        WeekDays WeekDay { get; set; }
        int Interval { get; set; }
    }

    #endregion

    [Serializable]
    [XmlInclude(typeof(HourlyCS)), XmlInclude(typeof(DailyCS)), XmlInclude(typeof(WeeklyCS)), XmlInclude(typeof(MonthlyCS))]
    public abstract class CheckSchedule : PropertyChangedHelper, ICheckSchedule
    {
        protected delegate bool IsGoodDay(DateTime result);

        DateTime _checkTime;
        DateTime? _nextRun;

		private static PropertyChangedEventArgs CheckTimeArgs = PropertyChangedHelper.CreateArgs<CheckSchedule>(cs => cs.CheckTime);
        [XmlAttribute]
        public DateTime CheckTime
        {
            get { return _checkTime; }
            set
            {
				if (value != _checkTime)
				{
					_checkTime = value;
					OnPropertyChanged(CheckTimeArgs);
				}
            }
        }

        [XmlIgnore]
        public DateTime? NextRun
        {
            get { return _nextRun; }
            set { _nextRun = value; }
        }

        public void UpdateNextRun()
        {
            NextRun = CalculateNextRun(NextRun);
        }

        public void NextRunStep()
        {
            NextRun = CalculateNextRunStep(NextRun);
        }

    	public abstract DateTime GetDatePriorStartingTime(DateTime nearest);
        protected abstract DateTime CalculateNextRun(DateTime? nextRun);
        protected abstract DateTime CalculateNextRunStep(DateTime? nextRun);
        protected TimeSpan GetExpirationSpan(){return TimeSpan.Zero;}
    }
   
}