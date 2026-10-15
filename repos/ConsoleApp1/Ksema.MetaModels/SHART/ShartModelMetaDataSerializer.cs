using System;
using System.Collections;
using System.IO;
using System.Text;
using Framework.Utils;
using Ksema.ObjectModel.Serialization;
using Serializer;
using System.Runtime.Serialization;

namespace Shart.ModelDef
{
	public class MetaDataIdProvider : IIdentityProvider
	{
		IIdentityProvider defaultProvider = new EnumerateIdProvider();
		public object getIdentity(object item)
		{
			if (item is IBaseMetaDataDef)
			{
				string guid = (item as IBaseMetaDataDef).GUID;
				if (!String.IsNullOrEmpty(guid))
					return guid;
			}
			return defaultProvider.getIdentity(item);
		}
	}
	public class ShartModelMetaDataSerializer : IMetaDataProvider
	{
	    private static MetaData __meta = null;


	    public static IShartModelMetaData Load( string fileName )
		{
			CSSerializer ser = GetSerializer();
			ICollection col = ser.Deserialize(fileName);

            IShartModelMetaData model = (IShartModelMetaData) CUtils.getFirstItem(col);

            return model;
		}

		public static IShartModelMetaData Load( Stream stream )
		{
			CSSerializer ser = GetSerializer();
			ICollection col = ser.Deserialize(stream);

			IShartModelMetaData model = (IShartModelMetaData)CUtils.getFirstItem( col );

			return model;
		}

		public static ShartModelBackup LoadBackup( string fileName )
		{
			CSSerializer ser = GetSerializer();
			ICollection col = ser.Deserialize(fileName);

			ShartModelBackup model = (ShartModelBackup)CUtils.getFirstItem( col );
			return model;
		}

        public static ShartModelMetaData LoadFromString(string modelContents)
        {
            if (string.IsNullOrEmpty(modelContents))
                return new ShartModelMetaData();

			CSSerializer ser = GetSerializer();
			using (StringReader rdr = new StringReader(modelContents))
            {
                ICollection col = ser.Deserialize(rdr);
                return (ShartModelMetaData)CUtils.getFirstItem(col);
            }
        }


		public static void Save( string fileName, IShartModelMetaData model )
		{
			CSSerializer ser = GetSerializer();
			ser.Serialize(model, fileName, Encoding.UTF8);
		}

		public static void Save( TextWriter writer, IShartModelMetaData model )
		{
			CSSerializer ser = GetSerializer();
			ser.Serialize(model, writer);
		}

		public static void Save( Stream stream, IShartModelMetaData model )
		{
			CSSerializer ser = GetSerializer();
			ser.Serialize(model, stream, Encoding.UTF8);
		}

		public static void SaveBackup( string fileName, ShartModelBackup backup )
		{
			CSSerializer ser = GetSerializer();
			ser.Serialize( backup, fileName, Encoding.UTF8 );
		}


	    public static MetaData GetMetaData()
	    {
	        if (__meta == null)
	        {
	            MetaData md = new MetaData();

	            md.SetName(typeof (ShartModelMetaData), "ShartModel");
	            md.SetName(typeof (DataFieldMetaData), "DataField");
	            md.SetName(typeof (IndexMetaData), "Index");
	            md.SetName(typeof (DimensionMetaData), "Dimension");
	            md.SetName(typeof (CubeMetaData), "Cube");
	            md.SetName(typeof (ReferenceMetaData), "Reference");
                md.SetName(typeof(ShartCompositeModel), "CompositeModel");
                md.SetName(typeof(SQLTemplateMetaData), "SqlTemplate");

                md.SetName( typeof( DimensionLevelMetaData ), "DimensionLevel" );

                md.SetName( typeof( IndexFieldMetaData ), "IndexField" );
                md.SetName( typeof( IndexSystemFieldMetaData ), "IndexSystemField" );
                md.SetName( typeof( IndexSystemIDFieldMetaData ), "IndexSystemIDField" );
                md.SetName( typeof( IndexAnalysisFieldMetaData ), "IndexAnalysisField" );
                md.SetName( typeof( IndexParentLevelFieldMetaData ), "IndexParentLevelField" );

				md.SetName( typeof( ShartModelBackup ), "ShartModelBackup" );

//				md.SetName( typeof( DatabaseFile ), "DatabaseFile" );
				md.SetName(typeof(DatabaseFilegroup), "DatabaseFilegroup");

				md.SetName(typeof(IntProperty), "IntProperty");
				md.SetName(typeof(StringProperty), "StringProperty");
				md.SetName(typeof(BooleanProperty), "BooleanProperty");
				md.SetName(typeof(DecimalProperty), "DecimalProperty");
				md.SetName(typeof(DoubleProperty), "DoubleProperty");
				md.SetName(typeof(DateTimeProperty), "DateTimeProperty");

				md.SetName( typeof( ContextMetaData ), "Context" );


				md.SetName( typeof( DatabaseMetaData ), "DatabaseMetaData" );
				md.SetName( typeof( PartitionFunction ), "PartitionFunction" );
				md.SetName( typeof( PartitionScheme ), "PartitionScheme" );
				md.SetName( typeof( StringValue ), "StringValue" );
				md.SetName( typeof( DatabaseFilegroupLink ), "DatabaseFilegroupLink" );
				md.SetName( typeof( PartitionSchemeFilegroup ), "PartitionSchemeFilegroup" );
				md.SetName( typeof( UpdateScriptMetaData ), "UpdateScript" );
	            __meta = md;
	        }
	        return __meta;
	    }

		public static CSSerializer GetSerializer()
		{
			CSSerializer ser = CSSerializer.CreateSerializerWithRealLinks();
			InitSerializer(ser);
			return ser;
		}

		private static void InitSerializer(CSSerializer ser)
		{
			ser.DefaultProvider = new MetaDataIdProvider();
			ser.MetaData = GetMetaData();
		}


	    MetaData IMetaDataProvider.GetMetaData()
	    {
            return GetMetaData();
	    }

		CSSerializer IMetaDataProvider.GetSerializer()
		{
			return GetSerializer();
		}
	}
}
