using System.Threading.Tasks;
using SAL.API;
using SAL.API.Command;
using SAL.API.SystemCommand;
using SAL.Infrastructure;

namespace SAL.Core.SystemHandlers
{
    [SalInstanceHandler]
    class GetAdapterConfigurationHandler : BaseCommandHandlerAsync<GetAdapterConfigurationCommand, GetAdapterConfigurationResult>
    {
        private IConfigWatcher configWatcher;

        public GetAdapterConfigurationHandler(IConfigWatcher configWatcher)
        {
            this.configWatcher = configWatcher;
        }

        public override async Task Handle(GetAdapterConfigurationCommand command)
        {
            await PublishResult(new GetAdapterConfigurationResult
            {
                AdapterType = AdapterConfiguration.AdapterType,
                AdapterName = AdapterConfiguration.AdapterName,
                Configuration = configWatcher.GetConfig()
            });

        }
    }
}