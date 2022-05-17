using System;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.Core.Processors;
using SAL.Core.Rabbit.Consts;
using SAL.Core.Rabbit.Interfaces;
using SAL.Infrastructure;
using MessageTypes = SAL.API.MessageTypes;

namespace SAL.Core.Client
{
    public class SalClient : ISalClient, ILoSalClient
    {
        private readonly IPublisher publisher;
        private readonly ICommandResultProcessor commandResultProcessor;
        private readonly ISalLogger salLogger;
        public Contour Contour { get; }

        public SalClient(ILifetimeScope scope, ISalLogger salLogger, Contour contour)
        {
            ITransport transport;
            if (contour == Contour.Front)
            {
                transport = scope.ResolveKeyed<ITransport>(contour);
                Contour = contour;
            }
            else if (contour == Contour.Back)
            {
                transport = scope.Resolve<ITransport>();
                Contour = contour;
            }
            else
                throw new SalUnknownContourException();

            this.publisher = transport.CreatePublisher();

            this.commandResultProcessor = contour != Contour.Back ? scope.ResolveKeyed<ICommandResultProcessor>(contour) : scope.Resolve<ICommandResultProcessor>();

            this.salLogger = salLogger;
        }

        
        public async Task<string> PublishCommandAsync(
            string commandName,
            object commandBody,
            string correlationId = null,
            CommandPriority priority = CommandPriority.Normal,
            TimeSpan? ttl = null,
            string handlerAdapterType = null,
            string handlerAdapterName = null,
            string resultAdapterType = null,
            string resultAdapterName = null)
        {
            if (string.IsNullOrWhiteSpace(correlationId))
                correlationId = Guid.NewGuid().ToString("N");

            var commandExchangeName = ExchangeNames.CommandExchange;
            var resultExchangeName = ExchangeNames.CommandResultExchange;

            var commandRoutingKey = commandName;
            var resultRoutingKey = $"{AdapterConfiguration.AdapterType}@{AdapterConfiguration.AdapterName}";

            if (!string.IsNullOrWhiteSpace(handlerAdapterType))
            {
                commandRoutingKey = $"{handlerAdapterType}@{handlerAdapterName}";
            }

            if (!string.IsNullOrWhiteSpace(resultAdapterType))
            {
                resultRoutingKey = $"{resultAdapterType}@{resultAdapterName}";
            }
            
            var commandContext = new CommandContext
            {
                ContextInfo = new ContextInfo(HandlerContext.SessionId, HandlerContext.AuthId, HandlerContext.ProcessId, HandlerContext.OperationId),
                Descriptor = new CommandDescriptor
                {
                    CorrelationId = correlationId,
                    CommandName = commandName,
                    CommandExchangeName = commandExchangeName,
                    CommandRoutingKey = commandRoutingKey,
                    Priority = priority,
                    SourceAdapterType = AdapterConfiguration.AdapterType,
                    SourceAdapterName = AdapterConfiguration.AdapterName,
                    ResultExchangeName = resultExchangeName,
                    ResultRoutingKey = resultRoutingKey,
                    PublishTimeStamp = DateTime.UtcNow,
                    TTL = ttl,
                    Contour = Contour.ToString()
                }
            };
            await LoPublishAsync(commandContext, commandBody);
            return correlationId;
        }
        
        
        public Task<SimpleCommandResult> ExecuteCommandAsync(
            string commandName,
            object commandBody,
            CommandPriority priority = CommandPriority.Normal,
            TimeSpan? ttl = null,
            string handlerAdapterType = null,
            string handlerAdapterName = null,
            bool throwIfTimeout = true)
        {
            ttl ??= TimeSpan.FromSeconds(60);
            
            var correlationId = Guid.NewGuid().ToString("N");

            var commandExchangeName = ExchangeNames.CommandExchange;
            var resultExchangeName = ExchangeNames.CommandResultExchange;

            var commandRoutingKey = commandName;
            var resultRoutingKey = $"!{AdapterConfiguration.AdapterType}@{AdapterConfiguration.AdapterName}";

            if (!string.IsNullOrWhiteSpace(handlerAdapterType))
            {
                commandRoutingKey = $"{handlerAdapterType}@{handlerAdapterName}";
            }
            
            var commandContext = new CommandContext
            {
                ContextInfo = new ContextInfo(HandlerContext.SessionId, HandlerContext.AuthId, HandlerContext.ProcessId, HandlerContext.OperationId),
                Descriptor = new CommandDescriptor
                {
                    CorrelationId = correlationId,
                    CommandName = commandName,
                    CommandExchangeName = commandExchangeName,
                    CommandRoutingKey = commandRoutingKey,
                    Priority = priority,
                    SourceAdapterType = AdapterConfiguration.AdapterType,
                    SourceAdapterName = AdapterConfiguration.AdapterName,
                    ResultExchangeName = resultExchangeName,
                    ResultRoutingKey = resultRoutingKey,
                    PublishTimeStamp = DateTime.UtcNow,
                    TTL = ttl,
                    Contour = Contour.ToString()
                }
            };
            return LoExecuteAsync(commandContext, commandBody, throwIfTimeout);
        }
        
        public async Task<CommandResult<TCommandResult>> ExecuteCommandAsync<TCommandResult>(
            string commandName,
            object commandBody,
            CommandPriority priority = CommandPriority.Normal,
            TimeSpan? ttl = null,
            string handlerAdapterType = null,
            string handlerAdapterName = null,
            bool throwIfTimeout = true
        ) where TCommandResult : class, new()
        {

            var result = await ExecuteCommandAsync(
                commandName,
                commandBody,
                priority,
                ttl,
                handlerAdapterType,
                handlerAdapterName,
                throwIfTimeout);
            return new CommandResult<TCommandResult>(result.CommandResult);
        }
        public Task PublishResultAsync(CommonCommandResult result, CommandContext commandContext)
        {
            return LoPublishResultAsync(commandContext, result);
        }
        public Task PublishResultAsync(object result, CommandContext commandContext)
        {
            return LoPublishResultAsync(commandContext, new CommonCommandResult
            {
                Error = null,
                Result = JObject.FromObject(result, SalSerializer.Create()),
                ResultCode = ResultCodes.Success
            });
        }
        public Task PublishResultAsync(object result, string resultCode, CommandContext commandContext)
        {
            if (resultCode == ResultCodes.Error)
                throw new Exception("ResultCode should not matter 'Error'");
            
            return LoPublishResultAsync(commandContext, new CommonCommandResult
            {
                Error = null,
                Result = JObject.FromObject(result, SalSerializer.Create()),
                ResultCode = resultCode
            });
        }
        public Task PublishResultAsync(InternalExceptionDTO exceptionDto, CommandContext commandContext)
        {
            return LoPublishResultAsync(commandContext, new CommonCommandResult
            {
                Error = exceptionDto,
                Result = null,
                ResultCode = ResultCodes.Error
            });
        }
        public Task PublishResultAsync(Exception exception, string errorCode, CommandContext commandContext)
        {
            if (string.IsNullOrWhiteSpace(errorCode))
                errorCode = SalErrorCodes.Fatal;
            return PublishResultAsync(new CommonCommandResult
            {
                Error = exception.ToDto(errorCode),
                Result = null,
                ResultCode = ResultCodes.Error
            }, commandContext);
        }


        public Task PublishEventAsync(
            string eventName,
            object eventBody,
            TimeSpan? ttl = null,
            string handlerAdapterType = null,
            string handlerAdapterName = null)
        {
            var correlationId = Guid.NewGuid().ToString("N");
            var exchangeName = ExchangeNames.EventExchange;
            var routingKey = eventName;

            if (!string.IsNullOrWhiteSpace(handlerAdapterType))
            {
                exchangeName = !string.IsNullOrWhiteSpace(handlerAdapterName) 
                    ? $"{handlerAdapterType}@{handlerAdapterName}:EventExchange" 
                    : $"{handlerAdapterType}:EventExchange";
            }
            var eventContext = new EventContext
            {
                ContextInfo = new ContextInfo(HandlerContext.SessionId, HandlerContext.AuthId, HandlerContext.ProcessId, HandlerContext.OperationId),
                Descriptor = new EventDescriptor
                {
                    CorrelationId = correlationId,
                    EventName = eventName,
                    ExchangeName = exchangeName,
                    RoutingKey = routingKey.ToString(),
                    SourceAdapterType = AdapterConfiguration.AdapterType,
                    SourceAdapterName = AdapterConfiguration.AdapterName,
                    PublishTimeStamp = DateTime.UtcNow,
                    TTL = ttl,
                    Contour = Contour.ToString()
                }
            };
            return LoPublishAsync(eventContext, eventBody);
        }
        
        public Task RaiseExceptionDetectEvent(string cid, InternalExceptionDTO exceptionDto)
        {
            return PublishEventAsync("System.ExceptionDetected",new ExceptionDetectedEvent
            {
                CorrelationId = cid,
                ExceptionDto = exceptionDto,
                AdapterType = AdapterConfiguration.AdapterType,
                AdapterName = AdapterConfiguration.AdapterName
            });
        }
        
        // lo

        # region private CreateRmqMessage

        private RabbitMessage CreateRmqMessage(CommandPayload commandPayload)
        {
            var transportMessage = new TransportMessage
            {
                Type = MessageTypes.Command,
                Payload = JObject.FromObject(commandPayload, SalSerializer.Create()),
            };

            return new RabbitMessage
            {
                Exchange = commandPayload.Context.Descriptor.CommandExchangeName,
                RoutingKey = commandPayload.Context.Descriptor.CommandRoutingKey,
                Priority = (byte)commandPayload.Context.Descriptor.Priority,
                Payload = SalSerializer.BinarySerialize(transportMessage),
                CorrelationId = commandPayload.Context.Descriptor.CorrelationId,
                TimeStamp = commandPayload.Context.Descriptor.PublishTimeStamp,
            };
        }

        private RabbitMessage CreateRmqMessage(CommandResultPayload commandResultPayload)
        {
            var transportMessage = new TransportMessage
            {
                Type = MessageTypes.CommandResult,
                Payload = JObject.FromObject(commandResultPayload, SalSerializer.Create()),
            };

            return new RabbitMessage
            {
                Exchange = commandResultPayload.Context.Descriptor.ResultExchangeName,
                RoutingKey = commandResultPayload.Context.Descriptor.ResultRoutingKey,
                Priority = (byte)commandResultPayload.Context.Descriptor.Priority,
                Payload = SalSerializer.BinarySerialize(transportMessage),
                CorrelationId = commandResultPayload.Context.Descriptor.CorrelationId,
                TimeStamp = commandResultPayload.Context.Descriptor.PublishResultTimeStamp ?? DateTime.UtcNow,
            };
        }

        private RabbitMessage CreateRmqMessage(EventPayload eventPayload)
        {
            var transportMessage = new TransportMessage
            {
                Type = MessageTypes.Event,
                Payload = JObject.FromObject(eventPayload, SalSerializer.Create()),
            };

            return new RabbitMessage
            {
                Exchange = eventPayload.Context.Descriptor.ExchangeName,
                RoutingKey = eventPayload.Context.Descriptor.RoutingKey,
                Priority = 0,
                Payload = SalSerializer.BinarySerialize(transportMessage),
                CorrelationId = eventPayload.Context.Descriptor.CorrelationId,
                TimeStamp = eventPayload.Context.Descriptor.PublishTimeStamp,
            };
        }

        #endregion

        #region public LoPublish
        public Task LoPublishCommandAsync(
            string commandName,
            object commandBody,
            string correlationId,
            CommandPriority priority,
            TimeSpan? ttl,
            string commandExchangeName,
            string commandRoutingKey,
            string resultExchangeName,
            string resultRoutingKey)
        {
            if (string.IsNullOrWhiteSpace(commandExchangeName))
                throw new ArgumentNullException(nameof(commandExchangeName));

            var commandContext = new CommandContext
            {
                ContextInfo = new ContextInfo(HandlerContext.SessionId, HandlerContext.AuthId, HandlerContext.ProcessId, HandlerContext.OperationId),
                Descriptor = new CommandDescriptor
                {
                    CorrelationId = correlationId,
                    CommandName = commandName,
                    CommandExchangeName = commandExchangeName,
                    CommandRoutingKey = commandRoutingKey,
                    Priority = priority,
                    SourceAdapterType = AdapterConfiguration.AdapterType,
                    SourceAdapterName = AdapterConfiguration.AdapterName,
                    ResultExchangeName = resultExchangeName,
                    ResultRoutingKey = resultRoutingKey,
                    PublishTimeStamp = DateTime.UtcNow,
                    TTL = ttl,
                    Contour = Contour.ToString()
                }
            };
            return LoPublishAsync(commandContext, commandBody);
        }


        public Task<SimpleCommandResult> LoExecuteCommandAsync(
            string commandName,
            object commandBody,
            CommandPriority priority,
            TimeSpan ttl,
            string commandExchangeName,
            string commandRoutingKey,
            bool throwIfTimeout
        )
        {
            if (string.IsNullOrWhiteSpace(commandExchangeName))
                throw new ArgumentNullException(nameof(commandExchangeName));

            var correlationId = Guid.NewGuid().ToString("N");
            var resultExchangeName = ExchangeNames.CommandResultExchange;
            var resultRoutingKey = $"!{AdapterConfiguration.AdapterType}@{AdapterConfiguration.AdapterName}";


            var commandContext = new CommandContext
            {
                ContextInfo = new ContextInfo(HandlerContext.SessionId, HandlerContext.AuthId, HandlerContext.ProcessId, HandlerContext.OperationId),
                Descriptor = new CommandDescriptor
                {
                    CorrelationId = correlationId,
                    CommandName = commandName,
                    CommandExchangeName = commandExchangeName,
                    CommandRoutingKey = commandRoutingKey,
                    Priority = priority,
                    SourceAdapterType = AdapterConfiguration.AdapterType,
                    SourceAdapterName = AdapterConfiguration.AdapterName,
                    ResultExchangeName = resultExchangeName,
                    ResultRoutingKey = resultRoutingKey,
                    PublishTimeStamp = DateTime.UtcNow,
                    TTL = ttl,
                    Contour = Contour.ToString()
                }
            };
            return LoExecuteAsync(commandContext, commandBody, throwIfTimeout);
        }


        public Task LoPublishEventAsync(
            string eventName,
            object eventBody,
            string correlationId,
            TimeSpan? ttl,
            string exchangeName,
            string routingKey)
        {
            if (string.IsNullOrWhiteSpace(exchangeName))
                throw new ArgumentNullException(nameof(exchangeName));


            var eventContext = new EventContext
            {
                ContextInfo = new ContextInfo(HandlerContext.SessionId, HandlerContext.AuthId, HandlerContext.ProcessId, HandlerContext.OperationId),
                Descriptor = new EventDescriptor
                {
                    CorrelationId = correlationId,
                    EventName = eventName,
                    ExchangeName = exchangeName,
                    RoutingKey = routingKey,
                    SourceAdapterType = AdapterConfiguration.AdapterType,
                    SourceAdapterName = AdapterConfiguration.AdapterName,
                    PublishTimeStamp = DateTime.UtcNow,
                    TTL = ttl,
                    Contour = Contour.ToString()
                }
            };
            return LoPublishAsync(eventContext, eventBody);
        }

        public async Task<SimpleCommandResult> LoExecuteAsync(CommandContext commandContext, object commandBody, bool throwIfTimeout)
        {
            if (!commandContext.Descriptor.TTL.HasValue)
                throw new ArgumentNullException(nameof(commandContext.Descriptor.TTL));

            var completionSource = new TaskCompletionSource<SimpleCommandResult>();
            commandResultProcessor.RegisterSimpleCommandResultHandler(commandContext.Descriptor.CorrelationId, completionSource,
                commandContext.Descriptor.TTL.Value, throwIfTimeout);

            await LoPublishAsync(commandContext, commandBody);
            return await completionSource.Task;
        }

        public Task LoPublishResultAsync(CommandContext commandContext, CommonCommandResult result)
        {
            var commandResultContext = new CommandResultContext
            {
                ContextInfo = commandContext.ContextInfo with { },
                Descriptor = new CommandResultDescriptor(commandContext.Descriptor)
                {
                    HandlerAdapterType = AdapterConfiguration.AdapterType,
                    HandlerAdapterName = AdapterConfiguration.AdapterName,
                    Contour = Contour.ToString()
                }
            };
            return LoPublishAsync(commandResultContext, result);
        }

        public Task LoPublishAsync(CommandContext commandContext, object commandBody)
        {
            var commandPayload = new CommandPayload
            {
                Context = commandContext,
                Payload = JObject.FromObject(commandBody, SalSerializer.Create())
            };

            salLogger.LogOutgoing(commandPayload);

            return publisher.PublishAsync(CreateRmqMessage(commandPayload));
        }

        public Task LoPublishAsync(CommandResultContext commandResultContext, CommonCommandResult result)
        {
            TimeSpan? ttl = null;
            ttl = commandResultContext.Descriptor.PublishTimeStamp + commandResultContext.Descriptor.TTL - DateTime.UtcNow;

            if (ttl is null || ttl > TimeSpan.Zero && !string.IsNullOrEmpty(commandResultContext.Descriptor.ResultExchangeName))
            {
                commandResultContext.Descriptor.PublishResultTimeStamp = DateTime.UtcNow;
                if (commandResultContext.Descriptor.HandlerTimeStamp.HasValue)
                    commandResultContext.Descriptor.HandlerDuration = commandResultContext.Descriptor.PublishResultTimeStamp.Value - commandResultContext.Descriptor.HandlerTimeStamp.Value;

                var commandResultPayload = new CommandResultPayload
                {
                    Context = commandResultContext,
                    Payload = result
                };

                salLogger.LogOutgoing(commandResultPayload);

                return publisher.PublishAsync(CreateRmqMessage(commandResultPayload));
            }
            else
            {
                salLogger.LogNullOutgoing(commandResultContext.Descriptor);
            }

            return Task.CompletedTask;
        }

        public Task LoPublishAsync(EventContext eventContext, object eventBody)
        {
            var eventPayload = new EventPayload()
            {
                Context = eventContext,
                Payload = JObject.FromObject(eventBody, SalSerializer.Create()),
            };

            salLogger.LogOutgoing(eventPayload);

            return publisher.PublishAsync(CreateRmqMessage(eventPayload));
        }

        #endregion
    }
}