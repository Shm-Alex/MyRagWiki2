using System;
using System.Collections.Generic;
using System.Linq;

namespace Shart.ModelDef
{
    public class CubeDTO : OLAPObjectDTO, ICubeMetaData
    {
        public IList<IReferenceMetaData> GetIdentifyFields()
        {
            return GetReferences().Where(r => r.IsIdentifying).ToList();
        }
    }
}
