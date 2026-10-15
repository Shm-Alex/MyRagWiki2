using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ksema.Framework;
using Ksema.ObjectModel;
using Microsoft.Practices.EnterpriseLibrary.Validation;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;

namespace Shart.ModelDef
{
	public interface IIndexMetaData : IBaseMetaDataDef
	{
		bool IsUnique { get; }
		bool IsPrimaryKey { get; }
		bool IsClustered { get; }
		DatabaseTable DBTable { get; }
		DatabaseDataSpace Filegroup { get; }
		IOLAPObjectMetaData Owner{ get; }
		ICollection<BaseIndexFieldMetaData> GetFields();
	}

	[HasSelfValidation]
	[Serializable]
	public class IndexMetaData : BaseMetaData, IObjectURIProvider, IIndexMetaData//, ICloneable
	{
		//[CSAttribute( "Name" )]
		//private string _name = string.Empty;
		//[CSAttribute( "Description" )]
		//private string _description = string.Empty;
		//[CSAttribute( "Guid" )]
		//private string _guid = string.Empty;

		[CSAttribute( "IsUnique" )]
		private bool _isUnique;

        [CSAttribute( "IsPrimaryKey" )]
        private bool _isPrimaryKey;

        [CSAttribute( "IsClustered" )]
        private bool _isClustered;

        [CSAttribute( "DBTable" )]
        private DatabaseTable _dbTable = DatabaseTable.ViewTable;

		[CSReference( "Fields", true, "Owner" )]
		private CSMultiLink<IndexMetaData, BaseIndexFieldMetaData> _fields = null;

		[CSReference( "Owner", false, "Indexes" )]
		private CSSingleLink<IndexMetaData, OLAPObjectMetaData> _owner = null;

        [CSReference( "Filegroup" )]
		private CSSingleLink<IndexMetaData, DatabaseDataSpace> _filegroup;

		[CSAttribute( "PartitionedField" )]
		private string _partitionedField;
        
		public IndexMetaData()
		{
			CSPropMgr.Instance.setupObject( this, false );
//			_fields = new CSMultiLink<Index, IndexField>( "Fields", "Owner", this );
//			_owner = new CSSingleLink<Index, OLAPObject>( "Owner", "Indexes", this );
		}

		[NotNullValidator]
		[StringLengthValidator( 1, 128, MessageTemplate = "Длина строки должна быть от 1 до 128 символов" )]
		[RegexValidator( @"[0-9a-zA-Z\s\-\.']*" )]
		public override string Name
		{
			get
			{
				return _name;
			}
			set
			{
				_name = value;
			}
		}


		public IList<BaseIndexFieldMetaData> Fields
		{
			get { return _fields; }
		}

		IOLAPObjectMetaData IIndexMetaData.Owner
		{
			get
			{
				return _owner.LinkedObject;
			}
		}


		public OLAPObjectMetaData Owner
		{
			get { return _owner.LinkedObject; }
			set { _owner.LinkedObject = value; }
		}

		//public string Name
		//{
		//    get { return _name; }
		//    set { _name = value; }
		//}

		//public string Description
		//{
		//    get { return _description; }
		//    set { _description = value; }
		//}

		//public string GUID
		//{
		//    get { return _guid; }
		//    set { _guid = value; }
		//}

		public bool IsUnique
		{
			get { return _isUnique; }
			set
			{
                if( !IsPrimaryKey )
                    _isUnique = value;
			}
		}

        public bool IsPrimaryKey
        {
            get { return _isPrimaryKey; }
            set
            {
                _isPrimaryKey = value;
                if( _isPrimaryKey )
                    _isUnique = true;
            }
        }

        public bool IsClustered
        {
            get { return _isClustered; }
            set { _isClustered = value; }
        }

        public DatabaseTable DBTable
        {
            get { return _dbTable; }
            set { _dbTable = value; }
        }

		public DatabaseDataSpace Filegroup
		{
			get { return _filegroup.LinkedObject; }
			set { _filegroup.LinkedObject = value; }
		}

		public string PartitionedField
		{
			get
			{
				return _partitionedField;
			}
			set
			{
				_partitionedField = value;
			}
		}

		public override string ToString()
        {
            if( Owner != null )
                return Owner.Name + '.' + Name;
            else
                return Name;
        }

        #region IObjectURIProvider Members
        public ObjectURI CreateURI()
        {
            ObjectURI result;
            result = new ObjectURI( string.Format( "IndexMetaData/{0}", GUID ) );
            return result;
        }
        #endregion

        [SelfValidation]
        public void DoValidate( ValidationResults results )
        {
            if( IsPrimaryKey && !IsUnique )
            {
                string errorMeassage = "Primary key index must be unique";
                ValidationResult vr = new ValidationResult( errorMeassage, this, "IsUnique", null, null );
                results.AddResult( vr );
            }

			if( Filegroup == null )
			{
				string errorMeassage = "Index Filegroup must be set";
				ValidationResult vr = new ValidationResult( errorMeassage, this, "Filegroup", null, null );
				results.AddResult( vr );
			}

			if( Filegroup is PartitionScheme && String.IsNullOrEmpty( PartitionedField ) )
			{
				string errorMeassage = "Index partion scheme field must be set";
				ValidationResult vr = new ValidationResult( errorMeassage, this, "PartitionedField", null, null );
				results.AddResult( vr );
			}

            if( Fields.Count == 0 )
            {
                string errorMeassage = "Index must contain indexed fields";
                ValidationResult vr = new ValidationResult( errorMeassage, this, "Fields", null, null );
                results.AddResult( vr );
            }
            else
            {
				if( Fields.FirstOrDefault( f => !f.IsIncluded ) == null )
				{
					string errorMeassage = "Index must contain key fields";
					ValidationResult vr = new ValidationResult( errorMeassage, this, "Fields", null, null );
					results.AddResult( vr );
				}

                foreach(BaseIndexFieldMetaData ixf in Fields )
                {
					if( IsClustered && ixf is IndexFieldMetaData &&
						((IndexFieldMetaData)ixf).Field.IsNull )
					{
						FieldMetaData fld =((IndexFieldMetaData)ixf).Field;
						string errorMeassage = string.Format( "Field {0}.{1} can't be nullable because it is part of clustered index {2}.{3}", fld.Owner.Name, fld.Name, Owner.Name, Name );
						ValidationResult vr = new ValidationResult( errorMeassage, this, "Fields", null, null );
						results.AddResult( vr );
					}

                    ValidationResults validResults = Validation.Validate<BaseIndexFieldMetaData>( ixf );
                    if( !validResults.IsValid )
                    {
                        foreach( ValidationResult vr in validResults )
                        {
                            results.AddResult( vr );
                        }
                    }
                }
            }
        }

		public ICollection<BaseIndexFieldMetaData> GetFields()
		{
			List<BaseIndexFieldMetaData> result = new List<BaseIndexFieldMetaData>( _fields.Count );
			foreach( BaseIndexFieldMetaData data in _fields )
			{
				result.Add( data );
			}
			return result;
		}

		internal IndexMetaData Clone()
		{
			IndexMetaData result = (IndexMetaData)MemberwiseClone();
			result.GUID = Guid.NewGuid().ToString();
			CSPropMgr.Instance.setupObject( result, true );
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
