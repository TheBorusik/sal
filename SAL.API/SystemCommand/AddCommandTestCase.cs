using SAL.Infrastructure;
using SAL.Infrastructure.ValidationAttribute;

namespace SAL.API.SystemCommand
{
    [SalServiceType("System")]
    [SalCommandName("AddCommandTestCase")]
    public class AddCommandTestCaseCommand : IHaveResult<Nothing>
    {
        [Required]public string CommandName { get; set; }
        [Required]public TestCase TestCase { get; set; }
    }
}