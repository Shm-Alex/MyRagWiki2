using System.Collections.Generic;

namespace Shart.ModelDef
{
    public interface IShartModelMetaData
    {
        string Name { get; }

    	IOLAPObjectMetaData TryGetContentMetaData( string sysName );

        IDimensionMetaData GetDimensionMetaData(string sysName);
        IDimensionMetaData TryGetDimensionMetaData(string sysName);
        ICubeMetaData GetCubeMetaData(string sysName);
        ICubeMetaData TryGetCubeMetaData(string sysName);
        ICollection<IDimensionMetaData> GetAllDimensionsMetaData();
        ICollection<ICubeMetaData> GetAllCubesMetaData();

        string DefaultClassName { get; }
        string DefaultSetName { get; }
        string DefaultRefName { get; }
        string DefaultBackRefName { get; }

        string AssemblyName { get; }
        string Namespace { get; }
    }
}