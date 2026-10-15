using System;
using System.Collections.Generic;
using Framework.Extensions;
using Framework.Remoting;

namespace Framework.Extensions
{
    class RuntimeModuleHolder
    {
        private readonly ModuleInfo _info;
        private readonly IRuntimeModuleContext _globalCtx;


        protected RuntimeModuleHolder(ModuleInfo aInfo, IRuntimeModuleContext globalCtx)
        {
            _info = aInfo;
            _globalCtx = globalCtx;
        }

        public ModuleInfo ModuleInfo
        {
            get { return _info; }
        }

        protected IRuntimeModuleContext GlobalContext
        {
            get { return _globalCtx; }
        }

        public static RuntimeModuleHolder CreateHolder(ModuleInfo info, IRuntimeModuleContext globalCtx)
        {
            RuntimeModuleHolder holder;

            lock (typeof(RuntimeModuleHolder))
            {
                switch (info.ActivationMode)
                {
                    case ModuleActivationMode.OnDemand:
                        holder = new RuntimeModuleHolder(info, globalCtx);
                        break;
                    case ModuleActivationMode.Session:
                        holder = new SessionModuleHolder(info, globalCtx);
                        break;
                    case ModuleActivationMode.Singleton:
                        holder = new SingletonModuleHolder(info, globalCtx);
                        break;
                    default:
                        holder = new RuntimeModuleHolder(info, globalCtx);
                        break;
                }
            }
            return holder;
        }

        public virtual void Unload()
        {
        }


        public virtual MarshalByRefObject GetModule()
        {
            return CreateModule();
        }

        protected MarshalByRefObject CreateModule()
        {
            MarshalByRefObject module = (MarshalByRefObject)Activator.CreateInstance(ModuleInfo.GetModuleType());
            IExtensionModule shModule = module as IExtensionModule;
            if (shModule != null)
                shModule.OnLoad(ModuleInfo.GetNamedArguments());
            return module;
        }
    }

    class SingletonModuleHolder : RuntimeModuleHolder
    {
        SponsorshipManager _globalSponsor;
        MarshalByRefObject _singleInstance = null;
        const string S_KEY = "globalsponsor";

        public SingletonModuleHolder(ModuleInfo aInfo, IRuntimeModuleContext ctx)
            : base(aInfo, ctx)
        {
            object val = ctx[S_KEY];
            if (val == null)
            {
                _globalSponsor = new SponsorshipManager();
                ctx[S_KEY] = _globalSponsor;
            }
            else
                _globalSponsor = (SponsorshipManager) val;
        }

        public override MarshalByRefObject GetModule()
        {
            return GetSingleInstance();
        }

        private MarshalByRefObject GetSingleInstance()
        {
            if (_singleInstance == null)
            {
                lock (this)
                {
                    if (_singleInstance == null)
                    {
                        _singleInstance = CreateModule();
                        _globalSponsor.Register(_singleInstance);
                    }
                }
            }
            return _singleInstance;
        }

        public override void Unload()
        {
            IExtensionModule module = _singleInstance as IExtensionModule;
            if (module != null)
            {
                lock (this)
                {
                    if (_singleInstance != null)
                    {
                        module.OnUnload();
                        _globalSponsor.Unregister(_singleInstance);
                        _singleInstance = null;
                    }
                }
            }

        }
    }

    class SessionModuleHolder : RuntimeModuleHolder
    {
        SessionSponsorshipManager _sessionSponsor;
        string S_KEY = "sessionsponsor";

        Dictionary<string, MarshalByRefObject> _sessionInstances = new Dictionary<string, MarshalByRefObject>();


        public SessionModuleHolder(ModuleInfo aInfo, IRuntimeModuleContext ctx)
            : base(aInfo, ctx)
        {
            object val = ctx[S_KEY];
            if (val == null)
            {
                _sessionSponsor = new SessionSponsorshipManager(GlobalContext.SessionProvider);
                ctx[S_KEY] = _sessionSponsor;
            }
            else
                _sessionSponsor = (SessionSponsorshipManager) val;

        }

        public override MarshalByRefObject GetModule()
        {
            return GetSessionInstance();
        }

        private MarshalByRefObject GetSessionInstance()
        {
            string key = GlobalContext.SessionProvider.GetCurrentSessionID();
            if (string.IsNullOrEmpty(key))
                throw new InvalidOperationException("cannot find current session!");
            MarshalByRefObject module;
            if (!_sessionInstances.TryGetValue(key, out module))
            {
                lock (this)
                {
                    if (!_sessionInstances.TryGetValue(key, out module))
                    {
                        module = CreateModule();
                        _sessionInstances[key] = module;
                        _sessionSponsor.Register(module, key);
                    }
                }
            }
            return module;
        }

        public override void Unload()
        {
            IExtensionModule module;

            if (_sessionInstances.Count > 0)
            {
                lock (this)
                {
                    foreach (MarshalByRefObject obj in _sessionInstances.Values)
                    {
                        module = obj as IExtensionModule;
                        if (module != null)
                            module.OnUnload();
                        _sessionSponsor.Unregister(obj);
                    }
                    _sessionInstances.Clear();
                }
            }

        }

    }

}
