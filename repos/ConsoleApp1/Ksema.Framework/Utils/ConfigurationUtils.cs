using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;

namespace Ksema.Framework.Utils
{
	public class ConfigurationUtils
	{
		public static object ExeConfigurationLock = new object();

		public static bool SupportedType(XmlNodeType nodeType)
		{
			switch (nodeType)
			{
				case XmlNodeType.Comment:
				case XmlNodeType.Whitespace:
					return true;
				default:
					return false;
			}
		}

		public static void UpdateConnectionStringConfig(ConnectionStringSettings newConStr)
		{
			lock (ExeConfigurationLock)
			{
				var configConStr = ConfigurationManager.ConnectionStrings[newConStr.Name];
				var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
				if (configConStr != null)
					config.ConnectionStrings.ConnectionStrings.Remove(newConStr.Name);
				config.ConnectionStrings.ConnectionStrings.Add(newConStr);
				config.Save(ConfigurationSaveMode.Modified);
				ConfigurationManager.RefreshSection("connectionStrings");
			}
		}
		public static void UpdateSectionsByName(IEnumerable<string> sectionNames)
		{
			lock (ExeConfigurationLock)
			{
				foreach (var sectionNm in sectionNames)
				{
					ConfigurationManager.RefreshSection(sectionNm);
				}
			}
		}
		public static void UpdateConnectionStringConfigs(List<ConnectionStringSettings> ConStrList)
		{
			lock (ExeConfigurationLock)
			{
				var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
				foreach (var newConStr in ConStrList)
				{
					var configConStr = ConfigurationManager.ConnectionStrings[newConStr.Name];
					if (configConStr != null)
						config.ConnectionStrings.ConnectionStrings.Remove(newConStr.Name);
					config.ConnectionStrings.ConnectionStrings.Add(newConStr);
				}
				config.Save(ConfigurationSaveMode.Modified);
				ConfigurationManager.RefreshSection("connectionStrings");
			}
		}


		//public static void UpdateBoSettings(string ConStrList)
		//{
		//	lock (ExeConfigurationLock)
		//	{
		//		var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
				
		//		foreach (var newConStr in ConStrList)
		//		{

		//			var configConStr = ConfigurationManager.ConnectionStrings[newConStr.Name];
		//			config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
		//			if (configConStr != null)
		//				config.GetSection(;
		//			config.ConnectionStrings.ConnectionStrings.Add(newConStr);
		//		}
		//		config.Save(ConfigurationSaveMode.Modified);
		//		ConfigurationManager.RefreshSection("connectionStrings");
		//	}
		//}

		public static bool IsAppSettingOn(string name, bool defaultValue)
		{
			string s = ConfigurationManager.AppSettings[name];
			if (string.IsNullOrEmpty(s))
				return defaultValue;
			s = s.ToLower();
			switch (s)
			{
				case "yes":
				case "true":
				case "on":
					return true;
				case "no":
				case "false":
				case "off":
					return false;
				default:
					throw new NotSupportedException("AppSetting '" + name + "' value not supported: " + s);
			}
		}
	}
}
