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
    internal partial class FrontAdapter
    {
        protected ISalClient frontClient;

        public List<FrontCommandHandlerInfo> frontCommands = new List<FrontCommandHandlerInfo>();
        public List<CommandResultHandlerInfo> frontCommandResults = new List<CommandResultHandlerInfo>();
        public List<EventHandlerInfo> frontEvents = new List<EventHandlerInfo>();

        protected override void InitSystem()
        {
            base.InitSystem();
            frontClient = Container.ResolveNamed<ISalClient>("front");
        }

        protected override void SendOnline()
        {
            SendIm().Wait();
        }

        protected override void SendOffline()
        {
            base.SendOffline();
            try
            {
                frontClient.PublishEventAsync(new IAmOffline
                {
                    Type = ServiceConfiguration.AdapterType,
                    Name = ServiceConfiguration.AdapterName
                }).Wait();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Ошибка отправки сообщения IAmOffline во front");
            }
        }


        public override void AddFrontCommandHandler(FrontCommandHandlerInfo handlerInfo)
        {
            frontCommands.Add(handlerInfo);
        }

        public override void AddFrontCommandResultHandler(CommandResultHandlerInfo resultHandlerInfo)
        {
            frontCommandResults.Add(resultHandlerInfo);
        }

        public override void AddFrontEventHandler(EventHandlerInfo eventHandlerInfo)
        {
            frontEvents.Add(eventHandlerInfo);
        }


        public override Task SendIm()
        {
            base.SendIm();
            try
            {
                return frontClient.PublishEventAsync(new IAmFrontEvent
                {
                    Type = ServiceConfiguration.AdapterType,
                    Name = ServiceConfiguration.AdapterName,
                    CommandHandlers = frontCommands.ToArray(),
                    CommandResultHandlers = frontCommandResults.ToArray(),
                    EventHandlers = frontEvents.ToArray()
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Ошибка отправки сообщения IAmFrontEvent");
            }
            return Task.CompletedTask;
        }
    }
}