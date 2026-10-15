using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Security;
using System.Threading;
using Framework.Macro;

namespace Ksema.Framework.Diagnostics
{
    public class EventLogTraceListenerEx : TraceListener 
    {
        const string LOG_NAME_ATT = "logName";
        EventLogTraceListener _listener;
	    private bool initListener;
	    private string ListenerSource;
	    EventLogTraceListener listener
	    {
			get
			{
				if (!initListener)
				{
					try
					{
						if (string.IsNullOrEmpty(ListenerSource))
							_listener = new EventLogTraceListener();
						else
						{
							if (!EventLog.SourceExists(ListenerSource))
								EventLog.CreateEventSource(ListenerSource, GetLogName());
							_listener = new EventLogTraceListener(ListenerSource);
						}
						initListener = true;
					}
					catch (SecurityException exception)
					{
						Tracer.Error(exception);
					}
					finally
					{
						initListener = true;
					}
				}
				return _listener;
			}
		}

        public EventLogTraceListenerEx()
        {
            //listener = new EventLogTraceListener();
        }

        public EventLogTraceListenerEx(string source)
        {
            CreateInternalListener(source);
        }

        public EventLogTraceListenerEx(string source, string name) 
            : base(name)
        {
            CreateInternalListener(source);
        }

        protected override string[] GetSupportedAttributes()
        {
            return new[] { LOG_NAME_ATT };
        }

        private void CreateInternalListener(string source)
        {
			if (source.StartsWith(MacroManager.DefaultMacroPrefix))
			{
				var manager = new MacroManager();
				manager.RegisterMacro("Lib", new StandardMacro());
				source = manager.ParseString(source).ToString();
			}
			ListenerSource = source;
        }

        private string GetLogName()
        {
            string logName = Attributes[LOG_NAME_ATT];
            return string.IsNullOrEmpty(logName) ? "Application" : logName;
        }

        public override void Close()
        {
			if (listener!=null)
				listener.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Close();
        }


        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {
			if ((Filter == null) || Filter.ShouldTrace(eventCache, source, eventType, id, null, null, data, null) && listener!=null )
            {
                listener.TraceData(eventCache, source, eventType, id, data);
            }
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
        {
			if ((Filter == null) || Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, data) && listener != null)
            {
                listener.TraceData(eventCache, source, eventType, id, data);
            }
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
			if ((Filter == null) || Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, null) && listener != null)
            {
                listener.TraceEvent(eventCache, source, eventType, id);
            }
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
			if ((Filter == null) || Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null) && listener != null)
            {
                listener.TraceEvent(eventCache, source, eventType, id, format, args);
            }
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
			if ((Filter == null) || Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null) && listener != null)
            {
                listener.TraceEvent(eventCache, source, eventType, id, message);
            }
        }

        public override void Write(string message)
        {
			if (listener!=null )
				listener.Write(message);
        }

        public override void WriteLine(string message)
        {
			if (listener!=null )
				listener.WriteLine(message);
        }
    }
}
