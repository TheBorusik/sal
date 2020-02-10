using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.API.Client;
using SAL.Core.DTO.Transport;
using SAL.Core.Helpers;
using SAL.Core.Processors;
using SAL.Core.Rabbit.Interfaces;
using SAL.Infrastructure;

namespace SAL.Core.Client
{
    public class SalClient : ISalClient, ILoSalClient
    {
        private IPublisher publisher;
        private ICommandResultProcessor commandResultProcessor;
        private ISalLogger salLogger;


        public SalClient(IPublisher publisher, ICommandResultProcessor commandResultProcessor, ISalLogger salLogger)
        {
            this.publisher = publisher;
            this.commandResultProcessor = commandResultProcessor;
            this.salLogger = salLogger;
        }


        public async Task<string> PublishCommandAsync<TCommand>(
            TCommand command,
            string correlationId = null,
            CommandPriority priority = CommandPriority.Normal,
            TimeSpan? ttl = null,
            string handlerServiceName = null,
            string resultServiceType = null,
            string resultServiceName = null
            )

            where TCommand : class, ICommand, new()
        {

            if (string.IsNullOrWhiteSpace(correlationId))
                correlationId = Guid.NewGuid().ToString("N");

            var commandType = command.GetType();

            if (string.IsNullOrWhiteSpace(resultServiceType))
                resultServiceType = ServiceConfiguration.AdapterType;

            if (string.IsNullOrWhiteSpace(resultServiceName) && !commandType.IsResultTypeHandler())
                resultServiceName = ServiceConfiguration.AdapterName;

            await PublishCommandAsync(
                commandType.GetSourceTypeName(),
                commandType.GetSourceName(),
                command,
                correlationId,
                priority,
                ttl,
                handlerServiceName,
                resultServiceType,
                resultServiceName
            );

            return correlationId;
        }

        public async Task<CommandResult<TCommandResult>> ExecuteCommandAsync<TCommand, TCommandResult>(
            TCommand command,
            CommandPriority priority = CommandPriority.Normal,
            int ttls = 60,
            string handlerServiceName = null
        ) where TCommand : class, IHaveResult<TCommandResult>, new() where TCommandResult : class, ICommandResult, new()
        {
            var commandType = command.GetType();
            var result = await ExecuteCommandAsync(
                commandType.GetSourceTypeName(),
                commandType.GetSourceName(),
                command,
                priority,
                ttls,
                handlerServiceName
                );

            return result.ConvertValue<CommandResult<TCommandResult>>();


        }

        public Task PublishResultAsync(ICommandResult result, CommandDescriptor commandDescriptor)
        {
            return PublishResultAsync(new CommonCommandResult
            {
                Error = null,
                Result = JObject.FromObject(result),
                ResultCode = ResultCodes.ResultOK
            }, commandDescriptor);
        }

        public Task PublishResultAsync(InternalExceptionDTO exceptionDTO, string resultCode, CommandDescriptor commandDescriptor)
        {
            return PublishResultAsync(new CommonCommandResult
            {
                Error = exceptionDTO,
                Result = null,
                ResultCode = resultCode
            }, commandDescriptor);
        }

        public Task PublishResultAsync(IList<FieldError> validationErrors, CommandDescriptor commandDescriptor)
        {
            return PublishResultAsync(new CommonCommandResult
            {
                Error = SalError.CreateValidationDto(validationErrors),
                Result = null,
                ResultCode = ResultCodes.ValidationFailed
            }, commandDescriptor);

        }

        public Task PublishResultAsync<TCommandResult>(CommandResult<TCommandResult> result, CommandDescriptor commandDescriptor) where TCommandResult : class, ICommandResult, new()
        {
            return PublishResultAsync(new CommonCommandResult
            {
                Error = null,
                Result = JObject.FromObject(result),
                ResultCode = ResultCodes.ResultOK
            }, commandDescriptor);
        }


        public Task PublishEventAsync(IEvent evnt, TimeSpan? ttl = null, string handlerServiceType = null, string handlerServiceName = null)
        {
            var eventType = evnt.GetType();
            return PublishEventAsync(
                eventType.GetSourceName(),
                evnt,
                ttl,
                handlerServiceType,
                handlerServiceName
            );
        }

        public Task RaiseExceptionDetectEvent(InternalExceptionDTO exceptionDTO)
        {
            return PublishEventAsync(new ExceptionDetectedEvent
            {
                ExceptionDto = exceptionDTO,
                ServiceName = ServiceConfiguration.AdapterName,
                ServiceType = ServiceConfiguration.AdapterType
            });
        }

        public Task RaiseExceptionDetectEvent(Exception ex)
        {
            return PublishEventAsync(new ExceptionDetectedEvent
            {
                ExceptionDto = ex.ToDto(SalErrorCodes.Fatal),
                ServiceName = ServiceConfiguration.AdapterName,
                ServiceType = ServiceConfiguration.AdapterType
            });
        }


        private RabbitMessage Pack(Message transportMessage)
        {
            return new RabbitMessage
            {
                Priority = transportMessage.Priority,
                Payload = SalSerializer.BinarySerialize(transportMessage),
                CorrelationId = transportMessage.CorrelationId,
                TimeStamp = transportMessage.TimeStamp,
                RoutingKey = transportMessage.Destination
            };
        }



        //lo
        public Task PublishCommandAsync(
            string handlerServiceType,
            string commandName,
            object commandBody,
            string correlationId,
            CommandPriority priority,
            TimeSpan? ttl,
            string handlerServiceName,
            string resultServiceType,
            string resultServiceName)
        {
            if (string.IsNullOrWhiteSpace(handlerServiceType))
                throw new ArgumentNullException(nameof(handlerServiceType));

            if (string.IsNullOrWhiteSpace(correlationId))
                throw new ArgumentNullException(nameof(correlationId));

            if (string.IsNullOrWhiteSpace(resultServiceType))
                throw new ArgumentNullException(nameof(resultServiceType));

            if (string.IsNullOrWhiteSpace(commandName))
                throw new ArgumentNullException(nameof(commandName));

            if (commandBody == null)
                throw new ArgumentNullException(nameof(commandBody));

            var routingKey = "";
            if (string.IsNullOrWhiteSpace(handlerServiceName))
                routingKey = $"{handlerServiceType}.{commandName}";
            else
                routingKey = $"{handlerServiceType}#{handlerServiceName}";

            var commandDescriptor = new CommandDescriptor
            {
                CorrelationId = correlationId,
                ServiceType = handlerServiceType,
                ServiceName = handlerServiceName,
                CommandName = commandName,
                Priority = priority,
                SourceServiceType = ServiceConfiguration.AdapterType,
                SourceServiceName = ServiceConfiguration.AdapterName,
                ResultServiceType = resultServiceType,
                ResultServiceName = resultServiceName,
                PublishTimeStamp = DateTime.UtcNow,
                TTL = ttl,
                IsSync = false
            };

            var commandPayload = new CommandPayload
            {
                Descriptor = commandDescriptor,
                Body = JObject.FromObject(commandBody)
            };

            salLogger.LogOutgoing(commandPayload);

            var transportMessage = new Message
            {
                Type = MessageTypes.Command,
                Payload = JObject.FromObject(commandPayload),
                Source = $"{commandDescriptor.SourceServiceType}.{commandDescriptor.SourceServiceName}",
                Priority = (byte)commandDescriptor.Priority,
                TimeStamp = commandDescriptor.PublishTimeStamp,
                Destination = routingKey,
                TTL = commandDescriptor.TTL,
                CorrelationId = correlationId,
                Session = SessionManager.Current
            };

            publisher.PublishCommand(Pack(transportMessage));

            return Task.CompletedTask;
        }

        public Task<CommonCommandResult> ExecuteCommandAsync(
            string handlerServiceType,
            string commandName,
            object commandBody,
            CommandPriority priority,
            int ttls,
            string handlerServiceName)
        {

            if (string.IsNullOrWhiteSpace(handlerServiceType))
                throw new ArgumentNullException(nameof(handlerServiceType));

            if (string.IsNullOrWhiteSpace(commandName))
                throw new ArgumentNullException(nameof(commandName));


            var correlationId = Guid.NewGuid().ToString("N");

            var completionSource = new TaskCompletionSource<CommonCommandResult>();

   
            var routingKey = string.IsNullOrWhiteSpace(handlerServiceName) 
                ? $"{handlerServiceType}.{commandName}" 
                : $"{handlerServiceType}#{handlerServiceName}";

            var commandDescriptor = new CommandDescriptor
            {
                CorrelationId = correlationId,
                ServiceType = handlerServiceType,
                ServiceName = handlerServiceName,
                CommandName = commandName,
                Priority = priority,
                SourceServiceType = ServiceConfiguration.AdapterType,
                SourceServiceName = ServiceConfiguration.AdapterName,
                ResultServiceType = ServiceConfiguration.AdapterType,
                ResultServiceName = ServiceConfiguration.AdapterName,
                PublishTimeStamp = DateTime.UtcNow,
                TTL = TimeSpan.FromSeconds(ttls),
                IsSync = true
            };

            var commandPayload = new CommandPayload
            {
                Descriptor = commandDescriptor,
                Body = JObject.FromObject(commandBody)
            };

            salLogger.LogOutgoing(commandPayload);

            var transportMessage = new Message
            {
                Type = MessageTypes.Command,
                Payload = JObject.FromObject(commandPayload),
                Source = $"{commandDescriptor.SourceServiceType}.{commandDescriptor.SourceServiceName}",
                Priority = (byte)commandDescriptor.Priority,
                TimeStamp = commandDescriptor.PublishTimeStamp,
                Destination = routingKey,
                TTL = commandDescriptor.TTL,
                CorrelationId = correlationId,
                Session = SessionManager.Current
            };

            commandResultProcessor.RegisterSimpleCommandResultHandler(correlationId, completionSource, ttls);
            
            publisher.PublishCommand(Pack(transportMessage));

            return completionSource.Task;
        }

        public Task PublishResultAsync(CommonCommandResult result, CommandDescriptor commandDescriptor)
        {
            TimeSpan? ttl = null;

            if (commandDescriptor.TTL.HasValue && commandDescriptor.IsSync)
            {
                ttl = commandDescriptor.PublishTimeStamp + commandDescriptor.TTL.Value - DateTime.UtcNow;
            }

            if (ttl == null || ttl > TimeSpan.Zero)
            {

                var commandResultDescriptor = new CommandResultDescriptor(commandDescriptor);

                if (commandResultDescriptor.HandlerTimeStamp.HasValue)
                    commandResultDescriptor.HandlerDuration = DateTime.UtcNow - commandResultDescriptor.HandlerTimeStamp.Value;

                commandResultDescriptor.HandlerServiceType = ServiceConfiguration.AdapterType;
                commandResultDescriptor.HandlerServiceName = ServiceConfiguration.AdapterName;


                var routingKey = "";

                if (commandResultDescriptor.IsSync)
                {
                    routingKey = $"{commandResultDescriptor.ResultServiceType}#{commandResultDescriptor.ResultServiceName}#Sync";
                }
                else
                {
                    routingKey = string.IsNullOrWhiteSpace(commandResultDescriptor.ResultServiceName) ?
                        commandResultDescriptor.ResultServiceType :
                        $"{commandResultDescriptor.ResultServiceType}#{commandResultDescriptor.ResultServiceName}";
                }

                var commandResultPayload = new CommandResultPayload
                {
                    Descriptor = commandResultDescriptor,
                    Body = result
                };

                salLogger.LogOutgoing(commandResultPayload);

                var transportMessage = new Message
                {
                    Type = MessageTypes.CommandResult,
                    Payload = JObject.FromObject(commandResultPayload),
                    Source = $"{ServiceConfiguration.AdapterType}.{ServiceConfiguration.AdapterName}",
                    Priority = (byte) commandResultDescriptor.Priority,
                    TimeStamp = commandResultDescriptor.PublishTimeStamp,
                    Destination = routingKey,
                    TTL = ttl,
                    CorrelationId = commandResultDescriptor.CorrelationId,
                    Session = SessionManager.Current
                };

                publisher.PublishCommandResult(Pack(transportMessage));
            }
            else
            {
                salLogger.LogNullOutgoing(commandDescriptor);
            }

            return Task.CompletedTask;
        }

        public Task PublishEventAsync(string eventName, object eventBody, TimeSpan? ttl, string handlerServiceType, string handlerServiceName)
        {
            var correlationId = Guid.NewGuid().ToString("N");

            var eventDescriptor = new EventDescriptor
            {
                CorrelationId = correlationId,
                EventName = eventName,
                ServiceType = handlerServiceType,
                ServiceName = handlerServiceName,
                SourceServiceType = ServiceConfiguration.AdapterType,
                SourceServiceName = ServiceConfiguration.AdapterName,
                PublishTimeStamp = DateTime.UtcNow,
                TTL = ttl
            };

            var eventPayload = new EventPayload()
            {
                Descriptor = eventDescriptor,
                Body = JObject.FromObject(eventBody)
            };

            salLogger.LogOutgoing(eventPayload);

            var routingKey = string.IsNullOrWhiteSpace(handlerServiceName) 
                ? eventName 
                : $"{handlerServiceType}#{handlerServiceName}";

            var transportMessage = new Message
            {
                Type = MessageTypes.Event,
                Payload = JObject.FromObject(eventPayload),
                Source = $"{eventDescriptor.SourceServiceType}.{eventDescriptor.SourceServiceName}",
                Priority = 0,
                TimeStamp = eventDescriptor.PublishTimeStamp,
                Destination = routingKey,
                TTL = eventDescriptor.TTL,
                CorrelationId = correlationId,
                Session = SessionManager.Current
            };

            publisher.PublishEvent(Pack(transportMessage));

            return Task.CompletedTask;
        }

    }
}
