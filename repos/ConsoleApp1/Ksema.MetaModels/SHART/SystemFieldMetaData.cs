using Ksema.MetaModels.SHART.Shart2OLTP;
using System;
namespace Shart.ModelDef
{
	public class SystemFieldMetaData: BaseMetaData, IFieldMetaData
	{
		private string _formula;
		private IOLAPObjectMetaData _owner;
		private string _runtimeDataTypeName;
		private DataType _dataType;
		private int _dataSize;
		private bool _isNull = true;
		private bool _isComputed;

		public SystemFieldMetaData()
		{
		}

		#region IFieldMetaData Members

		public IOLAPObjectMetaData Owner
		{
			get
			{
				return _owner;
			}
			set
			{
				_owner = value;
			}
		}

		public string RuntimeDataTypeName
		{
			get
			{
				return FieldMetaData.GetRuntimeDataType( this.DataType );
			}
		}

		public object DefaultValue
		{
			get
			{
				return FieldMetaData.GetDefaultValue( this.DataType );
			}
		}

		public virtual DataType DataType
		{
			get
			{
				return _dataType;
			}
			set
			{
				_dataType = value;
			}
		}

		public int DataSize
		{
			get
			{
				return _dataSize;
			}
			set
			{
				_dataSize = value;
			}
		}

		public bool IsComputed
		{
			get
			{
				return _isComputed;
			}
			set
			{
				_isComputed = value;
			}
		}

		public string Formula
		{
			get
			{
				return _formula;
			}
			set
			{
				_formula = value;
			}
		}

		#endregion

		public virtual bool IsNull
		{
			get
			{
				return _isNull;
			}
			set
			{
				_isNull = value;
			}
		}
	}

	/// <summary>
	/// DimNameID, CellID
	/// </summary>
	public class SystemIDFieldMetaData: SystemFieldMetaData
	{
		public SystemIDFieldMetaData( IOLAPObjectMetaData olapObj )
		{
			Owner = olapObj;

		}

		public override string Name
		{
			get
			{
				string result = null;
				if( Owner != null )
				{
					if( Owner is DimensionMetaData )
						result = ShartMappingUtils.GetDimIDName( Owner );
					else if( Owner is CubeMetaData )
					{
						result = ShartMappingUtils.GetCubeIDName( Owner );
					}
				}
				return result;
			}
			set
			{
			}
		}

		public override DataType DataType
		{
			get
			{
				DataType result = DataType.String;
				if( Owner != null )
				{
					if( Owner is DimensionMetaData )
						result = DataType.Integer;
					else if( Owner is CubeMetaData )
					{
						result = result = DataType.LongInteger;
					}
				}
				else
				{
					throw new Exception( "Property 'Owner' not set." );
				}
				return result;
			}
			set
			{
				;
			}
		}

		public override bool IsNull
		{
			get
			{
				return false;
			}
			set
			{
				;
			}
		}

	}

	public class SystemAnalysisFieldMetaData: SystemFieldMetaData
	{
		private DataFieldMetaData _field;
		private int _level;

		public SystemAnalysisFieldMetaData( DataFieldMetaData field, int level )
		{
			_field = field;
			_level = level;
		}

		public override string Name
		{
			get
			{
				return ShartMappingUtils.GetAnalysisColumnName( _field.Name, _level );
			}
			set
			{
			}
		}

		public override DataType DataType
		{
			get
			{
				return _field.DataType;
			}
			set
			{
				;
			}
		}
	}

	public class SystemParentLevelFieldMetaData: SystemFieldMetaData
	{
		private int _level;

		public SystemParentLevelFieldMetaData( int level )
		{
			_level = level;
		}

		public override string Name
		{
			get
			{
				return ShartMappingUtils.GetParentColumnName( _level );
			}
			set
			{
			}
		}

		public override DataType DataType
		{
			get
			{
				return DataType.Integer;
			}
			set
			{
			}
		}
	}
}