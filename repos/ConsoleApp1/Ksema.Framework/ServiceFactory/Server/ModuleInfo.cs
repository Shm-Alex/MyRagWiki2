using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace Framework.Extensions
{
    public enum ModuleActivationMode
    {
        Session, OnDemand, Singleton
    }

    [Serializable]
    public class NamedArg
    {
        [XmlAttribute]
        public string Name;

        [XmlText]
        public string Value;
    }

    [Serializable]
    public class ModuleInfo
    {
        string _name;
        string _binPath;
        bool _requiresSession;
        ModuleActivationMode _activationMode = ModuleActivationMode.OnDemand;
        string _assemblyName;
        string _typeName;
        Type _moduleType;

        [XmlAttribute]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        [XmlArrayItem("Arg")]
        public NamedArg[] Arguments;

        public IDictionary<string, string> GetNamedArguments()
        {
            Dictionary<string, string> args = new Dictionary<string,string>();
            if (Arguments == null || Arguments.Length == 0)
                return args;
            for (int i = 0; i < Arguments.Length; i++)
            {
                if(!string.IsNullOrEmpty(Arguments[i].Name))
                    args[Arguments[i].Name.Trim()] = Framework.Utils.Utils.Trim(Arguments[i].Value);
            }

            return args;
        }

        [XmlAttribute]
        public bool RequiresSession
        {
            get { return _requiresSession; }
            set { _requiresSession = value; }
        }

        [XmlAttribute]
        public ModuleActivationMode ActivationMode
        {
            get { return _activationMode; }
            set { _activationMode = value; }
        }

        [XmlElement]
        public string BinPath
        {
            get { return _binPath; }
            set { _binPath = value; _moduleType = null; }
        }

        [XmlElement]
        public string AssemblyName
        {
            get { return _assemblyName; }
            set { _assemblyName = value; _moduleType = null; }
        }

        [XmlElement]
        public string TypeName
        {
            get { return _typeName; }
            set { _typeName = value; _moduleType = null; }
        }

        string GetFullAssemblyPath()
        {
            string fullPath;
            if (string.IsNullOrEmpty(BinPath))
                fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssemblyName + ".dll");
            else 
                fullPath = Path.Combine(BinPath, AssemblyName + ".dll");
            return fullPath;
        }

        public Type GetModuleType()
        {
            if (_moduleType == null)
            {
                lock (this)
                {
                    if (_moduleType == null)
                    {
                        string fullPath = GetFullAssemblyPath();
                        Assembly asm = Assembly.LoadFrom(fullPath);
                        _moduleType = asm.GetType(TypeName, true, true);
                    }
                }
            }
            return _moduleType;
        }
    }
}
