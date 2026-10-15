using System;
using System.Collections.Generic;

namespace Framework.Utils
{
    public class ObjectLookupCache
    {
        Dictionary<Type, object> _singleInstanceList = new Dictionary<Type, object>();

        public object GetSingleton(Type type)
        {
            if (_singleInstanceList.ContainsKey(type))
                return _singleInstanceList[type];
            else
            {
                object obj = Activator.CreateInstance(type);
                _singleInstanceList[type] = obj;
                return obj;
            }
        }
    }
}
