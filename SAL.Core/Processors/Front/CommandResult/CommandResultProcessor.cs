using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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

        private readonly Dictionary<string, LinkedList<CommandResultHandlerInfo>> resultHandlers = new();
        private readonly LinkedList<CommandResultHandlerInfo> anyResultHandlers = new();
        private readonly ConcurrentDictionary<string, SimpleCommandResultHandlerInfo> simpleCommandResultHandlers = new();
        
        private readonly IMetricProvider metricProvider;

        public FrontCommandResultProcessor(ILifetimeScope container, ILoggerProvider loggerProvider, ISalLogger salLogger)
        {
            this.container = container;
            this.loggerProvider = loggerProvider;
            this.salLogger = salLogger;
            logger = loggerProvider.CreateLogger(nameof(FrontCommandResultProcessor));
            salService = container.Resolve<ISalService>();
            metricProvider = container.Resolve<IMetricProvider>();
        }

        public void Start()
        {
            try
            {
                salClient = container.ResolveKeyed<ISalClient>(Contour.Front);

                container.ComponentRegistry.Registrations
                    .Where(r => r.Services.OfType<TypedService>().Any(ts => ts.ServiceType == typeof(ICommandResultHandler2)))
                    .Select(a => a.Activator.LimitType)
                    .ForEach(RegisterCommandResultHandler);

                container.ComponentRegistry.Registrations
                    .Where(r => r.Services.OfType<TypedService>().Any(ts => ts.ServiceType == typeof(ICommonCommandResultHandler2)))
                    .Select(a => a.Activator.LimitType)
                    .ForEach(RegisterCommonCommandResultHandler);

                var configWatcher = container.Resolve<IConfigWatcher>();
                var jsonConfig = configWatcher.GetSection(ConfigurationSectionNames.FrontCommandResultProcessor);
                CommandResultProcessorConfig config = new CommandResultProcessorConfig();

                if (jsonConfig != null)
                {
                    config = jsonConfig.ConvertValue<CommandResultProcessorConfig>();
                }


                var transport = container.ResolveKeyed<ITransport>(Contour.Front);

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
            try
            {
                RegisterCommandResultHandler2(handlerType);
            }
            catch (Exception ex)
            {
                logger.Error($"Ошибка добавления типа {handlerType.Name}", ex);
                throw;
            }
        }

        private void RegisterCommonCommandResultHandler(Type handlerType)
        {
            try
            {
                RegisterCommonCommandResultHandler2(handlerType);
            }
            catch (Exception ex)
            {
                logger.Error($"Ошибка добавления типа {handlerType.Name}", ex);
                throw;
            }
        }

        private void RegisterCommandResultHandler2(Type handlerType)
        {
            var contourAttr = handlerType.GetAttribute<SalContourHandlerAttribute>();
            if (contourAttr?.Contour == Contour.Back)
                return;

            ICommandSchemeCreator schemaCreater = null;
            if (handlerType.IsAssignableTo<ICommandSchemeCreator>())
                schemaCreater = (ICommandSchemeCreator)container.Resolve(handlerType);

            var handlerInterfaces = handlerType.GetInterfaces()
                .Where(i => i.IsAssignableTo<ICommandResultHandler2>() && i.IsGenericType).ToArray();

            foreach(var handlerInterface in handlerInterfaces)
            {
                var resultType = handlerInterface.GetGenericArguments()[0];

                var interfaceMethodInfo = handlerInterface.GetMethod("ResultHandle");
                var handleMethod = handlerType.GetMethodByInterfaceMethodInfo(interfaceMethodInfo);

                var commandName = handleMethod.GetAttribute<SalCommandNameAttribute>()?.Name;

                if (string.IsNullOrWhiteSpace(commandName) && handlerInterfaces.Length == 1)
                {
                    commandName = handlerType.GetAttribute<SalCommandNameAttribute>()?.Name;
                }

                if (string.IsNullOrWhiteSpace(commandName))
                    throw new Exception($"Для {resultType.Name} в {handlerType.Name}  не заданно имя команды (SalCommandNameAttribute)");

                var commandResultHandlerInfo = new CommandResultHandlerInfo
                {
                    CommandName = commandName,
                    ResultType = resultType,
                    HandlerType = handlerType,
                    HandleMethod = handleMethod,
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


                    salService.AddFrontCommandResultHandler(new API.CommandResultHandlerInfo
                    {
                        IsCommon = commandResultHandlerInfo.IsCommon,
                        CommandName = commandResultHandlerInfo.CommandName,
                        ResultSchema = commandResultHandlerInfo.ResultSchema
                    });
                }

                logger.Info($"Для команды {commandName} добавлен обработчик результата {handlerType.Name}");
            }
        }

        private void RegisterCommonCommandResultHandler2(Type handlerType)
        {
            var contourAttr = handlerType.GetAttribute<SalContourHandlerAttribute>();
            if (contourAttr == null)
                return;
            if (contourAttr.Contour == Contour.Back)
                return;


            ICommandSchemeCreator schemaCreater = null;
            if (handlerType.IsAssignableTo<ICommandSchemeCreator>())
                schemaCreater = (ICommandSchemeCreator)container.Resolve(handlerType);


            var attrs = handlerType.GetAttributes<SalCommandNameAttribute>().ToArray();
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
                        HandleMethod = null,
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


                        var handlerInfo = new API.CommandResultHandlerInfo
                        {
                            IsCommon = commandResultHandlerInfo.IsCommon,
                            CommandName = commandName,
                            ResultSchema = commandResultHandlerInfo.ResultSchema
                        };

                        salService.AddFrontCommandResultHandler(handlerInfo);
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

        public void RegisterSimpleCommandResultHandler(string correlationId, TaskCompletionSource<SimpleCommandResult> completionSource, TimeSpan timeOut, bool throwIfTimeout)
        {
            var simpleCommandResultHandler = new SimpleCommandResultHandlerInfo
            {
                CommandCorrelationId = correlationId,
                ExpireDate = DateTime.UtcNow + timeOut,
                CompletionSource = completionSource,
                CancellationTokenSource = new CancellationTokenSource(),
                ThrowIfTimeout = throwIfTimeout
            };

            simpleCommandResultHandlers.TryAdd(correlationId, simpleCommandResultHandler);

            simpleCommandResultHandler.CancellationTokenSource.Token.Register(() =>
            {
                if (simpleCommandResultHandlers.TryRemove(correlationId, out var scrh))
                {
                    if (scrh.ThrowIfTimeout)
                        scrh.CompletionSource.TrySetException(new SalCommandTimeoutException());
                    else
                        scrh.CompletionSource.TrySetResult(new SimpleCommandResult
                        {
                            CommandResultContext = null,
                            CommandResult = new CommonCommandResult
                            {
                                ResultCode = ResultCodes.SalCommandTimeout,
                                Error = null,
                                Result = null
                            }
                        });
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
            var sw = new Stopwatch();
            var isFail = false;
            sw.Start();
            try
            {
                HandlerContext.Set(HandlerTypes.Processor, "FrontCommandResultProcessor", rabbitMessage.CorrelationId);
                var transportMessage = ExtractMessage(rabbitMessage);
                var commandResultPayload = ExtractCommandResultPayload(transportMessage, rabbitMessage.CorrelationId);
                HandlerContext.Update(commandResultPayload.Context.ContextInfo);
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
                isFail = true;
                await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, dto);
            }
            catch (SalException ex)
            {
                nack();
                isFail = true;
                logger.Error($"При обработке результата команды произошла ошибка ({ex.Code})", ex);
                await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, ex.ToDto());
            }
            catch (TargetInvocationException ex)
            {
                nack();
                isFail = true;
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
                isFail = true;
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
            sw.Stop();
            metricProvider.IncCommandResult(sw.Elapsed, isFail);
        }

        private async Task SyncHandler(RabbitMessage rabbitMessage, Action ack, Action nack)
        {
            var sw = new Stopwatch();
            var isFail = false;
            sw.Start();
            try
            {
                //todo проверить надобность
                HandlerContext.Set(HandlerTypes.Processor, "SyncFrontCommandResultProcessor", rabbitMessage.CorrelationId);
                var transportMessage = ExtractMessage(rabbitMessage);
                var commandResultPayload = ExtractCommandResultPayload(transportMessage,rabbitMessage.CorrelationId);
                
                HandlerContext.Update(commandResultPayload.Context.ContextInfo);

                salLogger.LogIncoming(commandResultPayload);
                await SyncProcessing(commandResultPayload);
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
                isFail = true;
                await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, dto);
            }
            catch (SalException ex)
            {
                nack();
                isFail = true;
                logger.Error($"При обработке результата команды произошла ошибка ({ex.Code})", ex);
                await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, ex.ToDto());
            }
            catch (TargetInvocationException ex)
            {
                nack();
                isFail = true;
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
                isFail = true;
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
            sw.Stop();
            metricProvider.IncCommandResult(sw.Elapsed, isFail);
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

        protected CommandResultPayload ExtractCommandResultPayload(TransportMessage transportTransportMessage, string cid)
        {
            var commandResultPayload = transportTransportMessage.Payload.ConvertValue<CommandResultPayload>();

            if (commandResultPayload.Context == null)
                throw new Exception($"Отсутствует CommandResultPayload.Descriptor | CorrelationId:{cid}");

            if (commandResultPayload.Context.Descriptor == null)
                throw new Exception($"Отсутствует CommandResultPayload.Context.Descriptor | CorrelationId:{cid}");

            if (string.IsNullOrWhiteSpace(commandResultPayload.Context.Descriptor.CommandName))
                throw new Exception($"Пустой CommandResultPayload.Context.Descriptor.CommandName | CorrelationId:{cid}");

            if (commandResultPayload.Payload == null)
                throw new Exception($"Отсутствует CommandResultPayload.Payload | CorrelationId:{cid}");

            commandResultPayload.Context.Descriptor.HandleResultTimeStamp = DateTime.UtcNow;
            commandResultPayload.Context.Descriptor.ProcessingDuration = commandResultPayload.Context.Descriptor.HandleResultTimeStamp - commandResultPayload.Context.Descriptor.PublishTimeStamp;
            
            return commandResultPayload;
        }

        private async Task Processing(TransportMessage transportMessage, CommandResultPayload commandResultPayload)
        {
            var commandResLogger = salLogger.GetLogger(commandResultPayload);

            if (commandResultPayload.Context.Descriptor.TTL.HasValue &&
                commandResultPayload.Context.Descriptor.PublishTimeStamp + commandResultPayload.Context.Descriptor.TTL.Value <= DateTime.UtcNow)
            {
                commandResLogger.Trace("Результат команды - протух");
                return;
            }


            HandlerContext.Update(HandlerTypes.FrontCommandResultHandler, commandResultPayload.Context.Descriptor.CommandName);

            using var scope = container.BeginLifetimeScope();


            var executingContext = new ExecutingContext
            {
                Scope = scope,
                SalClient = scope.ResolveKeyed<ISalClient>(Contour.Front),
                Logger = commandResLogger
            };

            var context = commandResultPayload.Context;

            var isHandled = false;

            if (resultHandlers.TryGetValue(commandResultPayload.Context.Descriptor.CommandName, out var resultCommandHandlersInfo))
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
                HandlerContext.Update(handlerName: commandResultPayload.Context.Descriptor.CommandName);
                throw SalError.CreateException(SalErrorCodes.NotHandledCommandResult);
            }
        }

        private Task SyncProcessing(CommandResultPayload commandResultPayload)
        {
            if (simpleCommandResultHandlers.TryRemove(commandResultPayload.Context.Descriptor.CorrelationId, out var simpleCommandResultHandler))
            {
                var context = commandResultPayload.Context;

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
            var handler = (ICommandResultHandler2)scope.Resolve(rchi.HandlerType);


            if (rchi.IsCommon)
            {
                if (handler is ICommonCommandResultHandler2 ccrha)
                {
                    return ccrha.ResultHandle(result, context, executingContext);
                }
                else
                {
                    throw SalError.CreateException(SalErrorCodes.Fatal, "Обработчик не являеться общим", properties: new { handlerType = handler.GetType().Name });
                }
            }

            var commandResultType = typeof(CommandResult<>).MakeGenericType(rchi.ResultType);
            var commandResult = result.ConvertValue(commandResultType);
            return (Task<bool>)rchi.HandleMethod.Invoke(handler, new[] { commandResult, context, executingContext });
        }
    }
}