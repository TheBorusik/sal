using System;
using System.Data.Common;

namespace SAL.API
{
    public static class SalDBObjectFactory
    {
        private static Func<DbTransaction, DbTransactionWrapper> createTransactionFunc = t => new DbTransactionWrapper(t);

        internal static DbTransactionWrapper CreateTransaction(DbTransaction externalConnection)
        {
            return createTransactionFunc(externalConnection);
        }

        public static void SetDbTransactionWrapper(Func<DbTransaction, DbTransactionWrapper> createFunc)
        {
            createTransactionFunc = createFunc;
        }


        private static Func<DbConnection, DbTransaction, DbConnectionWrapper> createConnectionFunc = (c,t) => new DbConnectionWrapper(c,t);

        internal static DbConnectionWrapper CreateConnection(DbConnection externalConnection, DbTransaction externalTransaction)
        {
            return createConnectionFunc(externalConnection, externalTransaction);
        }

        public static void SetDbConnectionWrapper(Func<DbConnection, DbTransaction, DbConnectionWrapper> createFunc)
        {
            createConnectionFunc = createFunc;
        }


    }
}