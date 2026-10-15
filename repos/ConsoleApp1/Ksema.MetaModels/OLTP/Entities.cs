using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Framework.Collections;
using Framework.Utils;
using Ksema.ObjectModel;

namespace Ksema.MetaModels.OLTP
{
    public interface IOLTPSchema
    {
        IList<Table> Tables { get; }

        IList<DataType> Types { get; }
    }

    public enum KeySourceType { Self, FromLinks };

    public class OLTPSchema : IOLTPSchema
    {
        [CSReference("tables", true, "owner")]
        private IList<Table> tables;
        [CSReference("types", true, "owner")]
        private IList<DataType> types;
        public OLTPSchema()
        {
            tables = new SingleOwnerItemCollection<OLTPSchema, Table>(this, "tables", "owner");
            types = new SingleOwnerItemCollection<OLTPSchema, DataType>(this, "types", "owner");
        }

        public IList<Table> Tables
        {
            get { return tables; }
        }

        public IList<DataType> Types
        {
            get { return types; }
        }

        public Table GetTable(string tableName)
        {
            foreach (Table table in Tables)
            {
                if (table.Name == tableName)
                    return table;
            }

            throw new NotFoundException("Table not found: " + tableName);
        }
    }
    public class Table
    {
        [CSReference("owner", false, "tables")]
        private IOLTPSchema owner = null;


        [CSAttribute("querywkeys")] 
        private bool queryWithKeys = false;

        public bool QueryWithKeys
        {
            get
            {
                return queryWithKeys;
            }

            set
            {
                queryWithKeys = value;
            }
        }

        [CSAttribute("name", Required = true)]
        private string name;

        [CSAttribute("schema")]
        private string schema;

        [CSAttribute("alias")]
        private string alias;
        public string Alias
        {
            get { return alias; }
            set { alias = value; }
        }

        [CSAttribute("keySource")]
        private KeySourceType keySrc;
        public KeySourceType KeySourceType
        {
            get
            {
                return keySrc;
            }

            set
            {
                keySrc = value;
            }

        }

        [CSReference("columns", true, "owner")]
        private IList<Column> columns;

        [CSReference("fKeys", true, null)] private IList<FK> fKeys;

        public Table()
        {
            columns = new SingleOwnerItemCollection<Table, Column>(this, "columns", "owner");
            fKeys =  new SingleOwnerItemCollection<Table,FK>(this,"fKeys","owner");
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string Schema
        {
            get { return schema; }
            set { schema = value; }
        }
        public string FullName
        {
            get
            {
                return String.IsNullOrEmpty(schema) ? name : (schema + "." + name);
            }
        }
        public IList<Column> Columns
        {
            get { return columns; }
        }
        public IList<FK> FKeys
        {
            get { return fKeys; }
        }

        public IOLTPSchema Owner
        {
            get { return owner; }
        }

        public IList<Column> GetPKColumns()
        {
            List<Column> pkCols = new List<Column>(Columns.Count);
            foreach (Column col in Columns)
            {
                if (col.IsPK)
                    pkCols.Add(col);
            }

            return pkCols;
        }

        public Column GetColumn(string aName)
        {
            return GetColumn(aName, true);
        }

		public Column GetColumn(string aName, bool throwNotFound)
		{
            foreach (Column col in Columns)
                if (col.Name == aName)
                    return col;
            if (throwNotFound)
                throw new NotFoundException("column not found: " + aName);
            else
                return null;
        }

        public FK GetFK(string aID)
        {
            foreach (FK fk in FKeys)
                if (fk.Id == aID)
                    return fk;
            throw new NotFoundException("FK not found: " + aID);
        }

        public DataTable CreateDataTable()
        {
            DataTable table = new DataTable(this.Name);
            foreach (Column col in this.Columns)
            {
				if (string.IsNullOrEmpty(col.Formula))
				{
					DataColumn dataCol = table.Columns.Add(col.Name, col.DataType.SystemType);
					//dataCol.MaxLength = col.Size;
					dataCol.AllowDBNull = col.IsNullable;
				}
            }

            return table;
        }
    }
    public class Column
    {
        [CSAttribute("name", Required = true)]
        private string name;
        [CSReference("dataType")]
        private DataType dataType;
        [CSAttribute("isNullable")]
        private bool isNullable = true;
        [CSAttribute("isPK")]
        private bool isPK = false;
        [CSReference("owner", false, "columns")]
        private Table owner = null;

        [CSAttribute("size")]
        private int size;

        private bool _IsAutoincrement = false;

        public int Size
        {
            get { return size; }
            set { size = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        public DataType DataType
        {
            get { return dataType; }
            set { dataType = value; }
        }
        public bool IsNullable
        {
            get { return isNullable; }
            set { isNullable = value; }
        }
        public bool IsPK
        {
            get { return isPK; }
            set { isPK = value; }
        }
        public Table Owner
        {
            get { return owner; }
        }

        [CSAttribute("Autoincrement")]
        public bool IsAutoincrement
        {
            get { return _IsAutoincrement; }
            set { _IsAutoincrement = value; }
        }

		[CSAttribute("Formula")]
		public string Formula { get; set; }
    }
    public class DataType
    {
        [CSAttribute("name", Required = true)]
        private string name;
        [CSReference("owner", false, "types")]
        private IOLTPSchema owner = null;

        [CSAttribute("dbType")] private DbType dbType;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public IOLTPSchema Owner
        {
            get { return owner; }
        }

        public DbType DBType
        {
            get { return dbType; }
            set { dbType = value; }
        }

        public Type SystemType
        {
            get 
            {
                switch (dbType)
                {
                    case DbType.AnsiString:
                        return typeof(string);
                    case DbType.Binary:
                        return typeof(byte[]);
                    case DbType.Byte:
                        return typeof(byte);
                    case DbType.Boolean:
                        return typeof(Boolean);
                    case DbType.Currency:
                        return typeof(decimal);
                    case DbType.Date:
                        return typeof(DateTime);                        
                    case DbType.DateTime:
                        return typeof(DateTime);                                                
                    case DbType.Decimal:
                        return typeof(decimal);
                    case DbType.Double:
                        return typeof(double);
                    case DbType.Guid:
                        return typeof(Guid);
                    case DbType.Int16:
                        return typeof(Int16);
                    case DbType.Int32:
                        return typeof(Int32);
                    case DbType.Int64:
                        return typeof(Int64);
                    case DbType.Object:
                        return typeof(object);
                    case DbType.SByte:
                        return typeof(sbyte);
                    case DbType.Single:
                        return typeof(Single);
                    case DbType.String:
                        return typeof(string);
                    case DbType.Time:
                        return typeof(DateTime);
                    case DbType.UInt16:
                        return typeof(UInt16);
                    case DbType.UInt32:
                        return typeof(UInt32);
                    case DbType.UInt64:
                        return typeof(UInt64);
                    case DbType.AnsiStringFixedLength:
                        return typeof(string);
                    case DbType.StringFixedLength:
                        return typeof(string);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public bool IsVariableLength
        {
            get 
            {
                switch (DBType)
                {
                    case DbType.StringFixedLength:
                    case DbType.AnsiString:
                    case DbType.AnsiStringFixedLength:
                        return true;
					case DbType.String:
						return false;
					default:
                        return false;
                }
            }
        }
    }

    public enum Ident_info { True, False, Default }
    public class FK
    {
        [CSAttribute("ID", Required = true)]
        private string id;

        [CSAttribute("Expression")] 
        private string expression;

        public string Expression
        {
            get { return expression; }
            set { expression = value; }
        }


        [CSReference("owner", false, "fKeys")]
        private Table owner = null;
        public Table Owner
        {
            get
            {
                return owner;
            }
        }

        [CSReference("tTable", false)] 
        private Table tTable;

        public Table TargetTable
        {
            get
            {
                return tTable;
            }
            set
            {
                tTable = value;
            }
        }

        [CSReference("fkColumns", true, null)]
        private IList<FKColumn> fkColumns = new List<FKColumn>();

        [CSAttribute("iinfo")]
        private Ident_info ident_info = Ident_info.Default;

        public Ident_info Ident_info
        {
            get
            {
                return ident_info;
            }

            set
            {
                ident_info = value;
            }
        }

        public string Id
        {
            get { return id; }
            set { id = value; }
        }
        public IList<FKColumn> FKColumns
        {
            get { return fkColumns; }
        }

        public bool IsIdentifying
        {
            get
            {
                switch (ident_info)
                {
                    case Ident_info.True:
                        return true;
                    case Ident_info.False:
                        return false;
                    case Ident_info.Default:
                    default:
                        {
                            foreach (FKColumn column in fkColumns)
                            {
                                if (!column.Primary.IsPK)
                                    return false;
                            }
                            return true;
                        }
                }
            }

            set
            {
                if (value)
                    ident_info = Ident_info.True;
                else
                    ident_info = Ident_info.False;
            }
        }
    }
    public class FKColumn
    {
        [CSAttribute("ID", Required = true)]
        private string id;
        [CSReference("primary")]
        private Column primary;
        [CSReference("foreign")]
        private Column foreign;

        public string Id
        {
            get { return id; }
            set { id = value; }
        }
        public Column Primary
        {
            get { return primary; }
            set { primary = value; }
        }
        public Column Foreign
        {
            get { return foreign; }
            set { foreign = value; }
        }
    }
}
