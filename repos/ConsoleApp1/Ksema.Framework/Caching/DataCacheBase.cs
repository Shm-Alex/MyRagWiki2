using System;
using Framework.Utils;
using System.Threading;

namespace Ksema.Framework.Caching
{
    public abstract class DataCacheBase
    {
        //protected readonly object SyncRoot = new object();
		protected readonly ReaderWriterLockSlim RWLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        protected DataCacheBase(string name, bool overwrite)
        {
            AssertUtils.IsNotEmpty(name, "name");
            Name = name;
            DataCacheManager.Instance.AddCache(this, overwrite);
        }

        public string Name { get; private set; }

        public abstract void Clear();
        //public abstract ICollection<KeyValuePair<object, object>> GetItems();

        public void AddDependency(DataCacheDependency dep)
        {
            dep.Invalidated += dep_Invalidated;
        }

        void dep_Invalidated(object sender, EventArgs e)
        {
            Clear();
        }
        
    }
}