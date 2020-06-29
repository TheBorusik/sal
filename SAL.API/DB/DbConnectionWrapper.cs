using System;
using System.Data;
using System.Data.Common;
using System.Transactions;
using IsolationLevel = System.Data.IsolationLevel;

namespace SAL.API
{
    public class DbConnectionWrapper : DbConnection
    {
        private readonly DbConnection externalConnection;
        private readonly DbTransaction externalTransaction;

        public DbConnectionWrapper(DbConnection externalConnection, DbTransaction externalTransaction)
        {
            this.externalConnection = externalConnection;
            this.externalTransaction = externalTransaction;
        }


        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            if (externalTransaction == null)
                return externalConnection.BeginTransaction(isolationLevel);
            return new DbTransactionWrapper(externalTransaction);
        }

        protected override DbCommand CreateDbCommand()
        {
            return externalConnection.CreateCommand();
        }

        public override void EnlistTransaction(Transaction transaction)
        {
            externalConnection.EnlistTransaction(transaction);
        }

        public override void Close()
        {
        }

        public override void ChangeDatabase(string databaseName)
        {
            throw new NotImplementedException();
        }

        public override void Open()
        {
        }

        public override string ConnectionString
        {
            get => externalConnection.ConnectionString;
            set => externalConnection.ConnectionString = value;
        }

        public override string Database => externalConnection.Database;
        public override ConnectionState State => externalConnection.State;
        public override string DataSource => externalConnection.DataSource;
        public override string ServerVersion => externalConnection.ServerVersion;

        public override int ConnectionTimeout => externalConnection.ConnectionTimeout;
    }
}