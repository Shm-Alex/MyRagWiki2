using System;
using System.Collections.Generic;
using System.Data;
using Tx=System.Transactions;

namespace Ksema.Framework.Database
{
    class TxConnectionScope : DbConnectionScope
    {
        public TxConnectionScope(IDbConnectionProvider aProvider) : base(aProvider, DbConnectionScopeOptions.Required)
        {
        }

        internal override IDbConnectionHolder CreateConnectionHolder()
        {
            if (Tx.Transaction.Current == null)
                throw new InvalidOperationException("Cannot operate in non-transaction context");

            return new TxConnectionHolder();
        }
    }

    class TxConnectionHolder : IDbConnectionHolder
    {
        private static Dictionary<string, TxConnectionEnlistment> s_localPool = new Dictionary<string, TxConnectionEnlistment>();
        private static object s_lock = new object();

        protected void CheckTransaction()
        {
            if (Tx.Transaction.Current == null)
                throw new InvalidOperationException("Cannot operate in non-transactional context");
        }

        public IDbConnection Connection
        {
            get
            {
                return LookupConnection();
            }
        }

        public void OpenConnection(IDbConnectionProvider provider, bool enlist)
        {
            if (enlist == false)
                throw new NotSupportedException("Enlist must be 'true' for TxConnectionHolder");

            CheckTransaction();

            IDbConnection currentConn = LookupConnection();
            if (currentConn != null)
                throw new InvalidOperationException("Db connection for current transaction is already set!");

            IDbConnection conn = DoCreateAndOpenConnection(provider);
            try
            {
                StoreConnection(conn);
            }
            catch
            {
                conn.Dispose();
                throw;
            }
        }

        IDbConnection DoCreateAndOpenConnection(IDbConnectionProvider provider)
        {
            try
            {
//                Trace.Write("Creating and opening connection (started at " + DateTime.Now + ")...");

                IDbConnection conn = provider.CreateConnection();
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
					SqlStatisticManager.Enable(conn);
					provider.SetupConnection(conn);
                }
//                Trace.WriteLine("done at " + DateTime.Now);
                return conn;
            }
            catch
            {
//                Trace.WriteLine("error at " + DateTime.Now + ": " + ex.Message);
                throw;
            }
        }

        void IDbConnectionHolder.CloseConnection()
        {
        }

        void StoreConnection(IDbConnection conn)
        {
            string localId = Tx.Transaction.Current.TransactionInformation.LocalIdentifier;
            TxConnectionEnlistment rm = new TxConnectionEnlistment(conn);
            lock (s_lock)
            {
                s_localPool[localId] = rm;
            }
//            Trace.WriteLine("Stored newly opened connection for the transaction: " + localId);
        }

        IDbConnection LookupConnection()
        {
            CheckTransaction();
            string localId = Tx.Transaction.Current.TransactionInformation.LocalIdentifier;
            IDbConnection conn = null;
            lock (s_lock)
            {
                TxConnectionEnlistment rm;
                if (s_localPool.TryGetValue(localId, out rm))
                    conn = rm.Connection;
            }

//            if(conn == null)
//                Trace.WriteLine("Connection not exists for the transaction with id: " + localId);
            return conn;
        }

        static internal void RemoveEnlistment(string id)
        {
            lock (s_lock)
            {
                s_localPool.Remove(id);
            }
        }
    }

}
