using System.Collections.Generic;

namespace Shart.ModelDef
{
    public interface ICubeMetaData : IOLAPObjectMetaData
    {
        IList<IReferenceMetaData> GetIdentifyFields();
    }
}