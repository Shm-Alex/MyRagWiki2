using System;
using Framework.Utils;

namespace Ksema.Framework.Diagnostics
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TraceSourceAttribute : Attribute
    {
        public TraceSourceAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public static string GetTraceSourceName(Type t)
        {
            AssertUtils.IsNotNull(t, "type");
            var atts = t.GetCustomAttributes(typeof(TraceSourceAttribute), true);
            if (atts != null && atts.Length > 0)
                return ((TraceSourceAttribute)atts[0]).Name;
            return t.Assembly.GetName().Name;
        }
    }
}