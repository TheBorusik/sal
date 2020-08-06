using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using NLog;
using SAL.API;
using SAL.API.Client;
using SAL.API.Events;

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
            SendIm().Wait();
        }

        protected virtual void SendOffline()
        {
            try
            {
                backClient.PublishEventAsync(new IAmOffline
                {
                    Type = ServiceConfiguration.AdapterType,
                    Name = ServiceConfiguration.AdapterName
                }).Wait();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Ошибка отправки сообщения IAmOffline");
            }
        }


        public virtual void AddFrontCommandHandler(FrontCommandHandlerInfo handlerInfo)
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

        public virtual  Task SendIm()
        {
            try
            {
                return backClient.PublishEventAsync(new IAmBackEvent
                {
                    Type = ServiceConfiguration.AdapterType,
                    Name = ServiceConfiguration.AdapterName,
                    CommandHandlers = backCommands.ToArray(),
                    CommandResultHandlers = backCommandResults.ToArray(),
                    EventHandlers = backEvents.ToArray()
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Ошибка отправки сообщения IAmBackEvent");
            }
            return Task.CompletedTask;
        }
    }
}