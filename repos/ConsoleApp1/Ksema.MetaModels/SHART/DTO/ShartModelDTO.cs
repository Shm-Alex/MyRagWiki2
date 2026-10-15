using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;

namespace Shart.ModelDef
{
    [DataContract]
    [KnownType(typeof(CubeDTO)),
    KnownType(typeof(DimensionDTO)),
    KnownType(typeof(DataFieldDTO)),
    KnownType(typeof(ReferenceDTO))]
    public class ShartModelDTO : IShartModelMetaData
    {
        public ShartModelDTO()
        {
            Contents = new Dictionary<string, OLAPObjectDTO>();
        }

        [DataMember]
        public IDictionary<string, OLAPObjectDTO> Contents{get; internal set;}

        public IOLAPObjectMetaData TryGetContentMetaData(string sysName)
        {
            OLAPObjectDTO data;
            if (Contents.TryGetValue(sysName, out data))
                return data;
            return null;
        }

        public IDimensionMetaData GetDimensionMetaData(string sysName)
        {
            return (IDimensionMetaData) Contents[sysName];
        }

        public IDimensionMetaData TryGetDimensionMetaData(string sysName)
        {
            return TryGetContentMetaData(sysName) as IDimensionMetaData;
        }

        public ICubeMetaData GetCubeMetaData(string sysName)
        {
            return (ICubeMetaData) Contents[sysName];
        }

        public ICubeMetaData TryGetCubeMetaData(string sysName)
        {
            return TryGetContentMetaData(sysName) as ICubeMetaData;
        }

        public ICollection<IDimensionMetaData> GetAllDimensionsMetaData()
        {
            return Contents.Values.Where(data => data is IDimensionMetaData).Cast<IDimensionMetaData>().ToList();
        }

        public ICollection<ICubeMetaData> GetAllCubesMetaData()
        {
            return Contents.Values.Where(data => data is ICubeMetaData).Cast<ICubeMetaData>().ToList();
        }

        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string DefaultClassName { get; set; }
        [DataMember]
        public string DefaultSetName { get; set; }
        [DataMember]
        public string DefaultRefName { get; set; }
        [DataMember]
        public string DefaultBackRefName { get; set; }
        [DataMember]
        public string AssemblyName { get; set; }
        [DataMember]
        public string Namespace { get; set; }
    }
}