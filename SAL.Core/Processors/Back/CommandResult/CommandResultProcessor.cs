using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.Core.Helpers;
using SAL.Core.Rabbit.Interfaces;
using SAL.Core.Service;
using SAL.Infrastructure;

namespace SAL.Core.Processors
{
    internal class CommandResultProcessor : IProcessor, ICommandResultProcessor
    {
        private ILoggerProvider loggerProvider;
        private ILogger logger;
        private ISalLogger salLogger;

        private ISubscription subscription;
        private ISubscription syncSubscription;

        private ISalClient salClient;
        private ISalService salService;

        private readonly ILifetimeScope container;

        private readonly Dictionary<string, LinkedList<CommandResultHandlerInfo>> resultHandlers = new();
        private readonly LinkedList<CommandResultHandlerInfo> anyResultHandlers = new();
        private readonly ConcurrentDictionary<string, SimpleCommandResultHandlerInfo> simpleCommandResultHandlers = new();


        public CommandResultProcessor(ILifetimeScope container, ILoggerProvider loggerProvider, ISalLogger salLogger)
        {
            this.container = container;
            this.loggerProvider = loggerProvider;
            this.salLogger = salLogger;
            logger = loggerProvider.CreateLogger(nameof(CommandResultProcessor));
            salService = container.Resolve<ISalService>();
        }

        public void Start()
        {
            try
            {
                salClient = container.Resolve<ISalClient>();

                container.ComponentRegistry.Registrations
                    .Where(r => r.Services.OfType<TypedService>().Any(ts => ts.ServiceType == typeof(ICommandResultHandler2)))
                    .Select(a => a.Activator.LimitType)
                    .ForEach(RegisterCommandResultHandler);

                container.ComponentRegistry.Registrations
                    .Where(r => r.Services.OfType<TypedService>().Any(ts => ts.ServiceType == typeof(ICommonCommandResultHandler2)))
                    .Select(a => a.Activator.LimitType)
                    .ForEach(RegisterCommonCommandResultHandler);

                var configWatcher = container.Resolve<IConfigWatcher>();
                var jsonConfig = configWatcher.GetSection(ConfigurationSectionNames.CommandResultProcessor);
                CommandResultProcessorConfig config = new CommandResultProcessorConfig();

                if (jsonConfig != null)
                {
                    config = jsonConfig.ConvertValue<CommandResultProcessorConfig>();
                }


                var transport = container.Resolve<ITransport>();

                var subscriptionFactory = transport.CreateMessageSubscription();

                subscription = subscriptionFactory.CreateCommandResult(config.GlobalPrefetchCount, config.InstancePrefetchCount, config.TypePrefetchCount, Handler);
                syncSubscription = subscriptionFactory.CreateSyncCommandResult(config.SyncPrefetchCount, SyncHandler);
            }
            catch (Exception ex)
            {
                logger.Error("При запуске произошла ошибка:", ex);
                throw;
            }
        }

        private void RegisterCommandResultHandler(Type handlerType)
        {
            var contourAttr = handlerType.GetAttribute<SalContourHandlerAttribute>();
            if(contourAttr?.Contour == Contour.Front)
                return;
            
            var handlerInterfaces = handlerType.GetInterfaces()
                .Where(i => i.IsAssignableTo<ICommandResultHandler2>() && i.IsGenericType).ToArray();
            
            ICommandSchemeCreator schemaCreater = null;
            if (handlerType.IsAssignableTo<ICommandSchemeCreator>())
                schemaCreater = (ICommandSchemeCreator) container.Resolve(handlerType);

            foreach(var handlerInterface in handlerInterfaces)
            {

                string commandName;
                Type resultType;
                var args = handlerInterface.GetGenericArguments();
                
                
                if (args.Length == 1)
                {
                    commandName = handlerType.GetSalName();
                    resultType = args[0];
                }
                else
                {
                    commandName = args[0].GetSalName();
                    resultType = args[1];
                }
                
                
                


                var commandResultHandlerInfo = new CommandResultHandlerInfo
                {
                    CommandName = commandName,
                    ResultType = resultType,
                    HandlerType = handlerType,
                    HandlerMethod = handlerInterface.GetMethod("ResultHandle"),
                    IsCommon = false,
                    ResultSchema = schemaCreater == null ? SalSchema.Generate(resultType) : schemaCreater.GetResultSchema(commandName)
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

                    
                    salService.AddBackCommandResultHandler(new API.CommandResultHandlerInfo()
                    {
                        IsCommon = commandResultHandlerInfo.IsCommon,
                        CommandName = commandResultHandlerInfo.CommandName,
                        ResultSchema = commandResultHandlerInfo.ResultSchema
                    });
                }

                logger.Info($"Для команды {commandName} добавлен обработчик результата {handlerType.Name}");
            }
        }

        private void RegisterCommonCommandResultHandler(Type handlerType)
        {
            var contourAttr = handlerType.GetAttribute<SalContourHandlerAttribute>();
            if(contourAttr?.Contour == Contour.Front)
                return;
            
            
            ICommandSchemeCreator schemaCreater = null;
            if (handlerType.IsAssignableTo<ICommandSchemeCreator>())
                schemaCreater = (ICommandSchemeCreator) container.Resolve(handlerType);
            
            var attrs = handlerType.GetAttributes<SalCommandNameAttribute>();
            if (attrs.Any())
            {
                attrs.ForEach(a =>
                {
                    var commandName = a.Name;
                    
                    var commandResultHandlerInfo = new CommandResultHandlerInfo
                    {
                        CommandName = commandName,
                        ResultType = null,
                        HandlerType = handlerType,
                        HandlerMethod = null,
                        IsCommon = true,
                    };
                    
                    if (schemaCreater != null)
                    {
                        commandResultHandlerInfo.ResultSchema = schemaCreater.GetResultSchema(commandName);
                    }
                    

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
                            ResultSchema = commandResultHandlerInfo.ResultSchema
                        };
                        
                        salService.AddBackCommandResultHandler(handlerInfo);
                    }

                    logger.Info($"Для команды {commandName} добавлен уневерсальный обработчик результата {handlerType.Name}");
                });
            }
            else
            {
                anyResultHandlers.AddLast(new CommandResultHandlerInfo
                {
                    HandlerType = handlerType,
                    IsCommon = true
                });
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
                HandlerContext.Set(HandlerTypes.Processor, "CommandResultProcessor", rabbitMessage.CorrelationId);
                var transportMessage = ExtractMessage(rabbitMessage);
                var commandResultPayload = ExtractCommandResultPayload(transportMessage);
                HandlerContext.Update(commandResultPayload.ContextInfo);
                salLogger.LogIncoming(commandResultPayload);
                await Processing(transportMessage, commandResultPayload);
                ack();
            }
            catch (JsonReaderException ex)
            {
                var dto = ex.ToDto();
                logger.Error("При обработке результата команды произошла ошибка десериализации", ex);
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
                logger.Error($"При обработке результата команды произошла ошибка ({ex.Code})", ex);
                await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, ex.ToDto());
            }
            catch (TargetInvocationException ex)
            {
                nack();
                if (ex.InnerException is SalException sex)
                {
                    logger.Error($"При обработке результата команды произошла ошибка ({sex.Code})", sex);
                    await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, sex.ToDto());
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
                //todo проверить надобность
                HandlerContext.Set(HandlerTypes.Processor, "SyncCommandResultProcessor", rabbitMessage.CorrelationId);
                var transportMessage = ExtractMessage(rabbitMessage);
                var commandResultPayload = ExtractCommandResultPayload(transportMessage);
                HandlerContext.Update(commandResultPayload.ContextInfo);
                salLogger.LogIncoming(commandResultPayload);
                await SyncProcessing(transportMessage, commandResultPayload);
                ack();
            }
            catch (JsonReaderException ex)
            {
                var dto = ex.ToDto();
                logger.Error("При обработке результата команды произошла ошибка десериализации", ex);
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
                logger.Error($"При обработке результата команды произошла ошибка ({ex.Code})", ex);
                await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, ex.ToDto());
            }
            catch (TargetInvocationException ex)
            {
                nack();
                if (ex.InnerException is SalException sex)
                {
                    logger.Error($"При обработке результата команды произошла ошибка ({sex.Code})", sex);
                    await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, sex.ToDto());
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

        protected TransportMessage ExtractMessage(RabbitMessage rabbitMessage)
        {
            var transportMessage = SalSerializer.BinaryDeserialize<TransportMessage>(rabbitMessage.Payload);
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

        protected CommandResultPayload ExtractCommandResultPayload(TransportMessage transportTransportMessage)
        {
            var commandResultPayload = transportTransportMessage.Payload.ConvertValue<CommandResultPayload>();

            if (commandResultPayload.Descriptor == null)
                throw new Exception($"Отсутствует commandPayload.Descriptor | CorrelationId:{transportTransportMessage.CorrelationId}");

            if (string.IsNullOrWhiteSpace(commandResultPayload.Descriptor.CommandName))
                throw new Exception($"Пустой commandPayload.Descriptor.CommandName | CorrelationId:{transportTransportMessage.CorrelationId}");

            if (commandResultPayload.Payload == null)
                throw new Exception($"Отсутствует commandPayload.Payload | CorrelationId:{transportTransportMessage.CorrelationId}");


            if (commandResultPayload.Descriptor.ResultAdapterType != AdapterConfiguration.AdapterType)
                throw new Exception($"Не соответствие Descriptor.ResultAdapterType и AdapterType для команды CorrelationId:{transportTransportMessage.CorrelationId}");


            return commandResultPayload;
        }

        private async Task Processing(TransportMessage transportMessage, CommandResultPayload commandResultPayload)
        {
            var commandResLogger = salLogger.GetLogger(commandResultPayload);

            if (commandResultPayload.Descriptor.TTL.HasValue &&
                commandResultPayload.Descriptor.PublishTimeStamp + commandResultPayload.Descriptor.TTL.Value <= DateTime.UtcNow)
            {
                commandResLogger.Trace("Результат команды - протух");
                return;
            }

            HandlerContext.Update(HandlerTypes.CommandResultHandler, commandResultPayload.Descriptor.CommandName);

            using var scope = container.BeginLifetimeScope();


            var executingContext = new ExecutingContext
            {
                Scope = scope,
                SalClient = scope.Resolve<ISalClient>(),
                Logger = commandResLogger
            };

            var context = new CommandResultContext
            {
                Descriptor = commandResultPayload.Descriptor,
                ContextInfo = new ContextInfo
                {
                    SessionId = HandlerContext.SessionId,
                    AuthId = HandlerContext.AuthId,
                    ProcessId = HandlerContext.ProcessId,
                    OperationId = HandlerContext.OperationId
                }
            };


            var isHandled = false;

            if (resultHandlers.TryGetValue(commandResultPayload.Descriptor.CommandName, out var resultCommandHandlersInfo))
            {
                foreach(var rchi in resultCommandHandlersInfo)
                {
                    HandlerContext.Update(handlerName: rchi.HandlerType.Name);
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
                    HandlerContext.Update(handlerName: node.Value.HandlerType.Name);
                    isHandled = await ExecuteResultHandlerAsync(scope, node.Value, commandResultPayload.Payload,
                        context, executingContext);
                    salLogger.LogHandler(commandResultPayload, node.Value.HandlerType.Name, isHandled);
                    node = node.Next;
                }
            }


            if (!isHandled)
            {
                HandlerContext.Update(handlerName: commandResultPayload.Descriptor.CommandName);
                throw SalError.CreateException(SalErrorCodes.NotHandledCommandResult);
            }
        }

        private Task SyncProcessing(TransportMessage transportMessage, CommandResultPayload commandResultPayload)
        {
            if (simpleCommandResultHandlers.TryRemove(commandResultPayload.Descriptor.CorrelationId, out var simpleCommandResultHandler))
            {
                var context = new CommandResultContext
                {
                    Descriptor = commandResultPayload.Descriptor,
                    ContextInfo = new ContextInfo
                    {
                        SessionId = HandlerContext.SessionId,
                        AuthId = HandlerContext.AuthId,
                        ProcessId = HandlerContext.ProcessId,
                        OperationId = HandlerContext.OperationId
                    }
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
            var handler = scope.Resolve(rchi.HandlerType);

            
            if (rchi.IsCommon)
                return ExecuteCommonResultHandlerAsync(handler, result, context, executingContext);
            else
            {
                var commandResultType = typeof(CommandResult<>).MakeGenericType(rchi.ResultType);
                var commandResult = result.ConvertValue(commandResultType);
                return (Task<bool>) rchi.HandlerMethod.Invoke(handler, new[] {commandResult, context, executingContext});
            }
        }

        private Task<bool> ExecuteCommonResultHandlerAsync(object handler, CommonCommandResult commonResult,CommandResultContext context, ExecutingContext executingContext)
        {
            if (handler is ICommonCommandResultHandler2 ccrha)
            {
                return ccrha.ResultHandle(commonResult,context, executingContext);
            }
            else
            {
                throw SalError.CreateException(SalErrorCodes.Fatal, "Обработчик не являеться общим", properties: new {handlerType = handler.GetType().Name});
            }
        }
    }
}