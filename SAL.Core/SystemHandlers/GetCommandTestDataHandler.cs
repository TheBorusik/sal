using System.Threading.Tasks;
using SAL.API;
using SAL.API.Command;
using SAL.API.SystemCommand;
using SAL.Core.Config;
using SAL.Infrastructure;

namespace SAL.Core.SystemHandlers
{
    [SalInstanceHandler]
    class GetCommandTestCasesHandler : BaseCommandHandlerAsync<GetCommandTestCasesCommand, GetCommandTestCasesResult>
    {
        private IConfigWatcher configWatcher;

        public GetCommandTestCasesHandler(IConfigWatcher configWatcher)
        {
            this.configWatcher = configWatcher;
        }

        public override async Task Handle(GetCommandTestCasesCommand command)
        {
            var testData = configWatcher.GetSection(ConfigurationSectionNames.CommandTestData);
            if (testData == null)
            {
                await PublishError(SalErrorCodes.NotFound);
                return;
            }

            var commandTestData = testData.GetValueIC(command.CommandName);
            if (commandTestData == null)
            {
                await PublishError(SalErrorCodes.NotFound);
                return;
            }
            
            await PublishResult(new GetCommandTestCasesResult
            {
                CommandName = command.CommandName,
                TestCases = commandTestData.ConvertValue<TestCase[]>()
            });
        }
    }
}