using System.Data.Common;
using IsolationLevel = System.Data.IsolationLevel;

namespace SAL.API
{
    public class DbTransactionWrapper : DbTransaction
    {
        protected readonly DbTransaction externalTransaction;

        public DbTransactionWrapper(DbTransaction externalConnection)
        {
            this.externalTransaction = externalConnection;
        }

        public override void Commit()
        {
        }

        public override void Rollback()
        {
        }

        protected override DbConnection DbConnection => externalTransaction.Connection;
        public override IsolationLevel IsolationLevel => externalTransaction.IsolationLevel;

    }
}