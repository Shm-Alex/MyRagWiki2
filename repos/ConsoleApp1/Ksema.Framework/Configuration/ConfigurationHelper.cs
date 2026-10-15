using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ksema.Framework;

namespace System
{
    public static class ConfigurationHelper
    {
        public static T Resolve<T>(this object o)
        {
            if (!ObjectContainer.IsContainerAvailable)
                return default(T);
            return ObjectContainer.Instance.Resolve<T>();
        }

        public static T Resolve<T>(this object o, string name)
        {
            if (!ObjectContainer.IsContainerAvailable)
                return default(T);
            return ObjectContainer.Instance.Resolve<T>(name);
        }
    }
}
