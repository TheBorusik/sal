using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.API.CommandResult;
using SAL.API.Monad;
using SAL.Core.Config;
using SAL.Core.DTO.Transport;
using SAL.Core.Helpers;
using SAL.Core.Rabbit.Interfaces;
using SAL.Core.Service;
using SAL.Infrastructure;

namespace SAL.Core.Processors
{
    internal class FrontCommandResultProcessor : IProcessor, ICommandResultProcessor
    {
        private ILoggerProvider loggerProvider;
        private ILogger logger;
        private ISalLogger salLogger;

        private ISubscription subscription;
        private ISubscription syncSubscription;

        private ISalClient salClient;
        private ISalService salService;

        private readonly ILifetimeScope container;

        private readonly IDictionary<string, LinkedList<CommandResultHandlerInfo>> resultHandlers = new Dictionary<string, LinkedList<CommandResultHandlerInfo>>();
        private readonly LinkedList<CommandResultHandlerInfo> anyResultHandlers = new LinkedList<CommandResultHandlerInfo>();
        private readonly ConcurrentDictionary<string, SimpleCommandResultHandlerInfo> simpleCommandResultHandlers = new ConcurrentDictionary<string, SimpleCommandResultHandlerInfo>();

        public FrontCommandResultProcessor(ILifetimeScope container, ILoggerProvider loggerProvider, ISalLogger salLogger)
        {
            this.container = container;
            this.loggerProvider = loggerProvider;
            this.salLogger = salLogger;
            logger = loggerProvider.CreateLogger(nameof(FrontCommandResultProcessor));
            salService = container.Resolve<ISalService>();
        }
        
        public void Start()
        {
            try
            {
                salClient = container.ResolveNamed<ISalClient>("front");

                container.ComponentRegistry.Registrations
                    .Where(r => r.Services.OfType<TypedService>().Any(ts => ts.ServiceType == typeof(ICommandResultHandler)))
                    .Select(a => a.Activator.LimitType)
                    .ForEach(RegisterCommandResultHandler);

                container.ComponentRegistry.Registrations
                    .Where(r => r.Services.OfType<TypedService>().Any(ts => ts.ServiceType == typeof(ICommonCommandResultHandler)))
                    .Select(a => a.Activator.LimitType)
                    .ForEach(RegisterCommonCommandResultHandler);

                var configWatcher = container.Resolve<IConfigWatcher>();
                var jsonConfig = configWatcher.GetSection(ConfigurationSectionNames.FrontCommandResultProcessor);
                CommandResultProcessorConfig config = new CommandResultProcessorConfig();

                if (jsonConfig != null)
                {
                    config = jsonConfig.ConvertValue<CommandResultProcessorConfig>();
                }


                var transport = container.ResolveNamed<ITransport>("front");

                var subscriptionFactory = transport.CreateMessageSubscription();

                subscription = subscriptionFactory.CreateCommandResult(config.GlobalPrefetchCount, config.InstancePrefetchCount, config.TypePrefetchCount, Handler, "FrontCommandResult");
                syncSubscription = subscriptionFactory.CreateSyncCommandResult(config.SyncPrefetchCount, SyncHandler, "FrontSyncCommandResults");
            }
            catch (Exception ex)
            {
                logger.Error("При запуске произошла ошибка:", ex);
                throw;
            }
        }

        private void RegisterCommandResultHandler(Type handlerType)
        {
            var handlerInterfaces = handlerType.GetInterfaces()
                .Where(i => i.IsAssignableTo<ICommandResultHandler>() && i.IsGenericType).ToArray();

            foreach (var handlerInterface in handlerInterfaces)
            {
                var commandType = handlerInterface.GetGenericArguments()[0];
                var commandName = commandType.GetRouteKey();

                var commandResultHandlerInfo = new CommandResultHandlerInfo
                {
                    CommandName = commandName,
                    CommandType = commandType,
                    ResultType = handlerInterface.GetGenericArguments()[1],
                    HandlerType = handlerType,
                    HandlerMethod = handlerInterface.GetMethod("ResultHandle"),
                    IsCommon = false,
                };


                if (resultHandlers.TryGetValue(commandName, out var handlers))
                {
                    handlers.AddLast(commandResultHandlerInfo);
                }
                else
                {
                    handlers = new LinkedList<CommandResultHandlerInfo>();
                    handlers.AddLast(commandResultHandlerInfo);
                    resultHandlers.Add(commandName, handlers);

                    var dtos = new List<DtoInfo>();
                    dtos.AddRange(commandResultHandlerInfo.CommandType.GetDtoInfos());
                    dtos.AddRange(commandResultHandlerInfo.ResultType.GetDtoInfos());


                    salService.AddFrontCommandResultHandler(new API.CommandResultHandlerInfo()
                    {
                        IsCommon = commandResultHandlerInfo.IsCommon,
                        CommandName = commandResultHandlerInfo.CommandName,
                        CommandDto = commandResultHandlerInfo.CommandType.Name,
                        ResultDto = commandResultHandlerInfo.ResultType.Name,
                        Dtos = dtos.ToArray()
                    });
                }

                logger.Info($"Для команды {commandName} добавлен обработчик результата {handlerType.Name}");
            }
        }

        private void RegisterCommonCommandResultHandler(Type handlerType)
        {
            var commandResultHandlerInfo = new CommandResultHandlerInfo
            {
                CommandName = null,
                CommandType = null,
                ResultType = null,
                HandlerType = handlerType,
                HandlerMethod = null,
                IsCommon = true,
            };
            
            ICommandDtoCreator dtoCreater = null;
            if (handlerType.IsAssignableTo<ICommandDtoCreator>())
                dtoCreater = (ICommandDtoCreator)container.Resolve(handlerType);


            var attrs = handlerType.GetCustomAttributes(typeof(SalCommandResultHandlerAttribute)).OfType<SalCommandResultHandlerAttribute>().ToArray();
            if (attrs.Any())
            {
                attrs.ForEach(a =>
                {
                    var name = a.Name;
                    name = Regex.Replace(name, "(.+)command$", "$1", RegexOptions.IgnoreCase);
                    var commandName = $"{a.ServiceType}.{name}";

                    if (resultHandlers.TryGetValue(commandName, out var handlers))
                    {
                        handlers.AddLast(commandResultHandlerInfo);
                    }
                    else
                    {
                        handlers = new LinkedList<CommandResultHandlerInfo>();
                        handlers.AddLast(commandResultHandlerInfo);
                        resultHandlers.Add(commandName, handlers);


                        var handlerInfo = new API.CommandResultHandlerInfo()
                        {
                            IsCommon = commandResultHandlerInfo.IsCommon,
                            CommandName = commandName,
                            Dtos = new DtoInfo[0]
                        };
                        
                        if (dtoCreater != null)
                        {
                            handlerInfo.Dtos = dtoCreater.GetCommandDtos(commandName);
                            handlerInfo.CommandDto = dtoCreater.GetCommandDtoName(commandName);
                            handlerInfo.ResultDto = dtoCreater.GetResultDtoName(commandName);
                        }
                        
                        salService.AddFrontCommandResultHandler(handlerInfo);
                    }

                    logger.Info($"Для команды {commandName} добавлен уневерсальный обработчик результата {handlerType.Name}");
                });
            }
            else
            {
                anyResultHandlers.AddLast(commandResultHandlerInfo);
                logger.Info($"Добавлен уневерсальный обработчик результата {handlerType.Name}");
            }
        }

        public void RegisterSimpleCommandResultHandler(string correlationId, TaskCompletionSource<SimpleCommandResult> completionSource, TimeSpan timeOut)
        {
            var simpleCommandResultHandler = new SimpleCommandResultHandlerInfo
            {
                CommandCorrelationId = correlationId,
                ExpireDate = DateTime.UtcNow + timeOut,
                CompletionSource = completionSource,
                CancellationTokenSource = new CancellationTokenSource()
            };

            simpleCommandResultHandlers.TryAdd(correlationId, simpleCommandResultHandler);

            simpleCommandResultHandler.CancellationTokenSource.Token.Register(() =>
            {
                if (simpleCommandResultHandlers.TryRemove(correlationId, out var scrh))
                {
                    scrh.CompletionSource.TrySetException(new SalCommandTimeoutException());
                }
            });
            simpleCommandResultHandler.CancellationTokenSource.CancelAfter(timeOut);
        }

        public void Online()
        {
            subscription.Start();
            syncSubscription.Start();
        }

        public void Offline()
        {
            subscription.Stop();
        }

        public void Stop()
        {
            subscription.Stop();
            syncSubscription.Stop();
        }

        private async Task Handler(RabbitMessage rabbitMessage, Action ack, Action nack)
        {
            try
            {
                HandlerContext.Type = HandlerTypes.Processor;
                HandlerContext.Name = "FrontCommandResultProcessor";
                var transportMessage = ExtractMessage(rabbitMessage);
                var commandResultPayload = ExtractCommandResultPayload(transportMessage);
                //SessionManager.StartAdapterSession(transportMessage.Session);
                SessionManager.Restore(transportMessage.Session);
                salLogger.LogIncoming(commandResultPayload);
                await Processing(transportMessage, commandResultPayload);
                ack();
            }
            catch (SalException ex)
            {
                nack();
                logger.Error("При обработке результата команды произошла ошибка", ex);
                await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, ex.ToDto());
            }
            catch (TargetInvocationException ex)
            {
                nack();
                if (ex.InnerException is SalException sex)
                {
                    logger.Error("При обработке результата команды произошла ошибка", ex);
                    await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, ex.ToDto());
                }
                else
                {
                    var dto = SalError.CreateDto(SalErrorCodes.Fatal,
                        "При обработке результата команды произошла ошибка"
                        , innerException: ex.InnerException
                        , properties: new
                        {
                            rabbitMessage.CorrelationId,
                            rabbitMessage.QueueName
                        });
                    logger.Error(dto, ex.InnerException);
                    await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, dto);
                }
            }
            catch (Exception ex)
            {
                nack();
                var dto = SalError.CreateDto(SalErrorCodes.Fatal,
                    "При обработке результата команды произошла ошибка"
                    , innerException: ex
                    , properties: new
                    {
                        rabbitMessage.CorrelationId,
                        rabbitMessage.QueueName
                    });
                logger.Error(dto, ex);
                await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, dto);
            }
        }

        private async Task SyncHandler(RabbitMessage rabbitMessage, Action ack, Action nack)
        {
            try
            {
                var transportMessage = ExtractMessage(rabbitMessage);
                var commandResultPayload = ExtractCommandResultPayload(transportMessage);
              // SessionManager.StartAdapterSession(transportMessage.Session);
                SessionManager.Restore(transportMessage.Session);
                salLogger.LogIncoming(commandResultPayload);
                await SyncProcessing(transportMessage, commandResultPayload);
                ack();
            }
            catch (JsonReaderException ex)
            {
       
                var dto = ex.ToDto();
                logger.Error("При обработке команды произошла ошибка десериализации", ex);
                var sb = new StringBuilder();
                sb.AppendLine("Rabbit message Payload");
                sb.AppendLine(SalEncoding.GetString(rabbitMessage.Payload));
                logger.Info(sb.ToString());
                nack();
                await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, dto); 
            }
            catch (SalException ex)
            {
                nack();
                logger.Error("При обработке результата команды произошла ошибка", ex);
                await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, ex.ToDto());
            }
            catch (TargetInvocationException ex)
            {
                nack();
                if (ex.InnerException is SalException sex)
                {
                    logger.Error("При обработке результата команды произошла ошибка", ex);
                    await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, ex.ToDto());
                }
                else
                {
                    var dto = SalError.CreateDto(SalErrorCodes.Fatal,
                        "При обработке результата команды произошла ошибка"
                        , innerException: ex.InnerException
                        , properties: new
                        {
                            rabbitMessage.CorrelationId,
                            rabbitMessage.QueueName
                        });
                    logger.Error(dto, ex.InnerException);
                    await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, dto);
                }
            }

            catch (Exception ex)
            {
                nack();
                var dto = SalError.CreateDto(SalErrorCodes.Fatal,
                    "При обработке результата команды произошла ошибка"
                    , innerException: ex
                    , properties: new
                    {
                        rabbitMessage.CorrelationId,
                        rabbitMessage.QueueName
                    });
                logger.Error(dto, ex);
                await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, dto);
            }
        }

        protected Message ExtractMessage(RabbitMessage rabbitMessage)
        {
            var transportMessage = SalSerializer.BinaryDeserialize<Message>(rabbitMessage.Payload);
            if (transportMessage == null)
                throw new Exception($"Неудалось десерилизовать сообщение | CorrelationId:{rabbitMessage.CorrelationId}");
            if (transportMessage.Type != MessageTypes.CommandResult)
                throw new Exception($"Сообщение не результат команды ({transportMessage.Type}) | CorrelationId:{rabbitMessage.CorrelationId}");

            if (transportMessage.Payload == null)
                throw new Exception($"Отсутствует message.Payload | CorrelationId:{rabbitMessage.CorrelationId}");

            if (transportMessage.Payload.Type == JTokenType.Null)
                throw new Exception($"Отсутствует message.Payload | CorrelationId:{rabbitMessage.CorrelationId}");

            return transportMessage;
        }

        protected CommandResultPayload ExtractCommandResultPayload(Message transportMessage)
        {
            var commandResultPayload = transportMessage.Payload.ConvertValue<CommandResultPayload>();

            if (commandResultPayload.Descriptor == null)
                throw new Exception($"Отсутствует commandPayload.Descriptor | CorrelationId:{transportMessage.CorrelationId}");

            if (string.IsNullOrWhiteSpace(commandResultPayload.Descriptor.CommandName))
                throw new Exception($"Пустой commandPayload.Descriptor.CommandName | CorrelationId:{transportMessage.CorrelationId}");

            if (commandResultPayload.Payload == null)
                throw new Exception($"Отсутствует commandPayload.Payload | CorrelationId:{transportMessage.CorrelationId}");


            if (commandResultPayload.Descriptor.ResultAdapterType != AdapterConfiguration.AdapterType)
                throw new Exception($"Не соответствие Descriptor.ResultAdapterType и AdapterType для результата CorrelationId:{transportMessage.CorrelationId}");


            if (!string.IsNullOrWhiteSpace(commandResultPayload.Descriptor.ResultAdapterName) &&
                !string.Equals(commandResultPayload.Descriptor.ResultAdapterName, AdapterConfiguration.AdapterName, StringComparison.InvariantCultureIgnoreCase))
                throw new Exception($"Не соответствие Descriptor.ResultAdapterName и AdapterName для результата CorrelationId:{transportMessage.CorrelationId}");

            return commandResultPayload;
        }

        private async Task Processing(Message message, CommandResultPayload commandResultPayload)
        {
            var commandResLogger = salLogger.GetLogger(commandResultPayload);

            if (commandResultPayload.Descriptor.TTL.HasValue &&
                commandResultPayload.Descriptor.PublishTimeStamp + commandResultPayload.Descriptor.TTL.Value <= DateTime.UtcNow)
            {
                commandResLogger.Trace("Результат команды - протух");
                return;
            }


            HandlerContext.Type = HandlerTypes.CommandResultHandler;
            HandlerContext.Name = commandResultPayload.Descriptor.CommandName;

            using var scope = container.BeginLifetimeScope();


            var executingContext = new ExecutingContext
            {
                Scope = scope,
                SalClient = scope.ResolveNamed<ISalClient>("front"),
                Logger = commandResLogger
            };

            var context = new CommandResultContext
            {
                Descriptor = commandResultPayload.Descriptor,
                Session = message.Session.DeepClone() as JObject
            };


            var isHandled = false;

            if (resultHandlers.TryGetValue(commandResultPayload.Descriptor.CommandName, out var resultCommandHandlersInfo))
            {
                foreach (var rchi in resultCommandHandlersInfo)
                {
                    HandlerContext.Name = rchi.HandlerType.Name;
                    isHandled = await ExecuteResultHandlerAsync(scope, rchi, commandResultPayload.Payload, context,
                        executingContext);
                    salLogger.LogHandler(commandResultPayload, rchi.HandlerType.Name, isHandled);
                    if (isHandled)
                        break;
                }
            }

            if (!isHandled)
            {
                var node = anyResultHandlers.First;
                while (node != null && isHandled == false)
                {
                    HandlerContext.Name = node.Value.HandlerType.Name;
                    isHandled = await ExecuteResultHandlerAsync(scope, node.Value, commandResultPayload.Payload,
                        context, executingContext);
                    salLogger.LogHandler(commandResultPayload, node.Value.HandlerType.Name, isHandled);
                    node = node.Next;
                }
            }


            if (!isHandled)
            {
                HandlerContext.Name = commandResultPayload.Descriptor.CommandName;
                throw SalError.CreateException(SalErrorCodes.NotHandledCommandResult);
            }
        }

        private Task SyncProcessing(Message message, CommandResultPayload commandResultPayload)
        {
            if (simpleCommandResultHandlers.TryRemove(commandResultPayload.Descriptor.CorrelationId, out var simpleCommandResultHandler))
            {
                var context = new CommandResultContext
                {
                    Descriptor = commandResultPayload.Descriptor,
                    Session = message.Session.DeepClone() as JObject
                };


                var setValue = simpleCommandResultHandler.CompletionSource.TrySetResult(new SimpleCommandResult
                {
                    CommandResult = commandResultPayload.Payload,
                    CommandResultContext = context
                });
                salLogger.LogHandler(commandResultPayload, "Sync", setValue);
            }
            else
            {
                salLogger.LogNullHandler(commandResultPayload);
            }

            return Task.CompletedTask;
        }

        public Task<bool> ExecuteResultHandlerAsync(ILifetimeScope scope, CommandResultHandlerInfo rchi, CommonCommandResult result, CommandResultContext context, ExecutingContext executingContext)
        {
            var handler = (ICommandResultHandler) scope.Resolve(rchi.HandlerType);

            handler.SetContexts(context, executingContext);

            if (rchi.IsCommon)
                return ExecuteCommonResultHandlerAsync(handler, result);
            else
            {
                var commandResultType = typeof(CommandResult<>).MakeGenericType(rchi.ResultType);
                var commandResult = result.ConvertValue(commandResultType);
                return (Task<bool>) rchi.HandlerMethod.Invoke(handler, new[] {commandResult});
            }
        }

        private Task<bool> ExecuteCommonResultHandlerAsync(object handler, CommonCommandResult commonResult)
        {
            if (handler is ICommonCommandResultHandler ccrha)
            {
                return ccrha.ResultHandle(commonResult);
            }
            else
            {
                throw SalError.CreateException(SalErrorCodes.Fatal, "Обработчик не являеться общим", properties: new {handlerType = handler.GetType().Name});
            }
        }
    }
}