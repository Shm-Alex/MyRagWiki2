using System;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.Threading;
using System.Xml;

namespace Framework.AppService
{
	using Framework.Utils;

	public interface IServiceInfo
	{
		string Name{get;}
		IServiceSchedule Schedule{get;}
		ThreadStart Worker{get;}
		IExceptionHandler ErrorHandler{get;}
		bool BreakOnError{get;}
	}

	public class ServiceInfo : IServiceInfo
	{
		string _Name;
		IServiceSchedule _Schedule;
		ThreadStart _Worker;
		IExceptionHandler _ErrorHandler;
		bool _BreakOnError;

		public ServiceInfo(string name, IServiceSchedule schedule, ThreadStart worker, IExceptionHandler errorHandler, bool breakOnError)
		{
			if(name == null || name.Trim() == "")
				throw new ArgumentNullException("name");
			if(schedule == null)
				throw new ArgumentNullException("schedule");
			if(worker == null)
				throw new ArgumentNullException("worker");

			_Name = name;
			_Schedule = schedule;
			_Worker = worker;
			_ErrorHandler = errorHandler;
			_BreakOnError = breakOnError;
		}

		public ServiceInfo(string name, IServiceSchedule schedule, ThreadStart worker)
			:this(name, schedule, worker, null, false)
		{
		}

		public string Name
		{
			get { return _Name; }
		}

		public IServiceSchedule Schedule
		{
			get { return _Schedule; }
		}

		public ThreadStart Worker
		{
			get { return _Worker; }
		}

		public IExceptionHandler ErrorHandler
		{
			get { return _ErrorHandler; }
		}

		public bool BreakOnError
		{
			get { return _BreakOnError; }
		}

	}

	class DelegateWorkerClass : WorkerClassBase
	{
		ThreadStart _worker;
		public DelegateWorkerClass(IServiceInfo info) : base(info.Schedule, info.ErrorHandler, info.BreakOnError)
		{
			_worker = info.Worker;
		}

		protected override void runAction()
		{
			_worker();
		}

	}

	public class MultiServiceHost : IServiceHost
	{
		Hashtable _services = Hashtable.Synchronized(new Hashtable());
		bool _started = false;
		bool _suspended = false;

		public void addService(IServiceInfo info, ThreadPriority priority)
		{
			checkStarted(false);
			checkRegistered(info.Name, false);
			DelegateWorkerClass wc = new DelegateWorkerClass(info);
			ServiceHost host = new ServiceHost(wc, priority);
			_services.Add(info.Name, host);
		}

		void checkStarted(bool started)
		{
			if(_started != started)
			{
				string s = started ? "not" : "already";
				throw new InvalidOperationException("services are " + s + " started");
			}
		}

		void checkRegistered(string name, bool registered)
		{
			if(IsRegistered(name) != registered)
			{
				string s = registered ? "not" : "already";
				throw new InvalidOperationException("service is " + s + " registered: " + name);
			}
		}

		public bool IsRegistered(string name)
		{
			return _services.Contains(name);
		}

		#region IServiceHost Members

		public void start()
		{
			checkStarted(false);
			try
			{
				foreach(ServiceHost host in _services)
					host.start();
				_started = true;
			}
			catch
			{
				foreach(ServiceHost host in _services)
				{
					if(host.IsStarted)
					{
						try
						{ 
							host.stop(); 
						}
						catch
						{
						}
					}
				}
				throw;
			}
		}

		public void stop()
		{
			checkStarted(true);
			Exception error = null;
			foreach(ServiceHost host in _services)
			{
				try
				{
					if(host.IsStarted)
						host.stop();
				}
				catch(Exception e)
				{
					error = e;
				}
				
			}

			if(error != null)
				throw error;
			else
				_started = false;

		}

		public void pause()
		{
			foreach(ServiceHost host in _services)
				host.pause();
			_suspended = true;
		}

		public void resume()
		{
			foreach(ServiceHost host in _services)
				host.resume();
			_suspended = false;
		}

		public void join()
		{
			checkStarted(true);
			foreach(ServiceHost host in _services)
			{
				host.join();
			}
			_started = false;
		}

		public bool IsStarted
		{
			get { return _started; }
		}

		public bool IsSuspended
		{
			get { return _suspended; }
		}

		#endregion
	}

	public class GlobalObjects : IConfigurationSectionHandler
	{
		static IDictionary __objects = null;

		public static object getObject(string name)
		{
			if(name == null || name.Trim().Length == 0)
				throw new ArgumentNullException("name");
			if(__objects == null)
				init();
			return __objects[name];
		}

		static void init()
		{
			__objects = (IDictionary) ConfigurationSettings.GetConfig("GlobalObjects");
		}

		#region IConfigurationSectionHandler Members

		public object Create(object parent, object configContext, XmlNode section)
		{
			IDictionary objects = new Hashtable();

			foreach(XmlElement node in section.ChildNodes)
			{
				string className = node.GetAttribute("class");
				string key = node.GetAttribute("key");
				Type t = Type.GetType(className, true, true);
				object obj = Activator.CreateInstance(t);
				objects.Add(key, obj);
			}

			return objects;
		}

		#endregion
	}

	public class MultiServiceFactory : IConfigurationSectionHandler
	{
		static IExceptionHandler __errorHandler = null;

		public static MultiServiceHost createFromConfig(string sectionName, IExceptionHandler errorHandler)
		{
			lock(typeof(MultiServiceFactory))
			{
				__errorHandler = errorHandler;
				MultiServiceHost host = (MultiServiceHost) ConfigurationSettings.GetConfig(sectionName);
				__errorHandler = null;

				return host;
			}
		}

		#region IConfigurationSectionHandler Members

		public object Create(object parent, object configContext, System.Xml.XmlNode section)
		{
			ThreadStart worker;
			IServiceSchedule schedule = null;
			MultiServiceHost multiService = new MultiServiceHost();
			XmlElement cfgNode = (XmlElement) section;

			string g_className = cfgNode.GetAttribute("class");
			string g_objectName = cfgNode.GetAttribute("object");
			string g_strBreak = cfgNode.GetAttribute("breakOnError");

			bool g_breakOnError = false;
			if(!Utils.isEmpty(g_strBreak))
				g_breakOnError = Boolean.Parse(g_strBreak);

			foreach(XmlElement node in section.ChildNodes)
			{
				string name = node.GetAttribute("name");
				string className = node.GetAttribute("class");
				string objectName = node.GetAttribute("object");
				string methodName = node.GetAttribute("method");
				string strBreak = node.GetAttribute("breakOnError");

				if(Utils.isEmpty(name))
					throw new InvalidOperationException("service name must be specified");

				if(Utils.isEmpty(methodName))
					throw new InvalidOperationException("method name must be specified for service: " + name);

				bool breakOnError = g_breakOnError;
				if(!Utils.isEmpty(strBreak))
					breakOnError = Boolean.Parse(strBreak);

				if(Utils.isEmpty(objectName) && Utils.isEmpty(className))
				{
					objectName = g_objectName;
					className = g_className;
				}

				if(!Utils.isEmpty(objectName))
				{
					object obj = GlobalObjects.getObject(objectName);
					worker = (ThreadStart) Delegate.CreateDelegate(typeof(ThreadStart), obj, methodName);
				}
				else if(!Utils.isEmpty(className))
				{
					Type t = Type.GetType(className, true, true);
					worker = (ThreadStart) Delegate.CreateDelegate(typeof(ThreadStart), t, methodName);
				}
				else
					throw new InvalidOperationException("neither class or object name were specified for service: " + name);

				foreach(XmlElement schNode in node.GetElementsByTagName("schedule"))
				{
					schedule = createSchedule(schNode);
					break;
				}
				if(schedule == null)
					throw new InvalidOperationException("schedule not specified for the service: " + name);

				IServiceInfo info = new ServiceInfo(name, schedule, worker, __errorHandler, breakOnError);
				multiService.addService(info, ThreadPriority.Normal);
			}

			return multiService;
		}

		IServiceSchedule createSchedule(XmlElement node)
		{
			string className = node.GetAttribute("class");
			Type t = Type.GetType(className, true, true);
			IObjectFactory factory = (IObjectFactory) Activator.CreateInstance(t);
			IDictionary data = new ListDictionary();
			foreach(XmlElement initNode in node.ChildNodes)
				data.Add(initNode.Name, initNode.InnerText);

			IServiceSchedule schedule = (IServiceSchedule) factory.createObject(data);
			return schedule;
		}

		#endregion
	}

}