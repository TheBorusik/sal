using SAL.Infrastructure;

namespace SAL.API
{
    [SalServiceType("WFM")]
    [SalCommandName("Result")]
    public class WfmProcessResultCommand : IHaveResult<None>
    {
        public string HandlerName { get; set; }
        public WfmProcessInfo ProcessInfo { get; set; }
        public CommonCommandResult ProcessResult { get; set; }
    }
}