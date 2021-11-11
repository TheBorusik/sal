using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using SAL.API;
using SAL.Infrastructure;

namespace SAL.Core.SystemHandlers
{
    [BackCommandName("System.StopAdapter")]
    class StopAdapterCommandHandler : BaseBackCommandHandlerAsync<Nothing, Nothing>
    {
        private readonly IHostApplicationLifetime lifeTime;

        public StopAdapterCommandHandler(IHostApplicationLifetime lifeTime)
        {
            this.lifeTime = lifeTime;
        }

        public override async Task Handle(Nothing command)
        {
            await PublishResult(new Nothing());
            lifeTime.StopApplication();
            
        }
    }
}