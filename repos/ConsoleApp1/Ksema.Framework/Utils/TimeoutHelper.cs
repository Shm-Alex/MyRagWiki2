using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Framework.Utils
{
    public class TimeoutHelper
    {
    	private const string GENERAL_TIMEOUT = "GeneralTimeout";
    	private const string BULK_COPY_TIMEOUT = "BulkCopy";
		private const string PATCH_UPDATE_TIMEOUT = "PatchUpdate";

    	private const string TIMEOUT_PREFIX = "timeout_";

        public static TimeSpan GeneralTimeout
        {
            get { return GetTimeSpan(GENERAL_TIMEOUT); }
        }

		public static TimeSpan BulkTimeout
		{
			get { return GetTimeSpan(BULK_COPY_TIMEOUT); }
		}

    	public static TimeSpan PatchUpdateTimeout
    	{
			get { return GetTimeSpan(PATCH_UPDATE_TIMEOUT); }
    	}

        public static TimeSpan GetTimeSpan(string name)
        {            
            string fName = TIMEOUT_PREFIX+name;
            string val =  ConfigurationManager.AppSettings[fName];
            if (val == null)
                throw new Exception("AppSettings value " + fName + " not found in configuration file");
            return TimeSpan.Parse(val);
        }

        public static int GetSeconds(string name)
        {
            return (int)GetTimeSpan(name).TotalSeconds;
        }

        public static double GetHours(string name)
        {
            return GetTimeSpan(name).TotalHours;
        }
    }
}
