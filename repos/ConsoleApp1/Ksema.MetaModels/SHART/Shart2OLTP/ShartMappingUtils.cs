using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text.RegularExpressions;
using Framework.Collections;
using Ksema.ObjectModel.ContextModel;
using Shart.ModelDef;

namespace Ksema.MetaModels.SHART.Shart2OLTP
{
	public static class ShartMappingUtils
	{
        public const string TokenIDName = "TokenID";
        public const string DimensionTablePrefix = "Dim_";
        public const string CubeTablePrefix = "Cell_";
        public const string ViewPostfix = "_View";
        public const string ParentIDName = "ParentID";
        public const string LevelName = "CurrLevel";
        public const string DateCreateName = "DateCreate";
        public const string DateChangeName = "DateChange";
        public const string DateCloseName = "DateClose";
        public const string VersionIDName = "VersionID";
    	public const string ExtCodeName = "ExtCode"; //Ну поспорьте со мной (gribkov)
    	public const string DataSymbolName = "DataSymbol";
	    public const string c_extraParam = "ExtraParam";
		public const string CodeName = "Code";
        public const string Name = "Name";
        public const string GuidName = "Guid";
        public const string ParentLevelPrefixName = "ParentLevel";
        public const string Parent1_Code = "Parent1_Code";
        public const string UserIDCreateName = "UserIDCreate";
        public const string UserIDChangeName = "UserIDChange";
        /// <summary>Код _ROOT_</summary>
		public const string RootCode = "_ROOT_";

		public const string CompositeModelName = "_COMPOSITE_";

		/// <summary>Код _REFROOT_</summary>
		public const string RefRootCode = "_REFROOT_";
		/// <summary>Код _REFNODE_</summary>
		public const string RefNodeCode = "_REFNODE_";
		
		public static string[] RootRefs = new[] {RootCode, RefNodeCode, RefRootCode};

		public static string RootRefsStr
		{
			get
			{
				return string.Join(", ", ShartMappingUtils.RootRefs.Select(x => "'" + x + "'"));
			}
		}

		public static string ParentCode(int level)
		{
			return string.Format("Parent{0}_Code", level);
		}

		public static bool IsEmail(string u)
		{
			//if ( Regex.Match(u, "^[A-Z0-9._%+-]+@[A-Z0-9]+.+([A-Za-z]{2,5})").Success )
			if ( u.Contains("@")  )
				return true;
			return false;
		}

		public static readonly DateTime DefaultCloseDate = new DateTime(2100, 1, 1);
		public static readonly DateTime DefaultCreateDate = new DateTime(1900, 1, 1);
		public static readonly DateTime DefaultCreateDate2 = new DateTime(1990, 1, 1);

        public static string GetDimMainTableName(string dimTablePostfix)
        {
            return DimensionTablePrefix + dimTablePostfix;
        }

        public static string GetDimMainTableName(IOLAPObjectMetaData dim)
        {
            return DimensionTablePrefix + dim.TablePostfix;
        }

        public static string GetDimViewTableName(IOLAPObjectMetaData dim)
        {
            return GetDimViewTableName( dim.TablePostfix );
        }

        public static string GetDimViewTableName( string dimTablePostfix )
        {
            return DimensionTablePrefix + dimTablePostfix + ViewPostfix;
        }

        public static string GetCubeViewTableName(IOLAPObjectMetaData cube)
        {
            return CubeTablePrefix + cube.TablePostfix + ViewPostfix;
        }
        public static string GetAnalysisColumnName(string fieldName, int level)
        {
            if (string.IsNullOrEmpty(fieldName))
                throw new ArgumentNullException("fieldName");

            return "Parent" + level + "_" + fieldName;
        }
        public static string GetParentColumnName(int level)
        {
            return ParentLevelPrefixName + level;
        }

        public static string GetDimIDName(IOLAPObjectMetaData dim)
        {
            return GetDimIDName( dim.TablePostfix );
        }

        public static string GetDimIDName( string dimTablePostfix )
        {
            return dimTablePostfix + "ID";
        }

        public static string GetCubeIDName(IOLAPObjectMetaData cube)
        {
            return "CellID";
        }

        /// <summary>
        /// Возвращает true если код системный.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static bool IsSystemCode( string code)
        {
            return String.Compare( code, RootCode, StringComparison.OrdinalIgnoreCase ) == 0
                   || String.Compare( code, RefRootCode, StringComparison.OrdinalIgnoreCase ) == 0
                   || String.Compare( code, RefNodeCode, StringComparison.OrdinalIgnoreCase ) == 0;
        }

        public static bool IsRootCode( string code )
        {
            return String.Compare(code, RootCode, StringComparison.OrdinalIgnoreCase) == 0;
        }

        /// <summary>
        /// Возвращает true если код _REFROOT_ или _REFNODE_.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static bool IsReferenceCode( string code )
        {
            return
                String.Compare(code, RefRootCode, StringComparison.OrdinalIgnoreCase) == 0 ||
                String.Compare(code, RefNodeCode, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public static string[] GetSystemCodes()
        {
            return new[] {RootCode, RefRootCode, RefNodeCode };
        }

        public static string GetReferenceViewName(string refName)
        {
            return refName + "_View";
        }
		public const string c_DynPrefix = "Dyn";
		public static string GetDimensionName(string name)//для Динамики
		{
			if (IsDynamicDimension(name))
				name = name.Substring(c_DynPrefix.Length);
			return name;
		}

		public static bool IsDynamicDimension(string name)
		{
			return name.StartsWith(c_DynPrefix);
		}

		public static string GetDimWithId (string dimId)
		{
			if (string.IsNullOrEmpty(dimId))
				return "";
			if ( dimId.Length>=2 &&  dimId.Substring(dimId.Length - 2).ToUpper() == "ID")
				return dimId;
			if (dimId.ToLower().EndsWith("code"))
				return dimId;
			return dimId + "Id";
		}

		public static Dictionary<string, KeyValuePair<string,string>> ConvertSQL2Simple = new Dictionary<string, KeyValuePair<string, string>>
		{
			{"yyyy.mm.dd" , new KeyValuePair<string,string>("102","%[0-9][0-9][0-9][0-9]%.[0-9]%.[0-9]%")},
			{"dd.mm.yyyy" , new KeyValuePair<string,string>("104","%[0-9]%.[0-9]%.[0-9][0-9][0-9][0-9]%")},
			{"dd-mm-yyyy" , new KeyValuePair<string,string>("105","%[0-9]%-[0-9]%-[0-9][0-9][0-9][0-9]%")},
			{"dd mm yyyy" , new KeyValuePair<string,string>("106","%[0-9]% [0-9]% [0-9][0-9][0-9][0-9]%")}
		};

		public static string GetConvertType(string convertType)
		{
			convertType = convertType.ToLower();

			if ( ConvertSQL2Simple.ContainsKey(convertType) )
				return ConvertSQL2Simple[convertType].Value;
			return "%";
		}
	    public const string RemoveTempTablesKey = "RemoveTempTables";
	    public const string RemoveTempTablesValue = "RemoveTempTablesValue";
    }

	public class CmpShartKey
	{
		public string Key { get; private set; }

		public string KeyCmp
		{
			get
			{
				return GetKey(Key);
			}
		}

		public static string GetKey(string key)
		{
			if (key.EndsWith("Id", StringComparison.InvariantCultureIgnoreCase))
				key = key.Substring(0, key.Length - 2);
			return key.ToLower();
		}
		public CmpShartKey(string key)
		{
			Key = key;
		}

		public static bool operator == (CmpShartKey prm1, CmpShartKey prm2)
		{
			return Cmp(prm1, prm2);
		}

		public static bool operator != (CmpShartKey prm1, CmpShartKey prm2)
		{
			return !Cmp(prm1, prm2);
		}
		public override bool Equals (object obj)
		{
			if (obj is CmpShartKey)
				return Cmp(obj as CmpShartKey, this);
			return false;
		}

		static bool Cmp(CmpShartKey t1, CmpShartKey t2)
		{
			return t1.KeyCmp == t2.KeyCmp;
		}

		public override int GetHashCode ()
		{
			return KeyCmp.GetHashCode() ;
		}
		
	}


	public static class CodeChecker
	{
		static TypedSet<string> _excludedCodes;
		static CodeChecker ()
		{
			_excludedCodes = new TypedSet<string>(ShartMappingUtils.GetSystemCodes());
		}

		private static string[] _mainCode =
		{
			"BKFOR", "BKKB", "BKLO", "BKMFO", "BKMGO", "BKNB", "BKNO", "BKSOZ", "BKTMP", "BKVO", "DYNAMIC", "INSUR", "IVS",
			"NKFO", "SNNO"
		};

		public static bool IsExcluded (string dim, string code)
		{
			if (dim != "Bank")
				return false;
			if (_mainCode.Contains(code))
				return true;
			return _excludedCodes.Contains(code);
		}
	}
}
