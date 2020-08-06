using System.Threading.Tasks;
using SAL.API;

namespace SAL.Core.Service
{
    public interface ISalService
    {
        void Start();
        void Stop();

        public void AddFrontCommandHandler(FrontCommandHandlerInfo handlerInfo);
        public void AddBackCommandHandler(CommandHandlerInfo handlerInfo);
        public void AddFrontCommandResultHandler(CommandResultHandlerInfo resultHandlerInfo);
        public void AddBackCommandResultHandler(CommandResultHandlerInfo resultHandlerInfo);
        public void AddFrontEventHandler(EventHandlerInfo eventHandlerInfo);
        public void AddBackEventHandler(EventHandlerInfo eventHandlerInfo);
        
        //service
        Task SendIm();
    }

}