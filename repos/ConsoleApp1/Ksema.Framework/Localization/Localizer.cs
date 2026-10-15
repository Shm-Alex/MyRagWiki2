using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;
using Ksema.Framework.Documentation;
using FUtils=Framework.Utils.Utils;
using AppUtils = Framework.Utils.AppUtils;
using System.Globalization;

namespace Framework.Localization
{

	public interface ILocalizer
	{
		string LocalizeString(string id);
        string LocalizeString(string id, string returnIfNotFound);
        string LocalizeString(string id, string returnIfNotFound, CultureInfo ci);

		CultureInfo DefaultCultureInfo { get; set; }

		string RegisterResource(string nsQualifiedResName);
		string RegisterResource(string resName, Assembly a);
		void UnregisterResource(string resName, Assembly a);
		void UnregisterResource(string nsQualifiedResName);

		void AddEntry(string name, string value);

		bool ContainsAddedEntry(string name);
		bool ContainsEntry(string name);
	}

	[Documentation("Framework.Localization.Doc")]
	public class Localizer : ILocalizer
	{
#if STATISTICS
		#region Statistics
		private IDictionary<string, string> _unusedValues;
		private IDictionary<string, string> _usedValues;
		private IList<string> _absentValues;
		private bool _initialized;
		private void Init(CultureInfo ci)
		{
			if (!_initialized)
				lock(this)
					if (!_initialized)
					{
						_unusedValues = new Dictionary<string, string>();
						_usedValues = new Dictionary<string, string>();
						_absentValues = new List<string>();
						foreach (var entry in m_resMap)
						{
							ResourceManager rm = entry.Value;
							foreach (DictionaryEntry item in rm.GetResourceSet(ci, true, true))
							{
								string key = (string)item.Key;
								string value = (string)item.Value;
								if (_unusedValues.ContainsKey(key))
									_unusedValues[key] = _unusedValues[key] + " => " + value;
								else
									_unusedValues.Add(key, value);
							}
						}
						_initialized = true;
					}
		}
		private void NotFound(string key)
		{
			_absentValues.Add(key);
		}
		private void Found(string key, string value)
		{
			_usedValues[key] = value;
			_unusedValues.Remove(key);
		}
		#endregion
#endif

		private List<KeyValuePair<string, ResourceManager>> m_resMap = null;
		private Dictionary<string, string> m_entries = null;

        public Localizer()
        {
			m_resMap = new List<KeyValuePair<string, ResourceManager>>();
			m_entries = new Dictionary<string, string>();
        }

		[Documentation("Framework.Localization.Culture")]
		public CultureInfo DefaultCultureInfo
		{
			get;
			set;
		}

		public void AddEntry(string name, string value)
		{
			m_entries[name] = value;
		}
		public bool ContainsAddedEntry(string name)
		{
			return m_entries.ContainsKey(name);
		}

		public bool ContainsEntry(string name)
		{
			var ci = DefaultCultureInfo ?? CultureInfo.CurrentUICulture;
			foreach (var entry in m_resMap)
			{
				ResourceManager rm = entry.Value;
				if ((rm.GetString(name, ci)) != null)
					return true;
			}
			return m_entries.ContainsKey(name);
		}

		[Documentation("Framework.Localization.LocalizeWithErrorAndCulture")]
		public string LocalizeString(string id, string returnIfNotFound, CultureInfo ci)
        {
#if STATISTICS
			Init(ci);
#endif

			string s = null;
			if (!String.IsNullOrEmpty(id))
			{
				foreach (var entry in m_resMap)
				{
					ResourceManager rm = entry.Value;
					if ((s = rm.GetString(id, ci)) != null)
						break;
				}
				if (FUtils.isEmpty(s))
					m_entries.TryGetValue(id, out s);
			}

#if STATISTICS
			if (FUtils.isEmpty(s))
			{
				NotFound(id);
			}
			else
			{
				Found(id, s);
			}
#endif

            return FUtils.isEmpty(s) && !String.IsNullOrEmpty(returnIfNotFound) ? String.Format(returnIfNotFound, id) : s;
        }

		[Documentation("Framework.Localization.LocalizeWithError")]
		public string LocalizeString(string id, string returnIfNotFound)
        {
			return LocalizeString(id, returnIfNotFound, DefaultCultureInfo ?? CultureInfo.CurrentUICulture);
        }

		[Documentation("Framework.Localization.Localize")]
		public string LocalizeString(string id)
        {
            return LocalizeString(id, "string with id '{0}' not found");
        }

        private string makeHash(string resName, Assembly a)
        {
            int nHash = (resName + "," + a.FullName).GetHashCode();
            return nHash.ToString();
        }
		string makeHash(string nsQualifiedResName)
		{
			Assembly asm;
			string resName = AppUtils.SplitResourceQualifiedName(nsQualifiedResName, out asm);
			return makeHash(resName, asm);
		}

		public string RegisterResource(string nsQualifiedResName)
		{
			Assembly asm;
			string resName = AppUtils.SplitResourceQualifiedName(nsQualifiedResName, out asm);
			return RegisterResource(resName, asm);
		}

        public string RegisterResource(string resName, Assembly a)
        {
            if( FUtils.isEmpty(resName) )
                throw new ArgumentNullException("resName", "Localizer.register");
            if( a == null )
                throw new ArgumentNullException("a", "Localizer.register");

            string hash = makeHash(resName, a);
            if( m_resMap.Select(pair => pair.Key).Contains(hash) )
                return hash;
            ResourceManager rm = new ResourceManager(resName, a);
            m_resMap.Add(new KeyValuePair<string, ResourceManager>(hash, rm));
            return hash;
        }

        private void unregisterImpl(string hash)
        {
            foreach(var item in m_resMap.FindAll(pair => pair.Key == hash))
                m_resMap.Remove(item);
        }
        public void UnregisterResource(string resName, Assembly a)
        {
            unregisterImpl(makeHash(resName, a));
        }

		public void UnregisterResource(string nsQualifiedResName)
		{
			unregisterImpl(makeHash(nsQualifiedResName));
		}
    }
}