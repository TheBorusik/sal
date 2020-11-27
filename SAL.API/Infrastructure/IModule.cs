using Autofac;

namespace SAL.API
{
    public interface IModule
    {
        void Configure(ContainerBuilder builder, IConfigWatcher config);
    }
}
