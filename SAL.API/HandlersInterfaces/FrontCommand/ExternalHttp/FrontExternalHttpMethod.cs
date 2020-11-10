using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;

namespace SAL.API.FrontCommand
{
    public abstract class FrontExternalHttpMethod : IFrontExternalHttpMethod
    {
        protected CommandContext commandContext;
        protected ISalClient salClient { get; set; }

        protected ILogger logger { get; set; }
        
        public abstract Task Handle(ExternalHttpRequest request);
        
        public async Task Handle(ExternalHttpRequest payload, CommandContext context, ExecutingContext executingContext)
        {
            salClient = executingContext.SalClient ;
            logger = executingContext.Logger;
            commandContext = context;
            await Handle(payload);
        }
        
        public Task PublishResult(ExternalHttpResponse result)
        {
            return salClient?.PublishResultAsync(result, ResultCodes.Success, commandContext.Descriptor);
        }
        
        public Task PublishError(InternalExceptionDTO error)
        {
            return salClient?.PublishResultAsync(error, commandContext.Descriptor);
        }

        public Task PublishError(string code,
            string message = null,
            object properties = null,
            System.Exception innerException = null)
        {
            return PublishError(SalError.CreateDto(code, message, properties, innerException));
        }
    }
}