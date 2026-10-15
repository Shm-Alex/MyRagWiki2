using System;
using System.Collections;

namespace Framework.Utils
{
	public interface ISupportDataCache
	{
		DataCache Cache{get;}
	}
	/// <summary>
	/// Кэширование данных по ключу. 
	/// Класс потокобезопасный на чтение / запись
	/// </summary>
	public class DataCache
	{
		public Hashtable _htbl = Hashtable.Synchronized(new Hashtable());
		volatile bool _useCache = false;
		public bool UseCache
		{
			get
			{
				return _useCache;
			}
			set
			{
				if (_useCache != value)
					lock(this)
					{
						if (_useCache != value)
						{
							_useCache = value;
							if(!_useCache)
								_htbl.Clear();
						}
					}
			}
		}
		public object lookupData(object key)
		{
			if(_useCache)
				return cloneData(_htbl[key]);
			return null;
		}
		public void storeData(object key, object data)
		{
			if(_useCache)
				_htbl[key] = cloneData(data);
		}
		public void reset()
		{
			_htbl.Clear();
		}
		object cloneData(object data)
		{
			ICollection lst = data as ICollection;
			if(lst != null)
			{
				ICloneable cl_data = data as ICloneable;
				if(cl_data != null)
					return cl_data.Clone();
			}
			return data;
		}
	}
}