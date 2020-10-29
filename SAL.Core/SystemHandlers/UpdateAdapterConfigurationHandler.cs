using System.Threading.Tasks;
using SAL.API;
using SAL.API.Command;
using SAL.API.SystemCommand;
using SAL.Infrastructure;

namespace SAL.Core.SystemHandlers
{
    [SalInstanceHandler]
    class UpdateAdapterConfigurationHandler : BaseCommandHandlerAsync<UpdateAdapterConfigurationCommand, Nothing>
    {
        private IConfigWatcher configWatcher;

        public UpdateAdapterConfigurationHandler(IConfigWatcher configWatcher)
        {
            this.configWatcher = configWatcher;
        }

        public override async Task Handle(UpdateAdapterConfigurationCommand command)
        {
            await PublishError(SalErrorCodes.NotImplemented);
        }
    }
}