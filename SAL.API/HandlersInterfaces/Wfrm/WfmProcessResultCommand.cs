using SAL.Infrastructure;


namespace SAL.API
{
    [SalCommandName("WFM.Result")]
    public class WfmProcessResultCommand : IHaveResult<None>
    {
        public string HandlerName { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public WfmProcessInfo ProcessInfo { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public CommonCommandResult ProcessResult { get; set; }
    }
}