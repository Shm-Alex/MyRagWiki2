using System;
using System.Collections.Generic;
using System.Text;
using Ksema.ObjectModel;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;
using Ksema.MetaModels.SHART.Shart2OLTP;
using Microsoft.Practices.EnterpriseLibrary.Validation;

namespace Shart.ModelDef
{
    [HasSelfValidation]
	[Serializable]
	public abstract class BaseIndexFieldMetaData
    {
        [CSReference( "Owner", false, "Fields" )]
        private CSSingleLink<BaseIndexFieldMetaData, IndexMetaData> _owner = null;

        [CSAttribute( "SerialNo" )]
        private string _serialNo = string.Empty;

		[CSAttribute( "IsIncluded" )]
		private bool _isIncluded = false;

        public BaseIndexFieldMetaData()
		{
			CSPropMgr.Instance.setupObject( this, false );
		}

        public abstract string FieldName { get; set; }

    	public bool IsIncluded
    	{
    		get
    		{
    			return _isIncluded;
    		}
    		set
    		{
    			_isIncluded = value;
    		}
    	}

    	public string SerialNo
        {
            get { return _serialNo; }
            set { _serialNo = value; }
        }

        public int Index
        {
            get
            {
                return Owner != null ? Owner.Fields.IndexOf( this ) : 0;
            }
        }  

        public IndexMetaData Owner
        {
            get { return _owner.LinkedObject; }
            set { _owner.LinkedObject = value; }
        }

        [SelfValidation]
        public abstract void DoValidate( ValidationResults results );
    }

	[Serializable]
	public class IndexFieldMetaData : BaseIndexFieldMetaData
	{
		[CSReference( "Field" )]
		private CSSingleLink<IndexFieldMetaData, FieldMetaData> _field = null;

        public IndexFieldMetaData( FieldMetaData field ) : base()
        {
//            CSPropMgr.Instance.setupObject( this, false );
            Field = field;
        }

        public IndexFieldMetaData()
            : base()
		{
///			CSPropMgr.Instance.setupObject( this, false );
		}

        public override string FieldName
        {
            get
            {
                return Field.Name;
            }
            set
            {
                //ничего не делаем
            }
        }   

        [NotNullValidator]
		public FieldMetaData Field
		{
			get { return _field.LinkedObject; }
			set { _field.LinkedObject = value; }
		}

        public override void DoValidate( ValidationResults results )
        {
			if( Field == null )
			{
				string errorMeassage = string.Format( "Field property not set. Index {0}.{1}. Field position: {2}", Owner.Owner.Name, Owner.Name, Owner.Fields.IndexOf( this ) );
				ValidationResult vr = new ValidationResult( errorMeassage, this, "Field", null, null );
				results.AddResult( vr );
			}

            if( Owner.Owner is CubeMetaData &&
                Owner.DBTable == DatabaseTable.MainTable
                && Field is ReferenceMetaData 
                && !((ReferenceMetaData)Field).IsIdentifying )
            {
                string errorMeassage = string.Format( "Index {0}.{1} contain field {2} that is not identifying", Owner.Owner.Name, Owner.Name, Field.Name );
                ValidationResult vr = new ValidationResult( errorMeassage, this, "Fields", null, null );
                results.AddResult( vr );
            }
        }
	}

	[Serializable]
	public class IndexSystemFieldMetaData : BaseIndexFieldMetaData
    {
        [CSAttribute( "FieldName" )]
        private string _fieldName = string.Empty;


        public override string FieldName
        {
            get
            {
                return _fieldName;
            }
            set
            {
                _fieldName = value;
            }
        }

        public override void DoValidate( ValidationResults results )
        {
            //string errorMeassage = "Index must contain indexed fields";
            //ValidationResult vr = new ValidationResult( errorMeassage, this, "Fields", null, null );
            //results.AddResult( vr );
        }

		public IndexSystemFieldMetaData Clone()
		{
			var result = (IndexSystemFieldMetaData)MemberwiseClone();
			CSPropMgr.Instance.setupObject( result, true );
			return result;
		}
	}

	[Serializable]
	public class IndexSystemIDFieldMetaData : BaseIndexFieldMetaData
    {
        private string _fieldName;

        public IndexSystemIDFieldMetaData( string fieldName ) : base()
        {
            _fieldName = fieldName;
        }

        public IndexSystemIDFieldMetaData(  )
            : base()
        {
        }

        public override string FieldName
        {
            get
            {
                string result = null;
                if( Owner != null )
                {
                    if( Owner.Owner is DimensionMetaData )
                        result = ShartMappingUtils.GetDimIDName( Owner.Owner );
                    else if( Owner.Owner is CubeMetaData )
                    {
                        result = ShartMappingUtils.GetCubeIDName( Owner.Owner );
                    }
                }
                else
                {

                    result = _fieldName;
                }
                return result;
            }
            set
            {
                //ничего не делаем
            }
        }

		public IndexSystemIDFieldMetaData Clone()
		{
			var result = (IndexSystemIDFieldMetaData)MemberwiseClone();
			CSPropMgr.Instance.setupObject( result, true );
			return result;
		}


        public override void DoValidate( ValidationResults results )
        {
            //string errorMeassage = "Index must contain indexed fields";
            //ValidationResult vr = new ValidationResult( errorMeassage, this, "Fields", null, null );
            //results.AddResult( vr );
        }
    }


	[Serializable]
	public class IndexAnalysisFieldMetaData : BaseIndexFieldMetaData
    {
        [CSReference( "Field" )]
        private CSSingleLink<IndexAnalysisFieldMetaData, DataFieldMetaData> _field = null;

        [CSAttribute( "Level" )]
        private int _level;

        public IndexAnalysisFieldMetaData( DataFieldMetaData field, int level  )
            : base()
        {
            //            CSPropMgr.Instance.setupObject( this, false );
            _level = level;
            Field = field;
        }

        public IndexAnalysisFieldMetaData()
            : base()
        {
            ///			CSPropMgr.Instance.setupObject( this, false );
        }

        public override string FieldName
        {
            get
            {
                return ShartMappingUtils.GetAnalysisColumnName( Field.Name, Level );
            }
            set
            {
                //ничего не делаем
            }
        }

        [NotNullValidator]
        public DataFieldMetaData Field
        {
            get { return _field.LinkedObject; }
            set { _field.LinkedObject = value; }
        }

        public int Level
        {
            get { return _level; }
            set { _level = value; }
        }

		public IndexAnalysisFieldMetaData Clone()
		{
			var result = (IndexAnalysisFieldMetaData)MemberwiseClone();
			CSPropMgr.Instance.setupObject( result, true );
			return result;
		}

        public override void DoValidate( ValidationResults results )
        {
            if( Owner != null && Owner.Owner != null && Owner.Owner is DimensionMetaData
                && Level > ((DimensionMetaData)Owner.Owner).LevelCount )
            {
                string errorMeassage = string.Format( "Index {0}.{1} contains nonexistent field: {2}", Owner.Owner.Name, Owner.Name, FieldName );
                ValidationResult vr = new ValidationResult( errorMeassage, this, FieldName, null, null );
                results.AddResult( vr );
            }
        }
    }

	[Serializable]
	public class IndexParentLevelFieldMetaData : BaseIndexFieldMetaData
    {
        private int _level;

        public IndexParentLevelFieldMetaData( int level )
            : base()
        {
            //            CSPropMgr.Instance.setupObject( this, false );
            _level = level;
        }

        public IndexParentLevelFieldMetaData()
            : base()
        {
            ///			CSPropMgr.Instance.setupObject( this, false );
        }

        public override string FieldName
        {
            get
            {
                return ShartMappingUtils.GetParentColumnName( Level );
            }
            set
            {
                //ничего не делаем
            }
        }

        [CSAttribute( "Level" )]
        public int Level
        {
            get { return _level; }
            set { _level = value; }
        }

		public IndexParentLevelFieldMetaData Clone()
		{
			var result = (IndexParentLevelFieldMetaData)MemberwiseClone();
			CSPropMgr.Instance.setupObject( result, true );
			return result;
		}

        public override void DoValidate( ValidationResults results )
        {
            if( Owner != null && Owner.Owner != null && Owner.Owner is DimensionMetaData
                && Level > ((DimensionMetaData)Owner.Owner).LevelCount )
            {
                string errorMeassage = string.Format( "Index {0}.{1} contains nonexistent field: {2}", Owner.Owner.Name, Owner.Name, FieldName );
                ValidationResult vr = new ValidationResult( errorMeassage, this, FieldName, null, null );
                results.AddResult( vr );
            }
        }
    }

}