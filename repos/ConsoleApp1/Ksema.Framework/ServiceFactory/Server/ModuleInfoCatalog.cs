using System;
using System.Xml.Serialization;

namespace Framework.Extensions
{
    [Serializable]
    [XmlRoot("ModuleCatalog")]
    public class ModuleInfoCatalog
    {
        [XmlArray("Modules")]
        [XmlArrayItem("Module")]
        public ModuleInfo[] ModuleInfoList;
    }
}
