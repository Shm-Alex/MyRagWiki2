using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Ksema.Framework.Diagnostics
{
    public class LogFileListener : ModifiedTextWriterTraceListener
    {
        const int DEFAULT_TOLERANT_SIZE = 1048576;
        const string TOLERANT_SIZE_ATT = "tolerantSize";
        const string TRUNCATE_ATT = "truncate";

    	private const string MULTIACCESS_ATT = "multiAccess";

        bool _initDone = false;

        public LogFileListener()
        {
        }

        public LogFileListener(string fileName) : base(fileName)
        {
            InitLogFile();
        }

        public LogFileListener(string fileName, string name) : base(fileName, name)
        {
            InitLogFile();
        }

        protected override string[] GetSupportedAttributes()
        {
            return new[] { TOLERANT_SIZE_ATT, TRUNCATE_ATT , MULTIACCESS_ATT};
        }

		private bool IsMultiAccess()
		{
			if (Attributes.ContainsKey(MULTIACCESS_ATT))
				return bool.Parse(Attributes[MULTIACCESS_ATT]);
			return false;
		}

        private int GetTolerantSize()
        {
            if (Attributes.ContainsKey(TOLERANT_SIZE_ATT))
                return Int32.Parse(Attributes[TOLERANT_SIZE_ATT]);
            return DEFAULT_TOLERANT_SIZE;
        }

        private bool Truncate
        {
            get
            {
                if (Attributes.ContainsKey(TRUNCATE_ATT))
                    return Boolean.Parse(Attributes[TRUNCATE_ATT]);
                return true;
            }
        }

        private void InitLogFile()
        {
            if (_initDone)
                return;

			string folder = Path.GetDirectoryName(fileName);
			if (String.IsNullOrEmpty(folder))
			{
				try
				{
					//folder = AppDomain.CurrentDomain.BaseDirectory;
					folder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
				}
				catch
				{
					folder = Environment.CurrentDirectory;
				}
				fileName = Path.Combine(folder, fileName);
			}

			fileName = LogFileTasks.GetLastUnusedFile(fileName, 0, Path.GetFileNameWithoutExtension(fileName));

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            if (Truncate)
                LogFileTasks.PrepareFileForLog(fileName, GetTolerantSize());

            using (StreamWriter writer = new StreamWriter(fileName, true))
            {
				writer.WriteLine(SystemInformation());
            }

            _initDone = true;
        }

		public virtual string SystemInformation()
		{
			string ipAdress = string.Join(",", System.Net.Dns.GetHostByName(System.Net.Dns.GetHostName()).AddressList.Select(ip => ip.ToString()));
			return String.Format("========= {0}, Version = {6}, Domain = {2}, User = {1}, Host = {3}, Executable = {4}, WorkingFolder = {5} , ip = {7} =========",
				DateTime.Now, Environment.UserName, Environment.UserDomainName, Environment.MachineName, (Environment.CommandLine ?? String.Empty).Trim(), (Environment.CurrentDirectory ?? String.Empty).Trim(), ApplicationVersion, ipAdress);
		}

    	public virtual string ApplicationVersion
    	{
    		get
    		{
				return this.GetType().Assembly.GetName().Version.ToString(4);
    		}
    	}

        public override void Write(string message)
        {
            InitLogFile();
            base.Write(message);
        }

        public override void WriteLine(string message)
        {
            InitLogFile();
            base.WriteLine(message);
        }
    }
}

