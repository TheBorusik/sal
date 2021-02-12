using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using SAL.API;
using SAL.Core.SystemHandlers;

namespace SAL.Core.Service
{
    internal partial class FrontAdapter
    {
        protected ISalClient frontClient;

        public List<FrontCommandHandlerInfo> frontCommands = new List<FrontCommandHandlerInfo>();
        public List<CommandResultHandlerInfo> frontCommandResults = new List<CommandResultHandlerInfo>();
        public List<EventHandlerInfo> frontEvents = new List<EventHandlerInfo>();
        public List<string> externalHttp = new List<string>();

        protected override void InitSystem()
        {
            base.InitSystem();
            frontClient = Container.ResolveNamed<ISalClient>("front");
        }

        protected override void SendOnline()
        {
            SendIm(backClient.Contour);
            SendIm(frontClient.Contour);
        }

        protected override void SendOffline()
        {
            base.SendOffline();
            try
            {
                frontClient.PublishEventAsync(new IAmOffline
                {
                    Type = AdapterConfiguration.AdapterType,
                    Name = AdapterConfiguration.AdapterName
                }, SystemEventTimes.BaseTTL).Wait();
            }
            catch (Exception)
            {
                logger.Error("Ошибка отправки сообщения IAmOffline во front");
            }
        }


        public override void AddFrontCommandHandler(FrontCommandHandlerInfo handlerInfo)
        {
            frontCommands.Add(handlerInfo);
        }

        public override void AddExternalHttpHandler(string path)
        {
            externalHttp.Add(path);
        }

        public override void AddFrontCommandResultHandler(CommandResultHandlerInfo resultHandlerInfo)
        {
            frontCommandResults.Add(resultHandlerInfo);
        }

        public override void AddFrontEventHandler(EventHandlerInfo eventHandlerInfo)
        {
            frontEvents.Add(eventHandlerInfo);
        }


        public override Task SendIm(string contour)
        {
            base.SendIm(contour);
            try
            {
                if(frontClient.Contour == contour)
                    return frontClient.PublishEventAsync(new IAmFrontEvent
                    {
                        Type = AdapterConfiguration.AdapterType,
                        Name = AdapterConfiguration.AdapterName,
                        AdapterVersion = AdapterConfiguration.AdapterVersion,
                        SalVersion = AdapterConfiguration.SalVersion,
                        AdapterHostName = AdapterConfiguration.AdapterHostName,
                        AdapterHostIp = AdapterConfiguration.AdapterHostIp,
                        InDocker = AdapterConfiguration.InDocker,
                        CommandHandlers = frontCommands.ToArray(),
                        CommandResultHandlers = frontCommandResults.ToArray(),
                        EventHandlers = frontEvents.ToArray(),
                        ExternalHttp = externalHttp.ToArray()
                    }, SystemEventTimes.BaseTTL);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Ошибка отправки сообщения IAmFrontEvent");
            }
            return Task.CompletedTask;
        }
    }
}