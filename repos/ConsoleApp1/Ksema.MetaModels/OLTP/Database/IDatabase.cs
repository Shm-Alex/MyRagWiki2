using System.Data;
using System.Data.Common;
using Ksema.MetaModels.OLTP;

namespace Ksema.Framework.Database
{

    public interface IDbConnectionProvider
    {
        IDbConnection CreateConnection();
        IDbConnection CreateLocalConnection();
        IDbConnection CreateLocalConnection(bool pooling);
        void SetupConnection(IDbConnection conn);
    }

    interface IDbConnectionHolder
    {
        void OpenConnection(IDbConnectionProvider provider, bool enlist);
        void CloseConnection();
        IDbConnection Connection { get;}
    }

    public interface IDatabase : IDbConnectionProvider
    {
		string GetDbName{get;}
        string ShortDbName { get; }

        IDbDataAdapter CreateDataAdapter();
        DbParameter CreateTypedParameter(string parName, DbType type, int size, object value);
        DbParameter CreateTypedParameter(string parName, DbType type, object value);
        DbParameter CreateParameter(string parName,  object value);
        string GetNativeNameFromDbType(DbType type);
        DbType GetDbTypeFromNativeName(string nativeDbType);
        char ParameterPrefix { get;}

        string CreateTableQuery(Table table, TableScope scope, ref string tableName);
        string DropTableQuery(string tableName,TableScope ts);
		string DropTableQuery(string tableName);

        void BulkCopy(DataTable table, IDbTransaction tran);
        void BulkCopy(DataTable table, IDbConnection conn);
    }

    public enum TableScope { Permanent, LocalTemporary, GlobalTemporary, TableVar }
}
