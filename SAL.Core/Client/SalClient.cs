using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.Core.Helpers;
using SAL.Core.Processors;
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


        public async Task<string> PublishCommandAsync<TCommand>(
            TCommand command,
            string correlationId = null,
            CommandPriority priority = CommandPriority.Normal,
            TimeSpan? ttl = null,
            string handlerServiceType = null,
            string handlerServiceName = null,
            bool typeHandler = false)
        
            where TCommand : class, new()
        {
            if (Contour == Contour.Front)
                throw new ContourNotSupportedException();
            
            if (string.IsNullOrWhiteSpace(correlationId))
                correlationId = Guid.NewGuid().ToString("N");

            var commandType = command.GetType();

            var resultServiceType = AdapterConfiguration.AdapterType;
            var resultServiceName = typeHandler ? null : AdapterConfiguration.AdapterName;
            
            await PublishCommandAsync(
                commandType.GetSalName(),
                command,
                correlationId,
                priority,
                ttl,
                handlerServiceType,
                handlerServiceName,
                resultServiceType,
                resultServiceName
            );

            return correlationId;
        }

        public async Task<string> PublishCommandAsync(
            string commandName, 
            object command, 
            string correlationId = null, 
            CommandPriority priority = CommandPriority.Normal, 
            TimeSpan? ttl = null, 
            string handlerServiceType = null, 
            string handlerServiceName = null, 
            bool typeHandler = false)
        {
            if (string.IsNullOrWhiteSpace(correlationId))
                correlationId = Guid.NewGuid().ToString("N");


            
            var resultServiceType = AdapterConfiguration.AdapterType;
            var resultServiceName = typeHandler ? null : AdapterConfiguration.AdapterName;
            
            await PublishCommandAsync(
                commandName,
                command,
                correlationId,
                priority,
                ttl,
                handlerServiceType,
                handlerServiceName,
                resultServiceType,
                resultServiceName
            );

            return correlationId;
        }

        public async Task<CommandResult<TCommandResult>> ExecuteCommandAsync<TCommand, TCommandResult>(
            TCommand command,
            CommandPriority priority = CommandPriority.Normal,
            TimeSpan? ttl = null,
            string handlerServiceType = null,
            string handlerServiceName = null
        ) where TCommand : class, IHaveResult<TCommandResult>, new() where TCommandResult : class, ICommandResult, new()
        {
            ttl ??= TimeSpan.FromSeconds(60);

            var commandType = command.GetType();
            var result = await ExecuteCommandAsync(
                commandType.GetSalName(),
                command,
                priority,
                ttl.Value,
                true,
                handlerServiceType,
                handlerServiceName
            );

            return new CommandResult<TCommandResult>(result.CommandResult);
        }

        public async Task<CommandResult<TCommandResult>> ExecuteCommandAsync<TCommandResult>(
            string commandName, 
            object command, 
            CommandPriority priority = CommandPriority.Normal, 
            TimeSpan? ttl = null,
            bool throwIfTimeout = true,
            string handlerServiceType = null, 
            string handlerServiceName = null) where TCommandResult : class, new()
        {
            ttl ??= TimeSpan.FromSeconds(60);
            
            var result = await ExecuteCommandAsync(
                commandName,
                command,
                priority,
                ttl.Value,
                throwIfTimeout,
                handlerServiceType,
                handlerServiceName
            );
            
            return new CommandResult<TCommandResult>(result.CommandResult);
        }


        public Task PublishResultAsync(ICommandResult result, CommandContext commandContext)
        {
            return PublishResultAsync(new CommonCommandResult
            {
                Error = null,
                Result = JObject.FromObject(result, SalSerializer.Create()),
                ResultCode = ResultCodes.Success
            }, commandContext);
        }

        public Task PublishResultAsync(object result, string resultCode, CommandContext commandContext)
        {
            if (resultCode == ResultCodes.Error)
            {
                throw new Exception("ResultCode should not matter 'Error'");
            }
            
            return PublishResultAsync(new CommonCommandResult
            {
                Error = null,
                Result = JObject.FromObject(result, SalSerializer.Create()),
                ResultCode = resultCode
            }, commandContext);
        }

        public Task PublishResultAsync(InternalExceptionDTO exceptionDTO, CommandContext commandContext)
        {
            return PublishResultAsync(new CommonCommandResult
            {
                Error = exceptionDTO,
                Result = null,
                ResultCode = ResultCodes.Error
            }, commandContext);
        }

        public Task PublishResultAsync(Exception exception, string code , CommandContext commandContext)
        {
            if (string.IsNullOrWhiteSpace(code))
                code = SalErrorCodes.Fatal;
            return PublishResultAsync(new CommonCommandResult
            {
                Error = exception.ToDto(code),
                Result = null,
                ResultCode = ResultCodes.Error
            }, commandContext);
        }

        public Task PublishResultAsync(IList<FieldError> validationErrors, CommandContext commandContext)
        {
            return PublishResultAsync(new CommonCommandResult
            {
                Error = SalError.CreateValidationDto(validationErrors),
                Result = null,
                ResultCode = ResultCodes.Error
            }, commandContext);
        }

        public Task PublishResultAsync<TCommandResult>(CommandResult<TCommandResult> result, CommandContext commandContext) where TCommandResult : class, ICommandResult, new()
        {
            if (typeof(TCommandResult) == typeof(None))
                return Task.CompletedTask;

            return PublishResultAsync(new CommonCommandResult
            {
                Error = result.Error,
                Result = result.Result.Clone(),
                ResultCode = result.ResultCode
            }, commandContext);
        }


        public Task PublishEventAsync(IEvent evnt, TimeSpan? ttl = null, string handlerServiceType = null, string handlerServiceName = null)
        {
            if (evnt == null)
                return Task.CompletedTask;

            return PublishEventAsync(
                evnt.GetType().GetSalName(),
                evnt,
                Guid.NewGuid().ToString("N"),
                ttl,
                handlerServiceType,
                handlerServiceName, 
                false
            );
        }

        public Task PublishCEventAsync(IEvent evnt, string handlerServiceType, TimeSpan? ttl = null)
        {
            if (evnt == null)
                return Task.CompletedTask;

            return PublishEventAsync(
                evnt.GetType().GetSalName(),
                evnt,
                Guid.NewGuid().ToString("N"),
                ttl,
                handlerServiceType,
                null, 
                true
            );
        }

        public Task PublishEventAsync(string eventName, object evnt,  TimeSpan? ttl = null, string handlerServiceType = null, string handlerServiceName = null)
        {
            if (evnt == null)
                return Task.CompletedTask;

            return PublishEventAsync(
                eventName,
                evnt,
                Guid.NewGuid().ToString("N"),
                ttl,
                handlerServiceType,
                handlerServiceName,
                false);
        }

        public Task PublishCEventAsync(string eventName, object evnt, string handlerServiceType, TimeSpan? ttl = null)
        {
            if (evnt == null)
                return Task.CompletedTask;

            return PublishEventAsync(
                eventName,
                evnt,
                Guid.NewGuid().ToString("N"),
                ttl,
                handlerServiceType,
                null, 
                true
            );
        }


        public Task RaiseExceptionDetectEvent(string cid, InternalExceptionDTO exceptionDTO)
        {
            return PublishEventAsync(new ExceptionDetectedEvent
            {
                CorrelationId = cid,
                ExceptionDto = exceptionDTO,
                AdapterType = AdapterConfiguration.AdapterType,
                AdapterName = AdapterConfiguration.AdapterName
            });
        }

        public Task RaiseExceptionDetectEvent(string cid, Exception ex)
        {
            return PublishEventAsync(new ExceptionDetectedEvent
            {
                CorrelationId = cid,
                ExceptionDto = ex.ToDto(),
                AdapterType = AdapterConfiguration.AdapterType,
                AdapterName = AdapterConfiguration.AdapterName
            });
        }

        //lo

        private RabbitMessage Pack(TransportMessage transportTransportMessage)
        {
            return new RabbitMessage
            {
                Priority = transportTransportMessage.Priority,
                Payload = SalSerializer.BinarySerialize(transportTransportMessage),
                CorrelationId = transportTransportMessage.CorrelationId,
                TimeStamp = transportTransportMessage.TimeStamp,
                RoutingKey = transportTransportMessage.Destination
            };
        }

        public async Task<string> PublishCommandAsync(string commandName, object commandBody)
        {
            var correlationId = Guid.NewGuid().ToString("N");
            await PublishCommandAsync(commandName, commandBody, correlationId,
                CommandPriority.Normal, null,
                null, null,
                AdapterConfiguration.AdapterType, AdapterConfiguration.AdapterName);

            return correlationId;
        }

        public Task PublishCommandAsync(
            string commandName,
            object commandBody,
            string correlationId,
            CommandPriority priority,
            TimeSpan? ttl,
            string handlerAdapterType,
            string handlerAdapterName,
            string resultAdapterType,
            string resultAdapterName)
        {
            if (string.IsNullOrWhiteSpace(commandName))
                throw new ArgumentNullException(nameof(commandName));

            if (string.IsNullOrWhiteSpace(correlationId))
                throw new ArgumentNullException(nameof(correlationId));

            if (string.IsNullOrWhiteSpace(resultAdapterType))
                throw new ArgumentNullException(nameof(resultAdapterType));


            if (commandBody == null)
                throw new ArgumentNullException(nameof(commandBody));


            var commandContext = new CommandContext
            {
                ContextInfo = new ContextInfo
                {
                    SessionId = HandlerContext.SessionId,
                    AuthId = HandlerContext.AuthId,
                    ProcessId = HandlerContext.ProcessId,
                    OperationId = HandlerContext.OperationId
                },
                Descriptor = new CommandDescriptor
                {
                    CorrelationId = correlationId,
                    CommandName = commandName,
                    Priority = priority,
                    SourceAdapterType = AdapterConfiguration.AdapterType,
                    SourceAdapterName = AdapterConfiguration.AdapterName,
                    DestinationAdapterType = handlerAdapterType,
                    DestinationAdapterName = handlerAdapterName,
                    ResultAdapterType = resultAdapterType,
                    ResultAdapterName = resultAdapterName,
                    PublishTimeStamp = DateTime.UtcNow,
                    TTL = ttl,
                    IsSync = false,
                    Contour = Contour.ToString()
                }
            };
            


            return PublishCommandAsync(commandContext, JObject.FromObject(commandBody, SalSerializer.Create()));
        }

        public async Task<SimpleCommandResult> ExecuteCommandAsync(
            string commandName,
            object commandBody,
            CommandPriority priority,
            TimeSpan ttl,
            bool throwIfTimeout,
            string handlerAdapterType,
            string handlerAdapterName)
        {
            if (string.IsNullOrWhiteSpace(commandName))
                throw new ArgumentNullException(nameof(commandName));


            var correlationId = Guid.NewGuid().ToString("N");

            var routingKey = "";
            if (string.IsNullOrWhiteSpace(handlerAdapterType) && string.IsNullOrWhiteSpace(handlerAdapterName))
                routingKey = commandName;
            else
                routingKey = $"{handlerAdapterType}#{handlerAdapterName}";


            var commandDescriptor = new CommandDescriptor
            {
                CorrelationId = correlationId,
                CommandName = commandName,
                Priority = priority,
                DestinationAdapterType = handlerAdapterType,
                DestinationAdapterName = handlerAdapterName,
                SourceAdapterType = AdapterConfiguration.AdapterType,
                SourceAdapterName = AdapterConfiguration.AdapterName,
                ResultAdapterType = AdapterConfiguration.AdapterType,
                ResultAdapterName = AdapterConfiguration.AdapterName,
                PublishTimeStamp = DateTime.UtcNow,
                TTL = ttl,
                IsSync = true,
                Contour = Contour.ToString()
            };


            var commandContext = new ContextInfo
            {
                SessionId = HandlerContext.SessionId,
                AuthId = HandlerContext.AuthId,
                ProcessId = HandlerContext.ProcessId,
                OperationId = HandlerContext.OperationId
            };
            
            var commandPayload = new CommandPayload
            {
                Descriptor = commandDescriptor,
                Payload = JObject.FromObject(commandBody, SalSerializer.Create()),
                ContextInfo = commandContext

            };

            return await ExecuteCommandAsync(commandPayload, routingKey, throwIfTimeout);
        }

        public async Task<SimpleCommandResult> ExecuteExternalHttp(
            ExternalHttpRequest request,
            string routePath,
            TimeSpan ttl,
            bool throwIfTimeout,
            string handlerAdapterType,
            string handlerAdapterName)
        {
            var correlationId = Guid.NewGuid().ToString("N");

            var routingKey = routePath;
            request.BasePath = routePath;

            var commandDescriptor = new CommandDescriptor
            {
                CorrelationId = correlationId,
                CommandName = "ExternalHttp",
                Priority = CommandPriority.Normal,
                DestinationAdapterType = handlerAdapterType,
                DestinationAdapterName = handlerAdapterName,
                SourceAdapterType = AdapterConfiguration.AdapterType,
                SourceAdapterName = AdapterConfiguration.AdapterName,
                ResultAdapterType = AdapterConfiguration.AdapterType,
                ResultAdapterName = AdapterConfiguration.AdapterName,
                PublishTimeStamp = DateTime.UtcNow,
                TTL = ttl,
                IsSync = true,
                Contour = Contour.ToString()
            };
            
            var commandContext = new ContextInfo
            {
                SessionId = HandlerContext.SessionId,
                AuthId = HandlerContext.AuthId,
                ProcessId = HandlerContext.ProcessId,
                OperationId = HandlerContext.OperationId
            };

            var commandPayload = new CommandPayload
            {
                Descriptor = commandDescriptor,
                Payload = JObject.FromObject(request, SalSerializer.Create()),
                ContextInfo = commandContext
            };

            return await ExecuteCommandAsync(commandPayload, routingKey, throwIfTimeout);
        }


        public async Task<SimpleCommandResult> ExecuteCommandAsync(CommandPayload commandPayload, string routingKey, bool throwIfTimeout)
        {
            commandPayload.Descriptor.TTL ??= TimeSpan.FromMinutes(1);

            salLogger.LogOutgoing(commandPayload);

            var transportMessage = new TransportMessage
            {
                Type = MessageTypes.Command,
                Payload = JObject.FromObject(commandPayload, SalSerializer.Create()),
                Source = $"{commandPayload.Descriptor.SourceAdapterType}.{commandPayload.Descriptor.SourceAdapterName}",
                Priority = (byte) commandPayload.Descriptor.Priority,
                TimeStamp = commandPayload.Descriptor.PublishTimeStamp,
                Destination = routingKey,
                TTL = commandPayload.Descriptor.TTL,
                CorrelationId = commandPayload.Descriptor.CorrelationId,
            };

            var completionSource = new TaskCompletionSource<SimpleCommandResult>();

            commandResultProcessor.RegisterSimpleCommandResultHandler(commandPayload.Descriptor.CorrelationId, completionSource,
                commandPayload.Descriptor.TTL.Value, throwIfTimeout);

            publisher.PublishCommand(Pack(transportMessage));

            return await completionSource.Task;
        }

        public Task PublishCommandAsync(CommandContext commandContext, JObject commandBody)
        {
            var routingKey = "";
            if (string.IsNullOrWhiteSpace(commandContext.Descriptor.DestinationAdapterType))
                routingKey = commandContext.Descriptor.CommandName;
            else if (string.IsNullOrWhiteSpace(commandContext.Descriptor.DestinationAdapterName))
                routingKey = commandContext.Descriptor.DestinationAdapterType;
            else
                routingKey = $"{commandContext.Descriptor.DestinationAdapterType}#{commandContext.Descriptor.DestinationAdapterName}";

            var commandPayload = new CommandPayload
            {
                Descriptor = commandContext.Descriptor,
                Payload = commandBody.Clone(),
                ContextInfo = commandContext.ContextInfo
            };

            salLogger.LogOutgoing(commandPayload);

            var transportMessage = new TransportMessage
            {
                Type = MessageTypes.Command,
                Payload = JObject.FromObject(commandPayload, SalSerializer.Create()),
                Source = $"{commandContext.Descriptor.SourceAdapterType}.{commandContext.Descriptor.SourceAdapterName}",
                Priority = (byte) commandContext.Descriptor.Priority,
                TimeStamp = commandContext.Descriptor.PublishTimeStamp,
                Destination = routingKey,
                TTL = commandContext.Descriptor.TTL,
                CorrelationId = commandContext.Descriptor.CorrelationId,
            };
            publisher.PublishCommand(Pack(transportMessage));
            return Task.CompletedTask;
        }

        public Task PublishResultAsync(CommonCommandResult result, CommandContext commandContext)
        {
            var commandResultContext = new CommandResultContext(commandContext);

            commandResultContext.Descriptor.HandlerAdapterType = AdapterConfiguration.AdapterType;
            commandResultContext.Descriptor.HandlerAdatpterName = AdapterConfiguration.AdapterName;
            commandResultContext.Descriptor.Contour = Contour.ToString();

            return PublishResultAsync(commandResultContext, result);
        }

        public Task PublishResultAsync(CommandResultContext commandResultContext, CommonCommandResult result)
        {
            TimeSpan? ttl = null;

            if (commandResultContext.Descriptor.TTL.HasValue && commandResultContext.Descriptor.IsSync)
            {
                ttl = commandResultContext.Descriptor.PublishTimeStamp + commandResultContext.Descriptor.TTL.Value - DateTime.UtcNow;
            }

            if (ttl == null || ttl > TimeSpan.Zero)
            {
                if (commandResultContext.Descriptor.HandlerTimeStamp.HasValue)
                    commandResultContext.Descriptor.HandlerDuration = DateTime.UtcNow - commandResultContext.Descriptor.HandlerTimeStamp.Value;


                var routingKey = "";

                if (commandResultContext.Descriptor.IsSync)
                {
                    routingKey = $"{commandResultContext.Descriptor.ResultAdapterType}#{commandResultContext.Descriptor.ResultAdapterName}#Sync";
                }
                else
                {
                    routingKey = string.IsNullOrWhiteSpace(commandResultContext.Descriptor.ResultAdapterName) ? commandResultContext.Descriptor.ResultAdapterType : $"{commandResultContext.Descriptor.ResultAdapterType}#{commandResultContext.Descriptor.ResultAdapterName}";
                }

                var commandResultPayload = new CommandResultPayload
                {
                    Descriptor = commandResultContext.Descriptor,
                    Payload = result,
                    ContextInfo = commandResultContext.ContextInfo
                    
                };

                salLogger.LogOutgoing(commandResultPayload);

                var transportMessage = new TransportMessage
                {
                    Type = MessageTypes.CommandResult,
                    Payload = JObject.FromObject(commandResultPayload, SalSerializer.Create()),
                    Source = $"{AdapterConfiguration.AdapterType}.{AdapterConfiguration.AdapterName}",
                    Priority = (byte) commandResultContext.Descriptor.Priority,
                    TimeStamp = commandResultContext.Descriptor.PublishTimeStamp,
                    Destination = routingKey,
                    TTL = ttl,
                    CorrelationId = commandResultContext.Descriptor.CorrelationId,
                };

                publisher.PublishCommandResult(Pack(transportMessage));
            }
            else
            {
                salLogger.LogNullOutgoing(commandResultContext.Descriptor);
            }

            return Task.CompletedTask;
        }


        public Task PublishEventAsync(string eventName, object eventBody, string correlationId, TimeSpan? ttl, string handlerServiceType, string handlerServiceName, bool isCEvent)
        {
            var eventContext = new EventContext
            {
                ContextInfo = new ContextInfo
                {
                    SessionId = HandlerContext.SessionId,
                    AuthId = HandlerContext.AuthId,
                    ProcessId = HandlerContext.ProcessId,
                    OperationId = HandlerContext.OperationId
                },
                Descriptor = new EventDescriptor
                {
                    CorrelationId = correlationId,
                    EventName = eventName,
                    DestinationAdapterType = handlerServiceType,
                    DestinationAdapterName = handlerServiceName,
                    SourceAdapterType = AdapterConfiguration.AdapterType,
                    SourceAdapterName = AdapterConfiguration.AdapterName,
                    PublishTimeStamp = DateTime.UtcNow,
                    TTL = ttl,
                    Contour = Contour.ToString(),
                    IsSystem = string.Equals(eventName.Split('.').First(), "System", StringComparison.InvariantCultureIgnoreCase),
                    IsCEvent = isCEvent
                }
            };


            return PublishEventAsync(eventContext, JObject.FromObject(eventBody, SalSerializer.Create()));
        }

        public Task PublishEventAsync(EventContext eventContext, JObject eventBody)
        {
            var eventPayload = new EventPayload()
            {
                Descriptor = eventContext.Descriptor,
                Payload = eventBody,
                ContextInfo = eventContext.ContextInfo
            };

            salLogger.LogOutgoing(eventPayload);

            string routingKey;


            if (string.IsNullOrWhiteSpace(eventContext.Descriptor.DestinationAdapterType))
                routingKey = eventContext.Descriptor.EventName;
            else if (eventContext.Descriptor.IsCEvent)
            {
                routingKey = eventContext.Descriptor.DestinationAdapterType;
            }
            else
            {
                routingKey = string.IsNullOrWhiteSpace(eventContext.Descriptor.DestinationAdapterName)
                    ? eventContext.Descriptor.DestinationAdapterType
                    : $"{eventContext.Descriptor.DestinationAdapterType}#{eventContext.Descriptor.DestinationAdapterName}";

                if (eventContext.Descriptor.IsSystem)
                {
                    routingKey = $"System#{routingKey}";
                }
            }
            


            var transportMessage = new TransportMessage
            {
                Type = MessageTypes.Event,
                Payload = JObject.FromObject(eventPayload, SalSerializer.Create()),
                Source = $"{eventContext.Descriptor.SourceAdapterType}.{eventContext.Descriptor.SourceAdapterName}",
                Priority = 0,
                TimeStamp = eventContext.Descriptor.PublishTimeStamp,
                Destination = routingKey,
                TTL = eventContext.Descriptor.TTL,
                CorrelationId = eventContext.Descriptor.CorrelationId,
            };

            if(!string.IsNullOrWhiteSpace(eventContext.Descriptor.DestinationAdapterType) && eventContext.Descriptor.IsCEvent)
                publisher.PublishCEvent(Pack(transportMessage));
            else 
                publisher.PublishEvent(Pack(transportMessage));

            return Task.CompletedTask;
        }
    }
}