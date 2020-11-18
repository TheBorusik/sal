using System;
using System.Data;
using Autofac.Core.Activators.Reflection;

namespace SAL.API
{
    public interface IBaseRepository
    {
        void Commit();
    }

    public interface IBaseRepository<T> where T : IDisposable, IBaseRepository
    {
        string ConnectionName { get; }

        T BeginTransaction(string connectionName = null, IsolationLevel il = IsolationLevel.Unspecified);

        TT GetRepository<TT>() where TT : IDisposable, IBaseRepository;

        T GetCurrentRepository();
    }
}