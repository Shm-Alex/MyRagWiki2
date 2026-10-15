using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;

namespace Ksema.Framework.Database
{
    [Flags]
    public enum DbConnectionScopeOptions { Required = 1, RequiresNew = 2, DoNotEnlist = 4, Default = Required }

    //TODO : переделать на производный от SimpleScopeBase
    public class DbConnectionScope : IDisposable
    {
        DbConnectionScopeOptions _options = DbConnectionScopeOptions.Default;
        IDbConnectionHolder _holder;
        bool _closeConn = false;
        bool _disposed = false;

        [ThreadStatic]
        static DbConnectionScope s_currentScope;
        DbConnectionScope _priorScope;
        Thread _scopeThread;

        public static DbConnectionScope Current
        {
            get { return s_currentScope; }
        }

        public static DbConnectionScope Create(IDbConnectionProvider prv, DbConnectionScopeOptions options)
        {
            return new DbConnectionScope(prv, options);
        }

        public static DbConnectionScope Create(IDbConnectionProvider prv)
        {
            return new DbConnectionScope(prv, DbConnectionScopeOptions.Default);
        }


        internal DbConnectionScope(IDbConnectionProvider aProvider, DbConnectionScopeOptions options)
        {
            if (aProvider == null)
                throw new ArgumentNullException("aProvider");
            _options = options;
            IDbConnectionHolder holder;

            if (s_currentScope == null)
                holder = CreateConnectionHolder();
            else
            {
                if (!Object.Equals(s_currentScope.GetType(), this.GetType()))
                    throw new NotSupportedException("Outer connection scope must have the same type");

                if (HasOption(DbConnectionScopeOptions.RequiresNew))
                    holder = CreateConnectionHolder();
                else
                    holder = s_currentScope._holder;
            }

            if (holder.Connection == null)
            {
                holder.OpenConnection(aProvider, !HasOption(DbConnectionScopeOptions.DoNotEnlist));
                _closeConn = true;
            }
            else
                _closeConn = false;

            _holder = holder;
            _priorScope = s_currentScope;
            s_currentScope = this;
            _scopeThread = Thread.CurrentThread;

        }

        private bool HasOption(DbConnectionScopeOptions option)
        {
            return (_options & option) == option;
        }

        internal virtual IDbConnectionHolder CreateConnectionHolder()
        {
            return new SimpleConnectionHolder();
        }

        private void CheckDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("DbConnectionScope");
            }
        }

        private bool IsDisposed
        {
            get 
            {
                return _disposed;
            }
        }

        void IDisposable.Dispose()
        {
            if (!IsDisposed)
            {
                if (_scopeThread != Thread.CurrentThread)
                    throw new InvalidOperationException("Cannot dispose on other thread");

                _disposed = true;
                if (s_currentScope == this)
                {
                    // In case the user called dispose out of order, skip up the chain until we find
                    //  an undisposed scope.
                    DbConnectionScope prior = _priorScope;
                    while (null != prior && prior.IsDisposed)
                    {
                        prior = prior._priorScope;
                    }
                    s_currentScope = prior;
                }

                if (_closeConn)
                    _holder.CloseConnection();
            }
        }


        public IDbConnection Connection
        {
            get
            {
                CheckDisposed();
                return _holder.Connection;
            }
        }

        public IDbCommand CreateCommand(string sql)
        {
            return CreateCommand(sql, null);
        }

        public IDbCommand CreateCommand(string sql, IEnumerable<DbParameter> paramList)
        {
            CheckDisposed();

            IDbCommand cmd = Connection.CreateCommand();
            cmd.CommandTimeout = 0;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = sql;
            BindParameters(cmd, paramList);

            return cmd;
        }

        void BindParameters(IDbCommand cmd, IEnumerable<DbParameter> aParameters)
        {
            if (aParameters != null)
            {
                foreach (DbParameter par in aParameters)
                {
                    DbParameter parCopy = (DbParameter)((ICloneable)par).Clone();
                    cmd.Parameters.Add(parCopy);
                }
            }
        }

    }

    class SimpleConnectionHolder : IDbConnectionHolder
    {
        private IDbConnection _Connection;

        public IDbConnection Connection
        {
            get { return _Connection; }
        }

        public void OpenConnection(IDbConnectionProvider provider, bool enlist)
        {
            IDbConnection conn = enlist ? 
                provider.CreateConnection() : 
                provider.CreateLocalConnection();

            if (conn.State != ConnectionState.Open)
                conn.Open();

			SqlStatisticManager.Enable(conn);
			provider.SetupConnection(conn);

            _Connection = conn;
        }

        public void CloseConnection()
        {
            if (_Connection != null)
            {
                IDbConnection conn = _Connection;
                _Connection = null;
				SqlStatisticManager.ProcessStatistics(conn);
				conn.Close();
            }
        }
    }

}
