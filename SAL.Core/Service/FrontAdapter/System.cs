using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using SAL.API;
using SAL.API.Const;
using SAL.Core.SystemHandlers;
using SAL.Infrastructure;

namespace SAL.Core.Service
{
    internal partial class FrontAdapter
    {
        protected ISalClient frontClient;

        public List<FrontCommandHandlerInfo> frontCommands = new();
        public List<CommandResultHandlerInfo> frontCommandResults = new();
        public List<EventHandlerInfo> frontEvents = new();
        public List<ExternalHandlerInfo> externalHttp = new();

        protected override void InitSystem()
        {
            base.InitSystem();
            frontClient = Container.ResolveKeyed<ISalClient>(Contour.Front);
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
                frontClient.PublishEventAsync("System.IAmOfflineEvent", new IAmOfflineEvent
                {
                    Type = AdapterConfiguration.AdapterType,
                    Name = AdapterConfiguration.AdapterName
                }, true, SalConst.SystemEventTTL).Wait();
            }
            catch (Exception)
            {
                logger.Error("Ошибка отправки сообщения IAmOffline во front");
            }
        }


        public override void AddExternalHttpHandler(ExternalHandlerInfo extInfo)
        {
            externalHttp.Add(extInfo);
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


        public override Task SendIm(Contour contour)
        {
            base.SendIm(contour);
            try
            {
                if (frontClient.Contour == contour)
                    return frontClient.PublishEventAsync("System.IAmFrontEvent", new IAmFrontEvent
                    {
                        Type = AdapterConfiguration.AdapterType,
                        Name = AdapterConfiguration.AdapterName,
                        AdapterContour = AdapterConfiguration.AdapterContour,
                        AdapterVersion = AdapterConfiguration.AdapterVersion,
                        SalVersion = AdapterConfiguration.SalVersion,
                        InDocker = AdapterConfiguration.InDocker,
                        MachineName = AdapterConfiguration.MachineName,
                        CommandHandlers = frontCommands.ToArray(),
                        CommandResultHandlers = frontCommandResults.ToArray(),
                        EventHandlers = frontEvents.ToArray(),
                        ExternalHandlers = externalHttp.ToArray(),
                    }, true, SalConst.SystemEventTTL);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Ошибка отправки сообщения IAmFrontEvent");
            }

            return Task.CompletedTask;
        }
    }
}