using System;
using Framework.Collections;
using System.Linq;

namespace Ksema.Framework.Caching
{
    public class DataCacheDependency
    {
        public void Invalidate()
        {
            if (Invalidated != null)
                Invalidated(this, EventArgs.Empty);
        }

        public event EventHandler Invalidated;
    }

    public class DataCacheManager
    {
        readonly WeakDictionary<string, DataCacheBase> _cacheTable;

        public DataCacheManager()
        {
            _cacheTable = new WeakDictionary<string, DataCacheBase>(StringComparer.OrdinalIgnoreCase);
        }

        public void Clear(string name)
        {
            DataCacheBase cache;
            lock (_cacheTable)
            {
                _cacheTable.RemoveCollectedEntries();
                _cacheTable.TryGetValue(name, out cache);
            }
            if (cache != null)
                cache.Clear();
        }

        public void ClearAll()
        {
            DataCacheBase[] cacheArray;
            lock (_cacheTable)
            {
                _cacheTable.RemoveCollectedEntries();
                cacheArray = _cacheTable.Values.ToArray();
            }
            for (int i = 0; i < cacheArray.Length; i++)
                cacheArray[i].Clear();
        }

        public void AddCache(DataCacheBase cache, bool overwrite)
        {
            lock (_cacheTable)
            {
                _cacheTable.RemoveCollectedEntries();
                string key = string.Intern(cache.Name);
				if (overwrite)
					_cacheTable[key] = cache;
				else
					_cacheTable.Add(key, cache);
            }
        }

        public void RemoveCache(string name)
        {
            lock (_cacheTable)
            {
                _cacheTable.RemoveCollectedEntries();
                _cacheTable.Remove(name);
            }
        }

        public static DataCacheManager Instance
        {
            get;
            private set;
        }

        static DataCacheManager()
        {
            Instance = new DataCacheManager();
        }
    }
}
