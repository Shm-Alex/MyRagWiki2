using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Framework.Utils;

namespace Ksema.Framework.Database
{
    public interface IKeyManager<T>
    {
        T CreateKey(string className);
        T CreateKeyRange(string className, int count);
    }

    /// <summary>
    /// Manages key information in a table: LastID (LastID int not null, Name char(20) PK)
    /// Locks are managed at a database level
    /// </summary>
    public class LastIDKeyManager : IKeyManager<long>
    {
        private const int MAX_NAME_LEN = 100;
        private const string COUNT_PAR = "count";
        private const string NAME_PAR = "className";
        private const string RESULT_PAR = "result";

//        private const string SQL_SP = "EXEC @result = dbo.CreateKeyRange @className, @count";
        private const string SQL_SP = "CreateKeyRange";

        private IDatabase _db;

    	private IDictionary<string, int> _idIncrementers;
    	
		public IDictionary<string, int> IdIncrementers
    	{
    		get { return _idIncrementers; }
    		set { _idIncrementers = value; }
    	}

		public LastIDKeyManager( IDatabase aDb) : this( aDb, null )
		{
			
		}

		public LastIDKeyManager( IDatabase aDb, IDictionary<string, int> idIncrementers )
        {
            if (aDb == null)
                throw new ArgumentNullException("aDb");
            _db = aDb;
			_idIncrementers = idIncrementers;
        }

        public long CreateKey(string className)
        {
            return CreateKeyRange(className, 1);
        }

	    public static object _lockCreateKeyRange = new object();

		public long CreateKeyRange(string className, int count)
        {
            long result;
            if (string.IsNullOrEmpty(className))
                throw new ArgumentNullException("className");
            if (count <= 0)
                throw new ArgumentOutOfRangeException("count", count, "Range length must be positive");

			lock (_lockCreateKeyRange)
			{
				using (IDbConnection conn = _db.CreateLocalConnection())
				{
					conn.Open();
					IDbTransaction tran = conn.BeginTransaction(IsolationLevel.Serializable);
					try
					{
						IDbCommand cmd = CreateCommand(conn, SQL_SP, className, count);
						cmd.CommandType = CommandType.StoredProcedure;

						DbParameter resultPar = _db.CreateTypedParameter(RESULT_PAR, DbType.Int64, (long) 0);
						resultPar.Direction = ParameterDirection.InputOutput;
						cmd.Parameters.Add(resultPar);

						cmd.Transaction = tran;
						cmd.CommandTimeout = 0;
						cmd.ExecuteNonQuery();

						result = Convert.ToInt64(resultPar.Value);

						tran.Commit();
					}
					catch
					{
						tran.Rollback();
						throw;
					}
				}

				result += GetIncerement(className);
			}

			return result;
        }

		private int GetIncerement( string className )
		{
			int result;
			if( _idIncrementers != null && _idIncrementers.TryGetValue( className, out result ) )
			{
			}
			else
			{
				result = 0;
			}
			return result;
		}

        private IDbCommand CreateCommand(IDbConnection conn, string sql, string className, int count)
        {
            IDbCommand cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.Text;

            DbParameter par = _db.CreateTypedParameter(NAME_PAR, DbType.String, MAX_NAME_LEN, className);
            cmd.Parameters.Add(par);
            sql = sql.Replace("@" + NAME_PAR, par.ParameterName);

            if (count > 0)
            {
                par = _db.CreateTypedParameter(COUNT_PAR, DbType.Int32, count);
                cmd.Parameters.Add(par);
                sql = sql.Replace("@" + COUNT_PAR, par.ParameterName);
            }

            cmd.CommandText = sql;
            return cmd;
        }
    }
}
