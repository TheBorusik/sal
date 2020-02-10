using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq;
using Autofac;
using Newtonsoft.Json.Linq;
using SAL.API;

namespace SAL.Core.DB
{
    internal class ConnectionCache
    {
        public string ConnectionString;
        public DbProviderFactory ProviderFactory;
    }

    public class DbConnectionCreator : IDbConnectionCreator
    {
        private const string mainSectionName = "ConnectionManager";
        private const string defaultConnectionKeyName = "DefaultConnection";
        private const string defaultConnectionSettingsSectionName = "DefaultSettings";

        private const string connectionsDataSectionName = "ConnectionData";
        private const string connectionPropertiesSectionName = "Properties";
        private const string connectionProviderNameKeyName = "ProviderName";
        private const string defaultConnectionName = "Default";

        private JObject config;
        private ILifetimeScope scope;


        private ConcurrentDictionary<string, ConnectionCache> cache = new ConcurrentDictionary<string, ConnectionCache>();


        public DbConnectionCreator(IConfigWatcher configWatcher, ILifetimeScope scope)
        {
            config = configWatcher.GetSection(mainSectionName) as JObject;
            configWatcher.Subscribe(mainSectionName, (s, e) => { config = configWatcher.GetSection(mainSectionName) as JObject; });
            this.scope = scope;

        }

        public DbConnection GetConnection(string connectionName = null)
        {
            if (string.IsNullOrWhiteSpace(connectionName))
                connectionName = GetDefaultConnectionName();
            if (!cache.TryGetValue(connectionName, out var cacheItem))
            {
                var data = BuildString(connectionName);
                cacheItem = new ConnectionCache
                {
                    ProviderFactory = data.fact,
                    ConnectionString = data.connectionString
                };

                cache.AddOrUpdate(connectionName, cacheItem, (s, cc) => cacheItem);
            }


            var connection = cacheItem.ProviderFactory.CreateConnection();
            if (connection == null)
                throw SalError.CreateException(ResultCodes.Fatal, "Неудалось создать Connection");
            connection.ConnectionString = cacheItem.ConnectionString;
            return connection;
        }

        public string GetConnectionString(string connectionName = null)
        {
            if (string.IsNullOrWhiteSpace(connectionName))
                connectionName = GetDefaultConnectionName();
            if (!cache.TryGetValue(connectionName, out var cacheItem))
            {
                var data = BuildString(connectionName);
                cacheItem = new ConnectionCache
                {
                    ProviderFactory = data.fact,
                    ConnectionString = data.connectionString
                };

                cache.AddOrUpdate(connectionName, cacheItem, (s, cc) => cacheItem);
            }

            return cacheItem.ConnectionString;
        }

        private (string connectionString, DbProviderFactory fact) BuildString(string connectionName)
        {
            if (config == null)
                throw SalError.CreateException(ResultCodes.Fatal, $"Section '{mainSectionName}' not found");

            var connectionSection = GetConnectionSection(connectionName);

            var providerName = GetProviderName(connectionSection);

            if (string.IsNullOrWhiteSpace(providerName))
                throw SalError.CreateException(ResultCodes.Fatal, $"Connection '{connectionName}' ProviderName not set");

            var fact = GetFactory(providerName);
            var builder = fact.CreateConnectionStringBuilder();
            if (builder == null)
                throw SalError.CreateException(ResultCodes.Fatal, "Неудалось создать ConnectionStringBuilder");

            var ignoreKeys = new[] { connectionPropertiesSectionName, connectionProviderNameKeyName };

            var defaultSettings = config.GetValue(defaultConnectionSettingsSectionName) as JObject;

            defaultSettings?.ForEach(val =>
            {
                if (val is JProperty jProp)
                {
                    var value = jProp.ConvertValue<string>();
                    if (!string.IsNullOrWhiteSpace(value))
                        builder[jProp.Name] = value;
                }
            });

            connectionSection.ForEach(val =>
            {
                if (val is JProperty jProp)
                {
                    if (ignoreKeys.Contains(jProp.Name, StringComparer.InvariantCultureIgnoreCase))
                        return;
                    var value = jProp.ConvertValue<string>();
                    if (!string.IsNullOrWhiteSpace(value))
                        builder[jProp.Name] = value;
                }
            });

            return (builder.ToString(), fact);
        }

        private DbProviderFactory GetFactory(string providerName)
        {
            if (scope.TryResolveNamed(providerName, typeof(DbProviderFactory), out var providerFactory))
                return (DbProviderFactory)providerFactory;
            throw SalError.CreateException(ResultCodes.Fatal, $"Data provider '{providerName}' not register in Autofac");
        }


        public T GetConnectionProperty<T>(string properyName, string connectionName = null)
        {
            var connectionSection = GetConnectionSection(connectionName);

            if (!connectionSection.ContainsKey(properyName))
                throw SalError.CreateException(ResultCodes.Fatal, $"Connection propety \"{properyName}\" not found");

            return connectionSection.GetValue<T>(properyName);
        }

        public string GetProviderName(string connectionName = null)
        {
            return GetProviderName(GetConnectionSection(connectionName));
        }

        public string GetDefaultConnectionName()
        {
            if (config == null)
                throw SalError.CreateException(ResultCodes.Fatal, $"Section '{mainSectionName}' not found");
           return config.GetSafeValue(defaultConnectionKeyName, defaultConnectionName);

        }

        internal string GetProviderName(JObject connectionSection)
        {
            return connectionSection.GetSafeValue(connectionProviderNameKeyName, string.Empty);
        }

        private JObject GetConnectionSection(string connectionName = null)
        {
            if (config == null)
                throw SalError.CreateException(ResultCodes.Fatal, $"Section '{mainSectionName}' not found");

            if (string.IsNullOrWhiteSpace(connectionName))
                connectionName = GetDefaultConnectionName();

            var connectionData = config.GetValueIC(connectionsDataSectionName) as JObject;

            if (connectionData == null)
                throw SalError.CreateException(ResultCodes.Fatal, $"Section '{mainSectionName}.{connectionsDataSectionName}' not found");


            var connectionSection = connectionData.GetValueIC(connectionName) as JObject;
            if (connectionSection == null)
                throw SalError.CreateException(ResultCodes.Fatal, $"Connection name '{connectionName}' not found");

            return connectionSection;
        }
    }
}