using System;
using System.Linq;
using System.Collections.Generic;
using Ksema.Framework;
using Ksema.ObjectModel;

namespace Shart.ModelDef
{
    [Flags()]
    public enum TriggerActivationStatement
    {
		Empty = 0x0,
        Insert = 0x1,
        Update = 0x2,
        Delete = 0x4
    }

	[Serializable]
	public class SQLTemplateMetaData : BaseMetaData, IObjectURIProvider
    {
        private string _sql;
        private string _error;
        private SQLTemplateType _type = SQLTemplateType.ModelTrigger;
        private TriggerActivationStatement _activationStatement = TriggerActivationStatement.Insert;
        private bool _insteadOf = false;
        private DatabaseTable _dbTable = DatabaseTable.ViewTable;

        [CSAttribute( "Sql" )]
        public string Sql
        {
            get { return _sql; }
            set { _sql = value; }
        }

        [CSAttribute( "Error" )]
        public string Error
        {
            get { return _error; }
            set { _error = value; }
        }

        [CSAttribute("TemplateType")]
        public SQLTemplateType TemplateType
        {
            get { return _type; }
            set { _type = value; }
        }

        [CSAttribute( "InsteadOf" )]
        public bool InsteadOf
        {
            get { return _insteadOf; }
            set { _insteadOf = value; }
        }

        [CSAttribute( "ActivationStatement" )]
        public TriggerActivationStatement ActivationStatement
        {
            get { return _activationStatement; }
            set { _activationStatement = value; }
        }

        [CSAttribute( "DBTable" )]
        public DatabaseTable DBTable
        {
            get { return _dbTable; }
            set { _dbTable = value; }
        }

        [CSReference("AttachedObjects", false, "TriggerTemplates")]
        CSMultiLink<SQLTemplateMetaData, OLAPObjectMetaData> _attachedObjects;

        public IList<OLAPObjectMetaData> AttachedObjects
        {
            get { return _attachedObjects; }
        }

        public SQLTemplateMetaData()
        {
            CSPropMgr.Instance.setupObject(this, false);
        }

        public ObjectURI CreateURI()
        {
            ObjectURI result = new ObjectURI( "sqltemplate/" + GUID );
            return result;
        }

		public OLAPObjectMetaData TryGetAttachedObjectByGuid( string guid )
		{
			return _attachedObjects.FirstOrDefault( obj => obj.GUID == guid );
		}

		public override string ToString()
		{
			return Name;
		}
    }
}
