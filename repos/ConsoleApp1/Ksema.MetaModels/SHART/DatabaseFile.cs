using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ksema.ObjectModel;
using Framework.Collections;

namespace Shart.ModelDef
{
/*
	/// <summary>
	/// Файл БД
	/// </summary>
	public class DatabaseFile : BaseMetaData
	{
		[CSReference( "Model", false, "DatabaseFiles" )]
		private CSSingleLink<DatabaseFile, ShartModelMetaData> _model;

		[CSReference( "Filegroup" )]
		private CSSingleLink<DatabaseFile, DatabaseFilegroup> _filegroup = null;

		public DatabaseFile()
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

		public ShartModelMetaData Model
		{
			get
			{
				return _model.LinkedObject;
			}
			set
			{
				_model.LinkedObject = value;
			}
		}

		public override string ToString()
		{
			return Name;
		}
	}

	public class DatabaseFileKey : IIndexer<DatabaseFile, string>
	{
		#region IIndexer Members
		string IIndexer<DatabaseFile, string>.GetIndexKey( DatabaseFile obj )
		{
			return GetKey( obj );
		}

		public IEqualityComparer<string> GetKeyComparer()
		{
			return StringComparer.OrdinalIgnoreCase;
		}

		#endregion

		public static string GetKey( DatabaseFile obj )
		{
			if( obj == null )
				throw new ArgumentNullException( "obj" );
			return obj.Name;
		}
	}
*/

	[Serializable]
	public abstract class DatabaseDataSpace: BaseMetaData
	{
		[CSAttribute( "IsDefault" )]
		private bool _isDefault = false;

		public DatabaseDataSpace()
		{
			CSPropMgr.Instance.setupObject( this, false );
		}

		public bool IsDefault
		{
			get
			{
				return _isDefault;
			}
			set
			{
				_isDefault = value;
			}
		}

	}

	/// <summary>
	/// Файловая группа БД
	/// </summary>
	[Serializable]
	public class DatabaseFilegroup: DatabaseDataSpace
	{
		[CSReference( "Model", false, "DatabaseFilegroups" )]
		private CSSingleLink<DatabaseFilegroup, ShartModelMetaData> _model;

/*
		[CSAttribute( "IsDefault" )]
		private bool _isDefault = false;
*/ 

		[CSAttribute( "DefaultFile" )]
		private string _defaultFile = "";

		public DatabaseFilegroup()
		{
			CSPropMgr.Instance.setupObject( this, false );
		}

		public ShartModelMetaData Model
		{
			get
			{
				return _model.LinkedObject;
			}
			set
			{
				_model.LinkedObject = value;
			}
		}

		public override string ToString()
		{
			return Name;
		}

		public string DefaultFile
		{
			get { return _defaultFile; }
			set { _defaultFile = value; }
		}
	}

	public class DatabaseFilegroupLink
	{
		[CSReference( "Link", false )]
		private CSSingleLink<DatabaseFilegroupLink, DatabaseFilegroup> _link;

		public DatabaseFilegroupLink()
		{
			CSPropMgr.Instance.setupObject( this, false );
		}

		public DatabaseFilegroup Link
		{
			get
			{
				return _link.LinkedObject;
			}
			set
			{
				_link.LinkedObject = value;
			}
		}
	}
}
