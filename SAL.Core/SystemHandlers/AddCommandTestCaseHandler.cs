using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.Infrastructure;

namespace SAL.Core.SystemHandlers
{
    [SalInstanceHandler]
    class AddCommandTestCaseHandler : BaseCommandHandlerAsync<AddCommandTestCaseCommand, Nothing>
    {
        private IConfigWatcher configWatcher;

        public AddCommandTestCaseHandler(IConfigWatcher configWatcher)
        {
            this.configWatcher = configWatcher;
        }

        public override async Task Handle(AddCommandTestCaseCommand command)
        {
            if (!configWatcher.CanUpdateConfig())
            {
                await PublishError("ConfigCannotBeChanged");
                return;
            }
            
            
            var testData = configWatcher.GetSection(ConfigurationSectionNames.CommandTestData) as JObject;
            if (testData == null)
            {
                testData = new JObject();
            }

            var commandTestData = testData.GetValueIC(command.CommandName)  as JArray;
            if (commandTestData == null)
            {
                commandTestData = new JArray();
                testData.Add(command.CommandName, commandTestData);
            }


            var testCases = commandTestData.ConvertValue<TestCase[]>().ToList();
            var testCase = testCases.FirstOrDefault(c => c.Name == command.TestCase.Name);
            if (testCase == null)
            {
                testCases.Add(command.TestCase);
            }
            else
            {
                testCase.Description = command.TestCase.Description;
                testCase.Case = command.TestCase.Case;
            }
            
            commandTestData.Clear();
            testCases.ForEach(t => commandTestData.Add(JToken.FromObject(t)));
            configWatcher.UpdateSection(ConfigurationSectionNames.CommandTestData, testData);
            await PublishResult(new Nothing());

        }
    }
}