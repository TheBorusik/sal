using Newtonsoft.Json.Linq;
using SAL.Infrastructure;

namespace SAL.API
{
    [SalServiceType("System")]
    [SalCommandName("GetCommandTestCases")]
    public class GetCommandTestCasesCommand : IHaveResult<GetCommandTestCasesResult>
    {
        public string CommandName { get; set; }
    }

    public class GetCommandTestCasesResult : ICommandResult
    {
        public string CommandName { get; set; }
        public TestCase[] TestCases { get; set; }
    }

    public class TestCase
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public JObject Case { get; set; }
    }
}