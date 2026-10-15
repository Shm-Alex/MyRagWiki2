using System;
using System.Collections.Generic;
using System.Reflection;
using Framework.Utils;

namespace Framework.DynamicInterface
{
    [Flags]
    public enum InstanceHandleMode { 
        None = 0, 
        CreateBeforeCall = 1, 
        DisposeAfterCall = 2, 
        CreateIfNotExists = 4, 
        Default = CreateIfNotExists, 
        SingleCall = CreateBeforeCall | DisposeAfterCall }

    public class InstanceModeAttribute : Attribute
    {
        InstanceHandleMode _mode;

        public InstanceModeAttribute(InstanceHandleMode mode)
        {
            this._mode = mode;
        }

        public InstanceHandleMode Mode
        {
            get { return _mode; }
        }
    }

    public class DynamicEventManager
    {
        public static object CreateDynamicInterfaceImpl(Type interfaceType, DynamicMethodMap methodMap)
        {
            throw new NotImplementedException();
        }

        public static void BindToEvents(Type eventsType, object eventsTarget, DynamicMethodMap methodMap)
        {
            EventInfo[] events = eventsType.GetEvents();
            object handler = methodMap.CreateHandler();
            foreach (EventInfo eventInfo in events)
            {
                MethodInfo mi = methodMap.GetMethod(eventInfo.Name);
                if (mi != null)
                {
                    Delegate del = Delegate.CreateDelegate(eventInfo.EventHandlerType, handler, mi, true);
                    eventInfo.AddEventHandler(eventsTarget, del);
                }
            }
        }
    }

    struct RuntimeEventInfo
    {
        public EventInfo EventInfo;
        public MethodInfo MethodInfo;

        public RuntimeEventInfo(EventInfo eventInfo, MethodInfo methodInfo)
        {
            EventInfo = eventInfo;
            MethodInfo = methodInfo;
        }
    }

    class InstanceHolder
    {
        Type m_type;
        object m_instance;

        public InstanceHolder(string typeName)
        {
            m_type = Type.GetType(typeName, true, true);
        }

        public InstanceHolder(Type type)
        {
            this.m_type = type;
        }

        public Object Instance
        {
            get { return m_instance; }
        }

        public void CreateInstance()
        {
            if (m_instance != null)
                DisposeInstance();
            m_instance = Activator.CreateInstance(m_type);
        }

        public void CreateIfNotExists()
        {
            if (m_instance == null)
                CreateInstance();
        }

        public void DisposeInstance()
        {
            if (m_instance == null)
                return;
            try
            {
                if (m_instance is IDisposable)
                    ((IDisposable)m_instance).Dispose();
            }
            finally
            {
                m_instance = null;
            }
        }
    }

    public class DynamicMethodMap
    {
        private Type _hType;
        private string _hClassName;
        private Dictionary<string, string> _methodMap = new Dictionary<string, string>();

        public IDictionary<string, string> MethodMap
        {
            get { return _methodMap; }
        }

        public Type HandlerType
        {
            get 
            {
                if (_hType == null)
                {
                    _hType = Type.GetType(_hClassName, true, true);
                }
                return _hType;
            }
        }

        public MethodInfo GetMethod(string eventName)
        {
            string methodName;
            if (_methodMap.TryGetValue(eventName, out methodName) && !string.IsNullOrEmpty(methodName))
            {
                MethodInfo mi = HandlerType.GetMethod(methodName);
                if (mi == null)
                    throw new NotFoundException("Method not found: " + _hClassName + "::" + methodName);
                return mi;
            }
            else
                return null;
        }

        public string HandlerClassName
        {
            get { return _hClassName; }
            set { _hClassName = value; _hType = null; }
        }

        public object CreateHandler()
        {
            return Activator.CreateInstance(HandlerType);
        }
    }
}
