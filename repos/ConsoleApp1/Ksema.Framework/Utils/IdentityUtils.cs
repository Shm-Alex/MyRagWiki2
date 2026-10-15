using System;
using System.Security.Principal;
using System.ServiceModel;
using Ksema.Framework.Caching;

namespace Ksema.Framework.Utils
{
	public class IdentityUtils
	{
		private static readonly DictionaryDataCache<WindowsKey, IIdentity> _identityCache;

		static IdentityUtils()
		{
			_identityCache = new DictionaryDataCache<WindowsKey, IIdentity>("WindowsIdentity", GetIdentity);
			_identityCache.AddDependency(new TimerDataCacheDependency(global::Framework.Utils.TimeoutHelper.GeneralTimeout));
		}

		private static IIdentity GetIdentity(WindowsKey key)
		{
			return key.Identity;
		}

		public static string GetShortName(string name)
		{
			int pos = name.IndexOf('\\');
			if (pos >= 0)
				return name.Substring(pos + 1);
			return name;
		}

		public static string GetFullIdentityName()
		{
			return GetCurrentPrimaryIdentityName(true);
		}

		public static string GetShortIdentityName()
		{
			return GetCurrentPrimaryIdentityName(false);
		}

		/// <summary>
		/// Точка входа - получение локального пользователя, _от_имени_которого_ выполняется операция.
		/// Если будет другая аутентификация - добавлять здесь!
		/// </summary>
		private static string GetCurrentPrimaryIdentityName(bool fullName)
		{
			IIdentity identity = OperationContext.Current == null ? WindowsIdentity.GetCurrent() : OperationContext.Current.ServiceSecurityContext.PrimaryIdentity;
			if (identity is WindowsIdentity)
			{
				var windowsIdentity = identity as WindowsIdentity;
				identity = _identityCache[new WindowsKey{ SID = windowsIdentity.User.Value, Identity = windowsIdentity }];
			}
			if (identity == null)
				return null;
			var name = identity.Name;
			return fullName ? name : GetShortName(name);
		}
	}

	public class WindowsKey : IComparable, IComparable<WindowsKey>
	{
		public string SID
		{
			get;
			set;
		}

		public WindowsIdentity Identity
		{
			get;
			set;
		}

		public override int GetHashCode()
		{
			return SID.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj is WindowsKey)
			{
				return SID.Equals((obj as WindowsKey).SID);
			}
			return false;
		}

		public int CompareTo(object obj)
		{
			return CompareTo(obj as WindowsKey);
		}

		public int CompareTo(WindowsKey other)
		{
			return SID.CompareTo(other != null ? other.SID : null);
		}
	}
}
