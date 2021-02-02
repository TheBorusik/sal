using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using SAL.API;
using SAL.Core.SystemHandlers;

namespace SAL.Core.Service
{
    internal partial class BackAdapter
    {
        protected ISalClient backClient;

        public List<CommandHandlerInfo> backCommands = new List<CommandHandlerInfo>();
        public List<CommandResultHandlerInfo> backCommandResults = new List<CommandResultHandlerInfo>();
        public List<EventHandlerInfo> backEvents = new List<EventHandlerInfo>();



        protected virtual void InitSystem()
        {
            backClient = Container.Resolve<ISalClient>();
        }

        protected virtual void SendOnline()
        {
            SendIm(backClient.Contour).Wait();
        }

        protected virtual void SendOffline()
        {
            try
            {
                backClient.PublishEventAsync(new IAmOffline
                {
                    Type = AdapterConfiguration.AdapterType,
                    Name = AdapterConfiguration.AdapterName
                }, SystemEventTimes.BaseTTL).Wait();
            }
            catch (Exception)
            {
                logger.Error( "Ошибка отправки сообщения IAmOffline");
            }
        }


        public virtual void AddFrontCommandHandler(FrontCommandHandlerInfo handlerInfo)
        {

        }
        public virtual void AddExternalHttpHandler(string path)
        {

        }
        public void AddBackCommandHandler(CommandHandlerInfo handlerInfo)
        {
            backCommands.Add(handlerInfo);
        }

        public virtual void AddFrontCommandResultHandler(CommandResultHandlerInfo resultHandlerInfo)
        {

        }

        public void AddBackCommandResultHandler(CommandResultHandlerInfo resultHandlerInfo)
        {
            backCommandResults.Add(resultHandlerInfo);
        }

        public virtual void AddFrontEventHandler(EventHandlerInfo eventHandlerInfo)
        {

        }

        public void AddBackEventHandler(EventHandlerInfo eventHandlerInfo)
        {
            backEvents.Add(eventHandlerInfo);
        }

        public virtual  Task SendIm(string contour)
        {
            try
            {
                if(backClient.Contour == contour)
                    return backClient.PublishEventAsync(new IAmBackEvent
                    {
                        Type = AdapterConfiguration.AdapterType,
                        Name = AdapterConfiguration.AdapterName,
                        AdapterVersion = AdapterConfiguration.AdapterVersion,
                        SalVersion = AdapterConfiguration.SalVersion,
                        CommandHandlers = backCommands.ToArray(),
                        CommandResultHandlers = backCommandResults.ToArray(),
                        EventHandlers = backEvents.ToArray()
                    }, SystemEventTimes.BaseTTL);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Ошибка отправки сообщения IAmBackEvent");
            }
            return Task.CompletedTask;
        }
    }
}