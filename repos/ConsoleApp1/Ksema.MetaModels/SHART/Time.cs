using System;

namespace Ksema.Shart
{
    public enum TimePeriodType
    {
        Undefined = 0,		// неизвестный тип периода
        Year,						// год
        HalfYear,					// полгода
        Quarter,					// квартал
        Month,						// месяц
        Week,						// неделя
        Day							// день (число)
    }

    public static class TimePeriodEx
    {
        /*
        /// <summary>
        /// ф-ция ищет нужную ноду в календаре
        /// </summary>
        static public int FindTimeNode(IDimension dim, DateTime dtSearchDate, TimePeriodType tpType)
        {
            string lUniqueKey = TimePeriodEx.CreateUniqueKey(TimePeriodEx.GetBegDate(dtSearchDate, tpType), tpType);

            //Ищем ноды по условию даты и уровню
            try
            {
                return dim.FindNode("Code", lUniqueKey).ID;
            }
            catch (ObjNotFoundException)
            {	// если ноды нет, то добавим... на этот го и на следующий...
                dim.CurrentModel.FillTimeDimension(dtSearchDate, TimePeriodType.Year, TimePeriodType.Day);
                dim.CurrentModel.FillTimeDimension(dtSearchDate.AddYears(1), TimePeriodType.Year, TimePeriodType.Day);
            }
            return dim.FindNode("Code", lUniqueKey).ID;
        }
        /// <summary>
        /// ф-ция ищет нужную ноду в календаре
        /// </summary>
        /// <param name="dim">Измерение "TIME"</param>
        /// <param name="periodTypeStr">Тип периода в виде строки (например, "MONTH")</param>
        /// <param name="timePartStr">Строка, содержащая период см. ConstructDateString()
        /// (например, "Апрель.2005")</param>
        /// <returns></returns>
        static public int FindTimeNode(
            IDimension dim,
            string periodTypeStr,
            string timePartStr
            )
        {
            TimePeriodType periodType = GetTimePeriodType(periodTypeStr);
            return FindTimeNode(dim, periodType, timePartStr);
        }
        /// <summary>
        /// ф-ция ищет нужную ноду в календаре
        /// </summary>
        /// <param name="dim">Измерение "TIME"</param>
        /// <param name="periodType">Тип периода </param>
        /// <param name="timePartStr">Строка, содержащая период см. ConstructDateString()
        /// (например, "Апрель.2005")</param>
        /// <returns></returns>
        static public int FindTimeNode(
            IDimension dim,
            TimePeriodType periodType,
            string timePartStr
            )
        {
            string dateStr = ConstructDateString(timePartStr, periodType);
            DateTime date = StringUtils.ToDateTime(dateStr);
            return FindTimeNode(dim, date, periodType);
        }
        /// <summary>
        /// функция формирует уникальный ключ на основании даты начала периода и его типа
        /// </summary>
        static public string CreateUniqueKey(DateTime aDateStartPeriod, TimePeriodType aTimePeriodType)
        {
            //				return	String.Format(	"{0:d}_{1:d2}_{2:d2}_{3:d}",
            //										aDateStartPeriod.Year,
            //										aDateStartPeriod.Month,
            //										aDateStartPeriod.Day,
            //										(int)aTimePeriodType);

            return aDateStartPeriod.Year.ToString() + "_" +
                    aDateStartPeriod.Month.ToString("d2") + "_" +
                    aDateStartPeriod.Day.ToString("d2") + "_" +
                    ((int)aTimePeriodType).ToString();
        }

        //static Regex __keyRegExpr = new Regex("(?<year>[0-9][0-9][0-9][0-9])_(?<month>[0-9][0-9])_(?<day>[0-9][0-9])_(?<timepart>[0-9])", RegexOptions.Compiled | RegexOptions.Singleline);
        public static int FindTimeNode(IDimension dim, string timeCode)
        {
            string[] parts = timeCode.Split('_');
            if (parts == null || parts.Length != 4)
                throw new ArgumentException("time code has an invalid format: " + timeCode);
            DateTime searchDate = new DateTime(Int32.Parse(parts[0]), Int32.Parse(parts[1]), Int32.Parse(parts[2]));
            TimePeriodType periodType = (TimePeriodType)Int32.Parse(parts[3]);
            return FindTimeNode(dim, searchDate, periodType);
        }
        */

        /// <summary>
        /// ф-ция возвращает начало периода для данной даты и типа периода
        /// </summary>
        static public DateTime GetBegDate(DateTime aTime, TimePeriodType aTimePeriodType)
        {
            DateTime retValue;
            switch (aTimePeriodType)
            {
                case TimePeriodType.Year:
                    retValue = new DateTime(aTime.Year, 1, 1, 0, 0, 0);
                    break;
                case TimePeriodType.HalfYear:
                    retValue = new DateTime(aTime.Year, GetBegMonthByHalfYear(GetHalfYear(aTime.Month)), 1, 0, 0, 0);
                    break;
                case TimePeriodType.Quarter:
                    retValue = new DateTime(aTime.Year, GetBegMonth(GetQuarter(aTime.Month)), 1, 0, 0, 0);
                    break;
                case TimePeriodType.Month:
                    retValue = new DateTime(aTime.Year, aTime.Month, 1, 0, 0, 0);
                    break;
                case TimePeriodType.Week:
                    {
                        int weekDay, monthDay, delta;

                        weekDay = GetDayOfWeek(aTime);
                        monthDay = aTime.Day;

                        delta = Math.Min(weekDay, monthDay) - 1;

                        retValue = new DateTime(aTime.Year, aTime.Month, aTime.Day - delta, 0, 0, 0);
                        break;
                    }
                case TimePeriodType.Day:
                    retValue = new DateTime(aTime.Year, aTime.Month, aTime.Day, 0, 0, 0);
                    break;
                default:
                    throw new ArgumentException("TimePeriodEx::GetBegDate. TimePeriodType is invalid");
            }
            return retValue;
        }
        /// <summary>
        /// ф-ция возвращает окончание периода для данной даты и типа периода
        /// </summary>
        static public DateTime GetEndDate(DateTime aTime, TimePeriodType aTimePeriodType)
        {
            DateTime retValue = GetBegDate(aTime, aTimePeriodType);
            retValue = AddPeriod(retValue, aTimePeriodType);
            retValue -= TimeSpan.FromSeconds(1);

            return retValue;
        }
        /// <summary>
        /// ф-ция добавляет к дате время периода
        /// </summary>
        static public DateTime AddPeriod(DateTime aTime, TimePeriodType aTimePeriodType)
        {
            return AddPeriods(aTime, aTimePeriodType, 1);
        }
        /// <summary>
        /// ф-ция добавляет к дате время периода
        /// </summary>
        static public DateTime AddPeriods(DateTime aTime, TimePeriodType aTimePeriodType, int numPeriods)
        {
            DateTime retValue = aTime;
            switch (aTimePeriodType)
            {
                case TimePeriodType.Year:
                    retValue = retValue.AddYears(1 * numPeriods);
                    break;
                case TimePeriodType.HalfYear:
                    retValue = retValue.AddMonths(6 * numPeriods);
                    break;
                case TimePeriodType.Quarter:
                    retValue = retValue.AddMonths(3 * numPeriods);
                    break;
                case TimePeriodType.Month:
                    retValue = retValue.AddMonths(1 * numPeriods);
                    break;
                case TimePeriodType.Week:
                    {
                        if (numPeriods != 1)
                            throw new InvalidOperationException("TimePeriodEx::AddPeriods. Adding a few weeks haven't been realised!");

                        int weekDay, maxMonthDay, day;

                        weekDay = GetDayOfWeek(aTime);
                        maxMonthDay = DateTime.DaysInMonth(aTime.Year, aTime.Month);

                        day = aTime.Day + (7 - weekDay);
                        day = Math.Min(day, maxMonthDay);

                        // установим на последний день периода
                        retValue = new DateTime(aTime.Year, aTime.Month, day, 0, 0, 0);
                        // добавим день
                        retValue = retValue.AddDays(1);
                        break;
                    }
                case TimePeriodType.Day:
                    retValue = retValue.AddDays(1 * numPeriods);
                    break;
                default:
                    throw new ArgumentException("TimePeriodEx::AddPeriod. TimePeriodType is invalid");
            }
            return retValue;
        }
        /// <summary>
        /// ф-ция проверяет, может ли данная дата быть началом периода
        /// с точностью до секунды.
        /// </summary>
        static public bool TestBegDate(DateTime aTime, TimePeriodType aTimePeriodType)
        {
            return (GetBegDate(aTime, aTimePeriodType) == aTime);
        }
        /// <summary>
        /// ф-ция возвращает номер половины года:
        /// </summary>
        static public int GetHalfYear(int month)
        {
            return (month + 5) / 6;
        }
        /// <summary>
        /// ф-ция возвращает номер первого месяца в квартале.
        /// </summary>
        static public int GetBegMonthByHalfYear(int iHalfYear)
        {
            int lResult = (iHalfYear - 1) * 6 + 1;
            return lResult;
        }
        /// <summary>
        /// ф-ция возвращает номер квартала по номеру месяца.
        /// </summary>
        static public int GetQuarter(int iMonth)
        {
            int lResult = (iMonth - 1) / 3 + 1;
            return lResult;
        }
        /// <summary>
        /// ф-ция возвращает номер первого месяца в квартале.
        /// </summary>
        static public int GetBegMonth(int iQuarter)
        {
            int lResult = (iQuarter - 1) * 3 + 1;
            return lResult;
        }
        /*
        /// <summary>
        /// Возвращает имя периода ()
        /// </summary>
        static public string GetPeriodName(IDimension timeDim,
                                            int nodeId)
        {
            string result = "";

            // Получить узел
            INode node = timeDim.FindNode(nodeId);

            object o = node[CubeConstants.TimeFields.LevelID];
            if (o == null)
            {
                return "";
            }

            int timePriod = (int)o;
            if (timePriod == (int)TimePeriodType.Year)
            {
                result = StringUtils.ToString(
                    node[CubeConstants.TimeFields.Year])
                    + " год";
            }
            else if (timePriod == (int)TimePeriodType.HalfYear)
            {
                result = StringUtils.ToString(
                    node[CubeConstants.TimeFields.HalfYear])
                    + " полугодие "
                    + StringUtils.ToString(
                    node[CubeConstants.TimeFields.Year])
                    + "г.";
            }
            else if (timePriod == (int)TimePeriodType.Quarter)
            {
                result = StringUtils.ToString(
                    node[CubeConstants.TimeFields.Quarter])
                    + " квартал "
                    + StringUtils.ToString(
                    node[CubeConstants.TimeFields.Year])
                    + "г.";
            }
            else if (timePriod == (int)TimePeriodType.Month)
            {
                result = (node[CubeConstants.TimeFields.MonthName_rus] as string)
                    + " "
                    + StringUtils.ToString(
                    node[CubeConstants.TimeFields.Year])
                    + "г.";
            }

            return result;
        }
         */
 
        /// <summary>
        /// Функция для получения имени периода
        /// </summary>
        /// <param name="dtDate">Дата</param>
        /// <param name="tpType">Тип периода</param>
        /// <returns></returns>
        public static string ToString(DateTime dtDate, TimePeriodType tpType)
        {
            string lResult = String.Empty;

            switch (tpType)
            {
                case TimePeriodType.Year:
                    lResult = dtDate.Year.ToString();
                    break;
                case TimePeriodType.HalfYear:
                    lResult = TimePeriodEx.GetHalfYear(dtDate.Month).ToString();
                    break;
                case TimePeriodType.Quarter:
                    lResult = TimePeriodEx.GetQuarter(dtDate.Month).ToString();
                    break;
                case TimePeriodType.Month:
                    lResult = dtDate.Month.ToString();
                    break;
                case TimePeriodType.Day:
                    lResult = dtDate.Day.ToString();
                    break;
                case TimePeriodType.Week:
                    lResult = GetWeekNumber(dtDate).ToString();
                    break;
            }
            return lResult;
        }
        /// <summary>
        /// Возвращает день недели, начиная с понедельника == 1
        /// по хорошему, это было бы лучше вынести все это в БД
        /// </summary>
        /// <returns></returns>
        public static int GetDayOfWeek(DateTime dtDate)
        {
            int lDayOfWeek = (int)dtDate.DayOfWeek;
            if (lDayOfWeek == 0)
                lDayOfWeek = 7;

            return lDayOfWeek;
        }
        /// <summary>
        /// ф-ция возвращает номер недели в годе:
        /// Если текущий период крупнее требуемого, то возвращаем 0
        /// </summary>
        public static int GetWeekNumber(DateTime dtDate)
        {
            int lMonthNumber = dtDate.Month;		// номер текущего месяца
            int i, weekCounter = 0,
                    curWeekDay, curDay;

            // пробежимся по всем месяцам и посчитаем, сколько там было дней
            for (i = 1; i <= lMonthNumber; i++)
            {
                curDay = 1;
                DateTime lDate = new DateTime(dtDate.Year, i, curDay);
                DateTime lEndDate = lDate.AddMonths(1);

                curWeekDay = GetDayOfWeek(lDate);
                // перейдем на следующий понедельник...
                lDate = lDate.AddDays(8 - curWeekDay);
                weekCounter++;
                while (lDate < lEndDate &&
                        lDate <= dtDate)
                {
                    lDate = lDate.AddDays(7);
                    weekCounter++;
                }
            }
            // определим номер дня недели для 1-го января
            return weekCounter;
        }
        /// <summary>
        /// Процедура формирует дату начала периода по строке, содержащей этот период.
        /// Например для года - это число (год).
        /// Для месяца - 'месяц.год'
        /// Для квартала -	'квартал.год' квартал может задаваться цифрами 1, 2, 3, 4
        ///					и римскими I,II, III, IV. допустимы слова: "кв", "квартал", "quarter"
        /// </summary>
        public static string ConstructDateString(string timePart, TimePeriodType periodType)
        {
            string retValue;
            switch (periodType)
            {
                case TimePeriodType.Day:
                    retValue = timePart;
                    break;
                case TimePeriodType.Year:
                    retValue = "01.01." + timePart;
                    break;
                case TimePeriodType.Month:
                    retValue = "01." + timePart;
                    break;
                case TimePeriodType.Quarter:
                    {
                        DateTime date = StringUtils.ToDateTimeFromQuarter(timePart);
                        retValue = StringUtils.ToString(date);
                        break;
                    }
                case TimePeriodType.HalfYear:
                    {
                        DateTime date = StringUtils.ToDateTimeFromHalfYear(timePart);
                        retValue = StringUtils.ToString(date);
                        break;
                    }
                default:
                    throw new InvalidOperationException("Тип даты: <" + periodType.ToString() +
                        "> не поддерживается.");
            }
            return retValue;
        }
        /// <summary>
        /// ф-ция возвращает тип периода, заданного в строке
        /// М.б. year, halfyear, month, quarter, day,
        /// MM.yyyy, q.yyyy, DD.MM.yyyy, b.yyyy
        /// 
        /// </summary>
        public static TimePeriodType GetTimePeriodType(string format)
        {
            format = format.ToUpper();

            if (format == TimePeriodType.Month.ToString().ToUpper())
            {
                return TimePeriodType.Month;
            }
            else if (format == TimePeriodType.Quarter.ToString().ToUpper())
            {
                return TimePeriodType.Quarter;
            }
            else if (format == TimePeriodType.Day.ToString().ToUpper())
            {
                return TimePeriodType.Day;
            }
            else if (format == TimePeriodType.HalfYear.ToString().ToUpper())
            {
                return TimePeriodType.HalfYear;
            }
            else if (format == TimePeriodType.Year.ToString().ToUpper())
            {
                return TimePeriodType.Year;
            }
            else if (format.IndexOf("DD") > -1)
            {
                return TimePeriodType.Day;
            }
            else if (format.IndexOf("MM") > -1)
            {
                return TimePeriodType.Month;
            }
            else if (format.IndexOf("Q") > -1)
            {
                return TimePeriodType.Quarter;
            }
            else if (format.IndexOf("B") > -1)
            {
                return TimePeriodType.HalfYear;
            }
            else if (format.IndexOf("YY") > -1)
            {
                return TimePeriodType.Year;
            }
            throw new InvalidOperationException("Тип даты: <" + format +
                                            "> для документа не поддерживается.");
        }
 
    }
}
