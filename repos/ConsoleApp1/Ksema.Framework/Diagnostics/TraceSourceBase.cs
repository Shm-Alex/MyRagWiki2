using System.Diagnostics;
using System;
using Framework.Utils;
using Ksema.Framework.Utils;

namespace Ksema.Framework.Diagnostics
{
    public interface ITraceSource
    {
        string Name { get; }
        void Trace(TraceEventType type, TraceData data);
		TraceListenerCollection Listeners { get; }
    }

    public interface ITraceMessageExtender
    {
        void AddProperties(TraceData data);
    }

    class TraceSourceBase : TraceSource, ITraceSource
    {
        const SourceLevels DEFAULT_LEVEL = SourceLevels.Warning;
        const string INHERIT_LISTENERS_ATT = "inheritListeners";
        const string MESSAGE_EXTENDER_ATT = "extenderType";
        const string INTERNAL_SOURCE = "Ksema.Framework.Diagnostics";

        static readonly EventLogTraceListenerEx s_eventLog;

        static TraceSourceBase()
        {
            System.Diagnostics.Trace.AutoFlush = true;
            s_eventLog = new EventLogTraceListenerEx(INTERNAL_SOURCE);
        }

		private string lastMessage = null;

        public TraceSourceBase(string name, TraceSourceBase parent) 
            : base(name, parent == null ? DEFAULT_LEVEL : parent.Switch.Level)
        {            
            AddInheritedListeners(parent);
            SetExtender(parent);
        }

        public void Trace(TraceEventType type, TraceData data)
        {
            try
            {
				bool extend = true;
				string newMessage = type.ToString() + (data != null ? data.ToString() : String.Empty);
				if (newMessage == lastMessage)
				{
					data = new TraceData("Repeated (" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + ")");
					extend = false;
				}
				else
					lastMessage = newMessage;
                if (data != null)
                {
                    if (extend && Extender != null)
                        Extender.AddProperties(data);

	                if (data.Items.Count == 0)
	                {
		                TraceEvent(type, data.ID, data.Message);
	                }
	                else
                        TraceData(type, data.ID, data);
                }
                else
                    TraceEvent(type, 0);
            }
            catch (Exception ex)
            {
                lock (s_eventLog)
                {
                    try
                    {
                        string message = "=== Error writing trace message === " + Environment.NewLine + ex + Environment.NewLine;
                        message += "=== Original trace data === " + Environment.NewLine + (data == null ? "" : data.ToString());

                        s_eventLog.TraceEvent(new TraceEventCache(), INTERNAL_SOURCE, TraceEventType.Error, 0, message);
                    }
                    catch(Exception inner)
                    {
                        if ((int)type <= (int)TraceEventType.Warning)
                            throw new CompositeException(new[] {new TraceException(data), ex, inner });
                    }
                }
            }
        }

        protected override string[] GetSupportedAttributes()
        {
            return new[] { INHERIT_LISTENERS_ATT, MESSAGE_EXTENDER_ATT };
        }

        protected ITraceMessageExtender Extender
        {
            get;
            set;
        }

        private Type GetExtenderType()
        {
            if (Attributes.ContainsKey(MESSAGE_EXTENDER_ATT))
            {
                string typeName = Attributes[MESSAGE_EXTENDER_ATT];
                Type t = Type.GetType(typeName, true, true);
                return t;
            }
            return null;
        }

        private bool InheritListeners
        {
            get 
            {
                bool inherit = true;
                if (Attributes.ContainsKey(INHERIT_LISTENERS_ATT))
                {
                    string strVal = Attributes[INHERIT_LISTENERS_ATT];
                    if (!string.IsNullOrEmpty(strVal))
                    {
                        switch (strVal.ToLower())
                        {
                            case "true":
                            case "yes":
                                inherit = true;
                                break;
                            case "false":
                            case "no":
                                inherit = false;
                                break;
                            default:
                                throw new FormatException("invalid value for the '" + INHERIT_LISTENERS_ATT + "' attribute: " + strVal);
                        }
                    }
                }

                return inherit;
            }
        }

        private void AddInheritedListeners(TraceSource parent)
        {
            if (InheritListeners && parent != null)
                foreach (TraceListener listener in parent.Listeners)
                    if (GetByName(Listeners, listener.Name) == null)
                        Listeners.Add(listener);
        }

        private static TraceListener GetByName(TraceListenerCollection listeners, string name)
        {
            foreach (TraceListener lst in listeners)
                if (lst.Name == name)
                    return lst;
            return null;
        }

        private void SetExtender(TraceSourceBase parent)
        {
            Type t = GetExtenderType();
            if (t != null)
                Extender = (ITraceMessageExtender)Activator.CreateInstance(t);
            else if (parent != null)
                Extender = parent.Extender;
        }

    }

	public class TraceExtender : ITraceMessageExtender
	{
		public void AddProperties(TraceData data)
		{
			var userName = IdentityUtils.GetFullIdentityName();
			var version = this.GetType().Assembly.GetName().Version.ToString(4);
			data.AddProperty("Information", string.Format("***** Date = {0}, Version = {1}, User = {2} *****", DateTime.Now, version, userName));
		}
	}

	public class TraceException : Exception
    {
        public TraceData TraceData { get; set; }

        public TraceException(TraceData data)
        {
            TraceData = data;
        }
    }
}