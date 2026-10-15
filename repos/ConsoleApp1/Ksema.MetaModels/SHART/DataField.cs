using Ksema.ObjectModel;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;
using System;

namespace Shart.ModelDef
{
	[Serializable]
	public class DataFieldMetaData : FieldMetaData, IDataFieldMetaData//, ICloneable
    {
        public const string FulltextIgnoreColumProperty = "fulltextIgnoreColumn";

		public static readonly int TEXT_TYPE_DATA_SIZE;
		public static readonly int IMAGE_TYPE_DATA_SIZE;

		public static readonly int STRING_TYPE_MAX_DATA_SIZE = 8000;
		public static readonly int LONGBINARY_TYPE_MAX_DATA_SIZE = 8000;

        private DataType _dataType = DataType.String;
        private bool _isAnalysis = false;
        private int _dataSize = 0;

		static DataFieldMetaData()
		{
			TEXT_TYPE_DATA_SIZE = (int)(Math.Pow( 2, 30 ) - 1);
			IMAGE_TYPE_DATA_SIZE = (int)(Math.Pow( 2, 31 ) - 1);
		}

        [CSAttribute( "DataType" )]
        public override DataType DataType
        {
            get { return _dataType; }
            set
            {
                _dataType = value;
                switch( _dataType )
                {
                    case DataType.Number:
                        _dataSize = 8;
                        break;
                    case DataType.String:
                        break;
					case DataType.Text:
						_dataSize = TEXT_TYPE_DATA_SIZE;
						//_dataSize = 2;
						break;
					case DataType.Boolean:
                        _dataSize = 1;
                        break;
                    case DataType.DateTime:
                        _dataSize = 8;
                        break;
                    case DataType.Integer:
                        _dataSize = 4;
                        break;
                    case DataType.LongBinary:
                        break;
					case DataType.Image:
						_dataSize = IMAGE_TYPE_DATA_SIZE;
						break;
				}

				CheckDataSize();
            }
        }


        [CSAttribute( "DataSize" )]
        [RangeValidator( 0, RangeBoundaryType.Inclusive, int.MaxValue, RangeBoundaryType.Inclusive )]
        public override int DataSize
        {
            get { return _dataSize; }
            set
            {
				if( !IsFixedLengthType( _dataType ) )
				{
					_dataSize = value;
					CheckDataSize();
				}
            }
        }

		private void CheckDataSize()
		{
			if( !IsFixedLengthType( _dataType ) && _dataSize > MaxDataTypeSize( _dataType ) )
			{
				_dataSize = MaxDataTypeSize( _dataType );
			}

		}

        [CSAttribute( "IsAnalysis" )]
        public bool IsAnalysis
        {
            get { return _isAnalysis; }
            set { _isAnalysis = value; }
        }

        public bool IsIndexed
        {
            get
            {
                return (this.DataType == DataType.String) && (!this.Properties.ContainsKey(FulltextIgnoreColumProperty) || !(bool)this.Properties[FulltextIgnoreColumProperty].Value);
            }
        }

        public override string ToString()
        {
            if( Owner != null )
                return Owner.Name + '.' + Name;
            else
                return Name;
        }

        public static bool IsFixedLengthType( DataType dataType )
        {
            bool result;
            switch( dataType )
            {
                case DataType.Number:
                case DataType.Boolean:
                case DataType.DateTime:
                case DataType.Integer:
				case DataType.Text:
				case DataType.Image:
                    result = true;
                    break;
                default:
                    result = false;
                    break;
            }
            return result;
        }

		/// <summary>
		/// Макс длина данных для типов данных с переменной длиной
		/// </summary>
		/// <param name="dataType"></param>
		/// <returns></returns>
		public static int MaxDataTypeSize( DataType dataType )
		{
			int result;
			switch( dataType )
			{
				case DataType.String:
					result = STRING_TYPE_MAX_DATA_SIZE;
					break;
				case DataType.LongBinary:
					result = LONGBINARY_TYPE_MAX_DATA_SIZE;
					break;
				default:
					result = -1;
					break;
			}
			return result;
		}

		public DataFieldMetaData Clone()
		{
			DataFieldMetaData result = (DataFieldMetaData)MemberwiseClone();
			result.GUID = Guid.NewGuid().ToString();
			CSPropMgr.Instance.setupObject( result, true );

			// Свойства
			foreach (var property in Properties.Values)
			{
				result.AddProperty(property.Clone());
			}

			return result;
		}

		#region ICloneable Members

		//object ICloneable.Clone()
		//{
		//    return Clone();
		//}

		#endregion

	}
}
