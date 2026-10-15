using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Ksema.MetaModels.OLTP;
using Framework.Utils;
using System.Text;
using Ksema.Framework.Utils;

namespace Ksema.Framework.Database
{
    public class SqlServerDatabase : IDatabase
    {
        string _connString;


        public SqlServerDatabase(string aConnString)
        {
            _connString = aConnString;
        }

        public string ShortDbName
        {
            get
            {
                return DBUtils.GetKeyValue("Initial Catalog", _connString);
            }
        }

    	public string GetDbName
    	{
    		get
    		{
				return DBUtils.GetKeyValue("Data Source", _connString) + "\\" + DBUtils.GetKeyValue("Initial Catalog", _connString);
    		}
    	}

        public IDbDataAdapter CreateDataAdapter()
        {
            return new SqlDataAdapter();
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(DBUtils.RemoveKeyValue("provider", _connString));
        }


        public IDbConnection CreateLocalConnection()
        {
            return CreateLocalConnection(true);
        }


        public IDbConnection CreateLocalConnection(bool pooling)
        {
            string connStr = DBUtils.RemoveKeyValue("provider", _connString)+ ";Enlist=false;";
            if (!pooling)
                connStr += ";Pooling=false";
            return new SqlConnection(connStr);
        }

        public void SetupConnection(IDbConnection conn)
        {
        	SetContextUser(conn);

        	// Болик (case 25773): Оставляю для истории, сейчас настройки перенесены в обновление моделей - задолбали здесь.

			//IDbCommand cmd = conn.CreateCommand();
			//cmd.CommandType = CommandType.Text;

			//cmd.CommandText = "SET ARITHABORT ON; SET XACT_ABORT ON;";
			//cmd.ExecuteNonQuery();

			//cmd = conn.CreateCommand();
			//cmd.CommandText = "DBCC USEROPTIONS";
			//cmd.CommandType = CommandType.Text;
			//using (IDataReader rdr = cmd.ExecuteReader())
			//{
			//    while (rdr.Read())
			//    {
			//        for (int i = 0; i < rdr.FieldCount; i++)
			//        {
			//            Trace.Write(rdr[i]);
			//            Trace.Write(";  ");
			//        }
			//        Trace.WriteLine("");
			//    }
			//}
        }

		private static bool? _traceHistory = null;
    	public static bool TraceHistory
    	{
    		get
    		{
				if (!_traceHistory.HasValue)
				{
					_traceHistory = ConfigurationUtils.IsAppSettingOn("traceHistory", false);
				}
				return _traceHistory.Value;
    		}
    	}

		private void SetContextUser(IDbConnection conn)
		{
			if (TraceHistory)
			{
				string userName = IdentityUtils.GetShortIdentityName();
				IDbCommand cmd = conn.CreateCommand();
				cmd.CommandType = CommandType.StoredProcedure;
				DbParameter par = CreateTypedParameter("userName", DbType.String, userName);
				cmd.Parameters.Add(par);
				cmd.CommandText = "sp_SetContextUser";
				cmd.ExecuteNonQuery();
			}
		}

    	public string GetNativeNameFromDbType(DbType type)
        {
            return GetDbTypeNativeName( type );
        }

        public static string GetDbTypeNativeName(DbType type)
        {
            string name;
            switch (type)
            {
                case DbType.Boolean:
                    name = "bit";
                    break;
                case DbType.AnsiString:
                    name = "varchar";
                    break;
				case DbType.String:
					name = "nvarchar(max)"; // в MS SQL 2005 nvarchar(max) заменяет ntext
					break;
                case DbType.Date:
                case DbType.DateTime:
                    name = "datetime";
                    break;
                case DbType.Decimal:
                    name = "decimal";
                    break;
                case DbType.Double:
                    name = "float";
                    break;
                case DbType.Int32:
                    name = "int";
                    break;
                case DbType.Int64:
                    name = "bigint";
                    break;
                case DbType.Binary:
                    name = "varbinary";
                    break;
				case DbType.Object:
					name = "varbinary(max)";
					break;
				case DbType.Guid:
					name = "uniqueidentifier";
					break;
				default:
                    throw new NotSupportedException("DbType not supported: " + type);
            }

            return name;
        }

		public static string GetDbTypeDefaultValue( DbType type )
		{
			string result;
			switch( type )
			{
				case DbType.Boolean:
					result = "0";
					break;
				case DbType.AnsiString:
					result = "''";
					break;
				case DbType.String:
					result = "''"; // в MS SQL 2005 nvarchar(max) заменяет ntext
					break;
				case DbType.Date:
				case DbType.DateTime:
					result = "'19000101'";
					break;
				case DbType.Decimal:
					result = "0";
					break;
				case DbType.Double:
					result = "0";
					break;
				case DbType.Int32:
					result = "0";
					break;
				case DbType.Int64:
					result = "0";
					break;
				//case DbType.Binary:
				//    result = "varbinary";
				//    break;
				case DbType.Guid:
					result = "'00000000-0000-0000-0000-000000000000'";
					break;
				default:
					throw new NotSupportedException( "DbType not supported: " + type );
			}

			return result;
		}

        public DbType GetDbTypeFromNativeName(string nativeDbType)
        {
            return GetDbType( nativeDbType );
        }

        public static DbType GetDbType(string nativeDbType)
        {
            if (string.IsNullOrEmpty(nativeDbType))
                throw new ArgumentNullException("nativeDbType");

            DbType type;
            nativeDbType = nativeDbType.Trim().ToLower();
            switch (nativeDbType)
            {
				case "uniqueidentifier":
					type = DbType.Guid;
					break;
                case "bigint":
                    type = DbType.Int64;
                    break;
                case "bit":
                    type = DbType.Boolean;
                    break;
                case "int":
                    type = DbType.Int32;
                    break;
                case "decimal":
                    type = DbType.Decimal;
                    break;
                case "float":
                    type = DbType.Double;
                    break;
                case "datetime":
                    type = DbType.DateTime;
                    break;
                case "varchar":
                    type = DbType.String;
                    break;
                default:
                    throw new NotSupportedException("Type not supported: " + nativeDbType);
            }
            return type;
        }

        private static SqlDbType GetSqlDbType(DbType type)
        {
            SqlDbType sqlType;
            switch (type)
            {
				case DbType.Xml:
					sqlType = SqlDbType.Xml;
					break;
				case DbType.Object:
					sqlType = SqlDbType.VarBinary;
					break;
                case DbType.Boolean:
                    sqlType = SqlDbType.Bit;
                    break;
				case DbType.String:
				case DbType.AnsiString:
                    sqlType = SqlDbType.VarChar;
                    break;
				case DbType.DateTime:
                    sqlType = SqlDbType.DateTime;
                    break;
                case DbType.Decimal:
                    sqlType = SqlDbType.Decimal;
                    break;
                case DbType.Double:
                    sqlType = SqlDbType.Float;
                    break;
                case DbType.Int32:
                    sqlType = SqlDbType.Int;
                    break;
                case DbType.Int64:
                    sqlType = SqlDbType.BigInt;
                    break;
				case DbType.Guid:
					sqlType = SqlDbType.UniqueIdentifier;
					break;
				default:
                    throw new NotSupportedException("DbType not supported: " + type);
            }
            return sqlType;
        }

        public DbParameter CreateTypedParameter(string parName, DbType type, int size, object value)
        {
            if (string.IsNullOrEmpty(parName))
                throw new ArgumentNullException("parName");
            parName = AddPrefix(parName);
            SqlDbType sqlType = GetSqlDbType(type);
            SqlParameter par = new SqlParameter(parName, sqlType);
			if (size > 0)
				par.Size = size;
            if (value == null)
            {
                par.IsNullable = true;
                par.Value = DBNull.Value;
            }
            else
            {
                par.Value = value;
            }
            return par;
        }

        public DbParameter CreateParameter(string parName,  object value)
        {
            if (string.IsNullOrEmpty(parName))
                throw new ArgumentNullException("parName");
            parName = AddPrefix(parName);
            
            SqlParameter par = new SqlParameter();
            par.ParameterName = parName;
            if (value == null)
            {
                par.IsNullable = true;
                par.Value = DBNull.Value;
            }
            else
            {
                par.Value = value;
            }
            return par;
        }

        public DbParameter CreateTypedParameter(string parName, DbType type, object value)
        {
            return CreateTypedParameter(parName, type, 0, value);
        }

        private string AddPrefix(string parName)
        {
            if (parName[0] != ParameterPrefix)
                parName = ParameterPrefix + parName;
            return parName;
        }

        public char ParameterPrefix
        {
            get { return '@'; }
        }

        public string CreateTableQuery(Table table, TableScope scope, ref string tableName)
        {
            tableName = GetTableName(scope, tableName);

            StringBuilder result = new StringBuilder();

			if(scope == TableScope.TableVar)
				result.Append("DECLARE ").Append(tableName).Append(" TABLE (");
			else
				result.Append("CREATE TABLE ").Append(tableName).Append(" (");

            GetColumnDefinitions(table, result);

			result.Append(")");
            return result.ToString();
        }

    	private static string GetTableName(TableScope scope, string tableName)
    	{
    		switch (scope)
    		{
    			case TableScope.TableVar:
    				if (!tableName.StartsWith("@"))
    					tableName = "@" + tableName;
    				break;
    			case TableScope.Permanent:
    				break;
    			case TableScope.LocalTemporary:
    				if (!tableName.StartsWith("#"))
    					tableName = "#" + tableName;
    				break;
    			case TableScope.GlobalTemporary:
    				if (!tableName.StartsWith("##"))
    					tableName = "##" + tableName;
    				break;
    			default:
    				throw new ArgumentOutOfRangeException("scope");
    		}
    		return tableName;
    	}

    	private void GetColumnDefinitions(Table table, StringBuilder result)
    	{
    		bool comma = false;
    		foreach (Column column in table.Columns)
    		{
    			if (comma)
    				result.Append(',');
    			result.Append(column.Name).Append(" ")
					.Append(this.GetNativeNameFromDbType(column.DataType.DBType))
					.Append((column.DataType.IsVariableLength ? "(" + column.Size + ")" : ""))
					.Append((column.IsAutoincrement ? " IDENTITY (1, 1) " : ""))
					.Append((column.IsNullable ? " NULL" : "  NOT NULL"));
    			comma = true;
    		}
    	}

		public string DropTableQuery(string tableName)
		{
			TableScope scope;
			if (tableName.StartsWith("@"))
				scope = TableScope.TableVar;
			else if (tableName.StartsWith("##"))
				scope = TableScope.GlobalTemporary;
			else if (tableName.StartsWith("#"))
				scope = TableScope.LocalTemporary;
			else
				scope = TableScope.Permanent;

			return DropTableQuery(tableName, scope);
		}

    	public string DropTableQuery(string tableName,TableScope ts)
        {
            switch(ts)
            {
                case TableScope.GlobalTemporary:
                case TableScope.LocalTemporary:
                    return string.Format("if object_id(N'tempdb..{0}') is not null " +
                                            " drop table {0}", tableName);

				case TableScope.TableVar:
					return "";
                default:
                    return string.Format("if exists (select * from dbo.sysobjects where id = object_id(N'{0}')" +
                                         "and OBJECTPROPERTY(id, N'IsUserTable') = 1) drop table {0}", tableName);
                    
            }
            //return "drop table " + tableName;
        }

        void DoBulkCopyImpl(DataTable table, SqlConnection conn, SqlTransaction tran)
        {
            using (SqlBulkCopy sqlBCP = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran))
            {
                sqlBCP.BulkCopyTimeout = TimeoutHelper.GetSeconds("BulkCopy"); 
                sqlBCP.DestinationTableName = table.TableName;
                SetColumnMappings(sqlBCP, table.Columns);
                sqlBCP.WriteToServer(table);
            }
        }

        void SetColumnMappings(SqlBulkCopy aBcp, DataColumnCollection aColumns)
        {
            foreach (DataColumn col in aColumns)
                aBcp.ColumnMappings.Add(col.ColumnName, col.ColumnName);
        }

        public void BulkCopy(DataTable table, IDbTransaction tran)
        {
            DoBulkCopyImpl(table, (SqlConnection) tran.Connection, (SqlTransaction) tran);
        }

        public void BulkCopy(DataTable table, IDbConnection conn)
        {
            DoBulkCopyImpl(table, (SqlConnection)conn, null);
        }

    }
}
