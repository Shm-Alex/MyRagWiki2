using Serializer;

namespace Ksema.MetaModels.OLTP
{
    public static class OLTPSerializer
    {
        static MetaData _metaData;

        public static MetaData GetMetaData()
        {
            if (_metaData == null)
            {
                MetaData md = new MetaData();
                md.SetName(typeof (OLTPSchema), "OLTPSchema");
                md.SetName(typeof (Table), "Table");
                md.SetName(typeof (Column), "Column");
                md.SetName(typeof (DataType), "DataType");
                md.SetName(typeof (FK), "FK");
                md.SetName(typeof (FKColumn), "FKColumn");

                _metaData = md;
            }
            return _metaData;
        }
    }
}
