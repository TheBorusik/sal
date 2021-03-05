using SAL.Infrastructure;

namespace SAL.API
{
    [SalServiceType("System")]
    [SalCommandName("AddCommandTestCase")]
    public class AddCommandTestCaseCommand : IHaveResult<Nothing>
    {
        [Required]public string CommandName { get; set; }
        [Required]public TestCase TestCase { get; set; }
    }
}