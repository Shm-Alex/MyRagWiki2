using System;
using System.Data;
using System.Diagnostics;
using System.Transactions;
using Tx=System.Transactions;

namespace Ksema.Framework.Database
{
    class TxConnectionEnlistment : IEnlistmentNotification
    {
        string _tranId;
        private IDbConnection _connection;
        bool _success = true;

        public TxConnectionEnlistment(IDbConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");

            _tranId = Tx.Transaction.Current.TransactionInformation.LocalIdentifier;
            _connection = connection;
            Tx.Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);
        }

        public IDbConnection Connection
        {
            get 
            {
                if (_connection == null)
                    throw new InvalidOperationException("The connection was closed due to outer transaction " + (_success ? "commit" : "rollback"));
                return _connection; 
            }
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            try
            {
//                Trace.Write("Closing connection (prepare phase of transaction with id: " + _tranId + ")...");
                CloseConnection(true);
                preparingEnlistment.Prepared();
            }
            catch
            {
                preparingEnlistment.ForceRollback();
                throw;
            }
        }

        public void Commit(Enlistment enlistment)
        {
//            Trace.WriteLine("...transaction commited (id: " + _tranId + ")");
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            try
            {
//                Trace.Write("Closing connection (rollback of transaction with id: " + _tranId + ")...");
                CloseConnection(false);
//                Trace.WriteLine("done!");
            }
            catch
            {
//                Trace.WriteLine("error: " + ex.Message);
                throw;
            }
            finally
            {
                enlistment.Done();
            }
        }

        public void InDoubt(Enlistment enlistment)
        {
//            Trace.WriteLine("Transaction in doubt: " + _tranId);
            enlistment.Done();
        }

        void CloseConnection(bool success)
        {
            RemoveEnlistment();
            if (_connection != null)
            {
                try
                {
					SqlStatisticManager.ProcessStatistics(_connection);
                    //_connection.Dispose();
                }
                finally
                {
                    _success = success;
                    _connection = null;
                }
            }
        }

        protected virtual void RemoveEnlistment()
        {
            TxConnectionHolder.RemoveEnlistment(_tranId);
        }
    }

}
