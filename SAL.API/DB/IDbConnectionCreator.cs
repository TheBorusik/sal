
using System.Data.Common;

namespace SAL.API
{
    public interface IDbConnectionCreator
    {
        DbConnection GetConnection(string connectionName = null);
        T GetConnectionProperty<T>(string properyName, string connectionName = null);
        string GetConnectionString(string connectionName = null);
        string GetProviderName(string connectionName = null);
        string GetDefaultConnectionName();
    }


}
