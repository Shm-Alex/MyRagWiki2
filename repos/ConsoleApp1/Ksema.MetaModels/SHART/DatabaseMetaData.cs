using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;
using Shart.ModelDef;
using Ksema.ObjectModel;
using Ksema.Framework;

namespace Shart.ModelDef
{
	public enum PartitionFunctionRange
	{
		Left,
		Right
	}

	[Serializable]
	public class DatabaseMetaData
	{
		public DatabaseMetaData()
		{
			CSPropMgr.Instance.setupObject( this, false );
		}

		[CSReference( "Owner", false, "Database" )]
		private CSSingleLink<DatabaseMetaData, ShartModelMetaData> _owner;


		[CSReference( "PartitionFunctions", true, "Database" )]
		private CSMultiLink<DatabaseMetaData, PartitionFunction> _partitionFunctions;

		[CSReference( "PartitionSchemes", true, "Database" )]
		private CSMultiLink<DatabaseMetaData, PartitionScheme> _partitionSchemes;


		public ShartModelMetaData Owner
		{
			get
			{
				return _owner.LinkedObject;
			}
			set
			{
				_owner.LinkedObject = value;
			}
		}

		public IList<PartitionFunction> PartitionFunctions
		{
			get
			{
				return _partitionFunctions;
			}
		}

		public IList<PartitionScheme> PartitionSchemes
		{
			get
			{
				return _partitionSchemes;
			}
		}
	}

	public interface IPartitionFunction: IBaseMetaDataDef
	{
		PartitionFunctionRange Range { get; }
		DataType InputParameterType { get; }
		int InputParameterMaxLength { get;  }
		IList<StringValue> RangeValues { get; }
	}

	public interface IPartitionScheme: IBaseMetaDataDef
	{
		IPartitionFunction Function
		{
			get;
			set;
		}
		IList<PartitionSchemeFilegroup> Filegroups
		{
			get;
		}
	}

	[Serializable]
	public class PartitionFunction: BaseMetaData, IObjectURIProvider, IPartitionFunction
	{
		public const string MOST_LEFT_RANGE = "MOST LEFT RANGE";
		public const string MOST_RIGHT_RANGE = "MOST RIGHT RANGE";

		[CSReference( "Database", false, "PartitionFunctions" )]
		private CSSingleLink<PartitionFunction, DatabaseMetaData> _database;

		[CSAttribute( "Range" )]
		private PartitionFunctionRange _range = PartitionFunctionRange.Right;

		[CSAttribute( "InputParameterType" )]
		private DataType _inputParameterType = DataType.Number;

		[CSAttribute( "InputParameterMaxLength" )]
		private int _inputParameterMaxLength = 1;

		[CSReference( "RangeValues", true )]
		private CSMultiLink<PartitionFunction, StringValue> _rangeValues;

		public PartitionFunction()
		{
			CSPropMgr.Instance.setupObject( this, false );
		}

		public DatabaseMetaData Database
		{
			get
			{
				return _database.LinkedObject;
			}
			set
			{
				_database.LinkedObject = value;
			}
		}

		public PartitionFunctionRange Range
		{
			get
			{
				return _range;
			}
			set
			{
				_range = value;
			}
		}

		public DataType InputParameterType
		{
			get
			{
				return _inputParameterType;
			}
			set
			{
				_inputParameterType = value;
			}
		}

		public int InputParameterMaxLength
		{
			get
			{
				return _inputParameterMaxLength;
			}
			set
			{
				_inputParameterMaxLength = value;
			}
		}

		public IList<StringValue> RangeValues
		{
			get
			{
				return _rangeValues;
			}
		}

		public override string ToString()
		{
			return Name;
		}

		#region IObjectURIProvider Members

		public ObjectURI CreateURI()
		{
			ObjectURI result = new ObjectURI( "PartitionFunction/" + GUID );
			return result;
		}

		#endregion
	}


	[Serializable]
	public class PartitionScheme: DatabaseDataSpace, IObjectURIProvider, IPartitionScheme
	{
		[CSReference( "Database", false, "PartitionSchemes" )]
		private CSSingleLink<PartitionScheme, DatabaseMetaData> _database;

		[CSReference( "Function", false )]
		private CSSingleLink<PartitionScheme, PartitionFunction> _function;

		[CSReference( "Filegroups", true )]
		private CSMultiLink<PartitionScheme, PartitionSchemeFilegroup> _filegroups = null;

		public PartitionScheme()
		{
			CSPropMgr.Instance.setupObject( this, false );
		}

		public DatabaseMetaData Database
		{
			get
			{
				return _database.LinkedObject;
			}
			set
			{
				_database.LinkedObject = value;
			}
		}



		public IPartitionFunction Function
		{
			get
			{
				return _function.LinkedObject;
			}
			set
			{
				_function.LinkedObject = (PartitionFunction)value;
			}
		}

		public IList<PartitionSchemeFilegroup> Filegroups
		{
			get
			{
				return _filegroups;
			}
		}

		public ObjectURI CreateURI()
		{
			ObjectURI result = new ObjectURI( "PartitionScheme/" + GUID );
			return result;
		}

		public override string ToString()
		{
			return Name;
		}
	}

	[Serializable]
	public class StringValue
	{
		[CSAttribute( "Value" )]
		private string _value;

		public StringValue()
		{
		}

		public StringValue( string str )
		{
			_value = str;
		}

		public string Value
		{
			get
			{
				return _value;
			}
			set
			{
				_value = value;
			}
		}

		public override string ToString()
		{
			return Value;
		}
	}

	[Serializable]
	public class PartitionSchemeFilegroup : BaseMetaData
	{

		[CSReference( "Filegroup", false )]
		private CSSingleLink<PartitionSchemeFilegroup, DatabaseFilegroup> _filegroup;

		[CSReference( "Value", false )]
		private CSSingleLink<PartitionSchemeFilegroup, StringValue> _value;

		public PartitionSchemeFilegroup()
		{
			CSPropMgr.Instance.setupObject( this, false );
		}

		public DatabaseFilegroup Filegroup
		{
			get
			{
				return _filegroup.LinkedObject;
			}
			set
			{
				_filegroup.LinkedObject = value;
			}
		}

		public StringValue Value
		{
			get
			{
				return _value.LinkedObject;
			}
			set
			{
				_value.LinkedObject = value;
			}
		}
	}
}