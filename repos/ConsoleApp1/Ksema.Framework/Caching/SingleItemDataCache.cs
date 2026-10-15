using System;

namespace Ksema.Framework.Caching
{
    public class SingleItemDataCache<T> : DataCacheBase
        //where T : class
    {
        readonly LazyInitValue<T> _item;

		public SingleItemDataCache(string name, Func<T> getData) : this(name, getData, false)
		{
		}

    	public SingleItemDataCache(string name, Func<T> getData, bool overwrite) : base(name, overwrite)
        {
            _item = new LazyInitValue<T>(getData);
        }

        public T Value
        {
            get 
            {
                return _item.Value;
            }
        }

        public override void Clear()
        {
            _item.Clear();
        }
    }
}