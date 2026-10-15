using System;
using System.Collections;
using System.Collections.Generic;
using Framework.Utils;
using System.Diagnostics;
using Ksema.Framework.Diagnostics;

namespace Framework.Extensions
{
    public class RuntimeModuleStorage : IRuntimeModuleContext
    {
        Dictionary<string, RuntimeModuleHolder> _moduleHolder;
        Dictionary<string, RuntimeModuleHolder> _moduleIntfCache;
        private ISessionProvider _sessionProvider;
        Hashtable _contextData;

        public RuntimeModuleStorage(ModuleInfoCatalog catalog, ISessionProvider prv)
        {
            if (prv == null)
                throw new ArgumentNullException("Session provider");

            _sessionProvider = prv;
            _contextData = new Hashtable();
            _moduleIntfCache = new Dictionary<string, RuntimeModuleHolder>(StringComparer.OrdinalIgnoreCase);
            if (catalog == null || catalog.ModuleInfoList == null)
                _moduleHolder = new Dictionary<string, RuntimeModuleHolder>();
            else
            {
                _moduleHolder = new Dictionary<string, RuntimeModuleHolder>(catalog.ModuleInfoList.Length);
                foreach (ModuleInfo info in catalog.ModuleInfoList)
                {
                    _moduleHolder.Add(info.TypeName, RuntimeModuleHolder.CreateHolder(info, this));
                }
            }
        }

        public object GetModule(string fullIntfName)
        {
            if (string.IsNullOrEmpty(fullIntfName))
                throw new ArgumentNullException("interface name");

            RuntimeModuleHolder holder;
            if (_moduleIntfCache.TryGetValue(fullIntfName, out holder))
                return holder.GetModule();

            lock (this)
            {
                if (_moduleIntfCache.TryGetValue(fullIntfName, out holder))
                    return holder.GetModule();

                foreach (RuntimeModuleHolder h in _moduleHolder.Values)
                {
                    Type intfType = h.ModuleInfo.GetModuleType().GetInterface(fullIntfName, true);
                    if (intfType != null)
                    {
                        holder = h;
                        break;
                    }
                }
                if (holder != null)
                {
                    _moduleIntfCache.Add(fullIntfName, holder);
                    return holder.GetModule();
                }
            }
            throw new NotFoundException("Module not registered for the interface: " + fullIntfName);
        }

        public bool RegisterModule(ModuleInfo info)
        {
            if (info == null)
                throw new ArgumentNullException("module info");

            if (_moduleHolder.ContainsKey(info.TypeName))
                return false;

            lock (this)
            {
                if (_moduleHolder.ContainsKey(info.TypeName))
                    return false;
                _moduleHolder.Add(info.TypeName, RuntimeModuleHolder.CreateHolder(info, this));
                return true;
            }
        }

        public void Unload()
        {
            lock (this)
            {
                foreach (RuntimeModuleHolder holder in _moduleHolder.Values)
                {
                    try
                    {
                        holder.Unload();
                    }
                    catch(Exception ex)
                    {
						Tracer.Error( ex );
                    }
                }
                _moduleHolder.Clear();
                _moduleIntfCache.Clear();
            }
        }

        public ISessionProvider SessionProvider
        {
            get { return _sessionProvider; }
        }

        public object this[string key]
        {
            get { return _contextData[key]; }
            set { _contextData[key] = value; }
        }
    }
}
