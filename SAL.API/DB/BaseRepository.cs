using System;
using System.Data;
using System.Data.Common;
using Autofac;

namespace SAL.API
{
    public abstract class BaseRepository<T> : IBaseRepository<T>, IBaseRepository, IDisposable where T : IDisposable, IBaseRepository
    {
        protected readonly DbTransaction transaction;
        protected readonly DbConnection connection;
        private readonly ILifetimeScope scope;
        protected IDbConnectionCreator connectionCreator;
        public abstract string ConnectionName { get; }

        protected BaseRepository(ILifetimeScope scope, IDbConnectionCreator connectionCreator)
        {
            this.scope = scope;
            this.connectionCreator = connectionCreator;
            transaction = null;
            connection = null;
        }

        protected BaseRepository(ILifetimeScope scope, DbConnection connection, DbTransaction transaction)
        {
            this.scope = scope;
            this.connection = connection;
            this.transaction = transaction;
            connectionCreator = null;
        }


        protected virtual DbConnection OpenConnection()
        {
            if (connection != null)
                return SalDBObjectFactory.CreateConnection(connection, transaction);
            var con = connectionCreator.GetConnection(ConnectionName);
            con.Open();
            return con;
        }

        protected virtual DbConnection OpenConnection(string connectionName)
        {
            if (connection != null)
                throw new NotSupportedException();
            var con = connectionCreator.GetConnection(connectionName);
            con.Open();
            return con;
        }


        public virtual T BeginTransaction(string connectionName, IsolationLevel il = IsolationLevel.Unspecified)
        {
            if (connectionCreator == null)
                throw new NotSupportedException();

            var con = connectionCreator.GetConnection(connectionName ?? ConnectionName);
            con.Open();
            var tran = con.BeginTransaction(il);

            return scope.Resolve<T>(new TypedParameter(typeof(DbConnection), con), new TypedParameter(typeof(DbTransaction), tran));
        }

        public virtual TT GetRepository<TT>() where TT : IDisposable, IBaseRepository
        {
            if (connectionCreator != null)
            {
                return scope.Resolve<TT>();
            }

            return scope.Resolve<TT>(
                new TypedParameter(typeof(DbConnection), SalDBObjectFactory.CreateConnection(connection, transaction)),
                new TypedParameter(typeof(DbTransaction), SalDBObjectFactory.CreateTransaction(transaction)));
        }

        public virtual T GetCurrentRepository()
        {
            return GetRepository<T>();
        }


        private bool commited = false;

        public virtual void Commit()
        {
            if (transaction != null)
            {
                if (!commited)
                {
                    transaction.Commit();
                    commited = true;
                }
            }
        }

        private bool disposed = false;

        public void Dispose()
        {
            if (!disposed)
            {
                if (transaction != null)
                {
                    if (!commited)
                        transaction.Rollback();
                    transaction.Dispose();
                }

                connection?.Close();
                connection?.Dispose();
                disposed = true;
            }
        }
    }
}