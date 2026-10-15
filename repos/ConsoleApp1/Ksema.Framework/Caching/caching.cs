using System;
using System.Collections;

namespace Framework.Caching
{
	public interface IObjectContainer
	{
		object this[string key]{get;set;}
		void remove(string key);
	}

	public interface ICache : IObjectContainer
	{
		void insert(string key, object item, ICacheDependency dep);
		void reset();
		bool IsValid{get;}
	}

	public interface ICacheDependency
	{
	}

	public sealed class CacheMgr
	{
		private static ICache _sessionCache, _appCache;
		private CacheMgr(){}
		public static ICache SessionCache
		{
			get
			{
				if(_sessionCache == null)
					_sessionCache = new DictionaryCache();
				return _sessionCache;
			}
			set{_sessionCache = value;}
		}
		public static ICache AppCache
		{
			get
			{
				if(_appCache == null)
					_appCache = new DictionaryCache();
				return _appCache;
			}
			set{_appCache = value;}
		}
	}

	public sealed class FastKey
	{
		static string _baseGuid = Guid.NewGuid().ToString();
		static Int64 _incr = 0;
		public static string create()
		{
			Int64 incr = System.Threading.Interlocked.Increment(ref _incr);
			return _baseGuid + ":" + incr.ToString();
		}
	}

	public class DictionaryCache : ICache
	{
		IDictionary _dict;
		public DictionaryCache(IDictionary dict)
		{
			if(dict == null)
				throw new ArgumentNullException("dictionary");
			_dict = dict;
		}
		public DictionaryCache()
		{
			_dict = Hashtable.Synchronized(new Hashtable());
		}
		#region ICache Members

		public void insert(string key, object item, ICacheDependency dep)
		{
			_dict[key] = item;
		}

		public void reset()
		{
			ICollection dictKeys = _dict.Keys;
			object[] keys = new object[dictKeys.Count];
			dictKeys.CopyTo(keys, 0);
			foreach(object key in keys)
				_dict.Remove(key);
		}

		public bool IsValid
		{
			get{return true;}
		}

		#endregion

		#region IObjectContainer Members

		public object this[string key]
		{
			get
			{
				return _dict[key];
			}
			set
			{
				_dict[key] = value;
			}
		}

		public void remove(string key)
		{
			_dict.Remove(key);
		}

		#endregion

	}

	public class TimeSpanExpiration : ICacheDependency
	{
		TimeSpan _expTime;

		public TimeSpan ExpirationTime
		{
			get { return _expTime; }
		}

		public TimeSpanExpiration(TimeSpan expTime)
		{
			_expTime = expTime;
		}

		public TimeSpanExpiration(int expMin)
		{
			_expTime = TimeSpan.FromMinutes(expMin);
		}

		static ICacheDependency __neverExp = new TimeSpanExpiration(TimeSpan.Zero);
		public static ICacheDependency NeverExpires
		{
			get { return __neverExp; } 
		}
	}

}