using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.API.Client;
using SAL.API.Helpers;
using SAL.API.Monad;
using SAL.Core.DTO.Transport;
using SAL.Core.Helpers;
using SAL.Core.Processors;
using SAL.Core.Rabbit.Interfaces;
using SAL.Infrastructure;

namespace SAL.Core.Client
{
    public class SalClient : ISalClient, ILoSalClient
    {
        private readonly IPublisher publisher;
        private readonly ICommandResultProcessor commandResultProcessor;
        private readonly ISalLogger salLogger;

        public SalClient(ILifetimeScope scope, ISalLogger salLogger, string prefix)
        {
            var transport = !string.IsNullOrWhiteSpace(prefix) ? scope.ResolveNamed<ITransport>(prefix) : scope.Resolve<ITransport>();

            this.publisher = transport.CreatePublisher();

            this.commandResultProcessor = !string.IsNullOrWhiteSpace(prefix) ? scope.ResolveNamed<ICommandResultProcessor>(prefix) : scope.Resolve<ICommandResultProcessor>();

            this.salLogger = salLogger;
        }


        public async Task<string> PublishCommandAsync<TCommand>(
            TCommand command,
            string correlationId = null,
            CommandPriority priority = CommandPriority.Normal,
            TimeSpan? ttl = null,
            string handlerServiceType = null,
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
                resultServiceType = AdapterConfiguration.AdapterType;

            if (string.IsNullOrWhiteSpace(resultServiceName) && !commandType.IsResultTypeHandler())
                resultServiceName = AdapterConfiguration.AdapterName;

            await PublishCommandAsync(
                commandType.GetRouteKey(),
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
                commandType.GetRouteKey(),
                command,
                priority,
                ttl.Value,
                handlerServiceType,
                handlerServiceName
            );

            return result.ConvertValue<CommandResult<TCommandResult>>();
        }


        public Task PublishResultAsync(ICommandResult result, CommandDescriptor commandDescriptor)
        {
            return PublishResultAsync(new CommonCommandResult
            {
                Error = null,
                Result = JObject.FromObject(result, SalSerializer.Create()),
                ResultCode = ResultCodes.Success
            }, commandDescriptor);
        }

        public Task PublishResultAsync(object result, string code, CommandDescriptor commandDescriptor)
        {
            return PublishResultAsync(new CommonCommandResult
            {
                Error = null,
                Result = JObject.FromObject(result, SalSerializer.Create()),
                ResultCode = code
            }, commandDescriptor);
        }

        public Task PublishResultAsync(InternalExceptionDTO exceptionDTO, CommandDescriptor commandDescriptor)
        {
            return PublishResultAsync(new CommonCommandResult
            {
                Error = exceptionDTO,
                Result = null,
                ResultCode = exceptionDTO.Code
            }, commandDescriptor);
        }

        public Task PublishResultAsync(IList<FieldError> validationErrors, CommandDescriptor commandDescriptor)
        {
            return PublishResultAsync(new CommonCommandResult
            {
                Error = SalError.CreateValidationDto(validationErrors),
                Result = null,
                ResultCode = ResultCodes.Error
            }, commandDescriptor);
        }

        public Task PublishResultAsync<TCommandResult>(CommandResult<TCommandResult> result, CommandDescriptor commandDescriptor) where TCommandResult : class, ICommandResult, new()
        {
            return PublishResultAsync(new CommonCommandResult
            {
                Error = null,
                Result = JObject.FromObject(result, SalSerializer.Create()),
                ResultCode = ResultCodes.Success
            }, commandDescriptor);
        }


        public Task PublishEventAsync(IEvent evnt, TimeSpan? ttl = null, string handlerServiceType = null, string handlerServiceName = null)
        {
            if (evnt == null)
                return Task.CompletedTask;
            return PublishEventAsync(
                evnt.GetType().GetRouteKey(),
                evnt,
                ttl,
                handlerServiceType,
                handlerServiceName
            );
        }

        public Task RaiseExceptionDetectEvent(string cid, InternalExceptionDTO exceptionDTO)
        {
            return PublishEventAsync(new ExceptionDetectedEvent
            {
                CorrelationId = cid,
                ExceptionDto = exceptionDTO,
                ServiceName = AdapterConfiguration.AdapterName,
                ServiceType = AdapterConfiguration.AdapterType
            });
        }

        public Task RaiseExceptionDetectEvent(string cid, Exception ex)
        {
            return PublishEventAsync(new ExceptionDetectedEvent
            {
                CorrelationId = cid,
                ExceptionDto = ex.ToDto(SalErrorCodes.Fatal),
                ServiceName = AdapterConfiguration.AdapterName,
                ServiceType = AdapterConfiguration.AdapterType
            });
        }

        //lo

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
                SourceAdapterType = AdapterConfiguration.AdapterType,
                SourceAdapterName = AdapterConfiguration.AdapterName,
                DestinationAdapterType = handlerAdapterType,
                DestinationAdapterName = handlerAdapterName,
                ResultAdapterType = resultAdapterType,
                ResultAdapterName = resultAdapterName,
                PublishTimeStamp = DateTime.UtcNow,
                TTL = ttl,
                IsSync = false
            };

            var commandPayload = new CommandPayload
            {
                Descriptor = commandDescriptor,
                Payload = JObject.FromObject(commandBody, SalSerializer.Create())
            };


            UpdateOprationId();
            salLogger.LogOutgoing(commandPayload);

            var transportMessage = new Message
            {
                Type = MessageTypes.Command,
                Payload = JObject.FromObject(commandPayload, SalSerializer.Create()),
                Source = $"{commandDescriptor.SourceAdapterType}.{commandDescriptor.SourceAdapterName}",
                Priority = (byte) commandDescriptor.Priority,
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
            string commandName,
            object commandBody,
            CommandPriority priority,
            TimeSpan ttl,
            string handlerAdapterType,
            string handlerAdapterName)
        {
            if (string.IsNullOrWhiteSpace(commandName))
                throw new ArgumentNullException(nameof(commandName));


            var correlationId = Guid.NewGuid().ToString("N");

            var completionSource = new TaskCompletionSource<CommonCommandResult>();

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
                IsSync = true
            };

            var commandPayload = new CommandPayload
            {
                Descriptor = commandDescriptor,
                Payload = JObject.FromObject(commandBody, SalSerializer.Create())
            };
            UpdateOprationId();
            salLogger.LogOutgoing(commandPayload);

            var transportMessage = new Message
            {
                Type = MessageTypes.Command,
                Payload = JObject.FromObject(commandPayload, SalSerializer.Create()),
                Source = $"{commandDescriptor.SourceAdapterType}.{commandDescriptor.SourceAdapterName}",
                Priority = (byte) commandDescriptor.Priority,
                TimeStamp = commandDescriptor.PublishTimeStamp,
                Destination = routingKey,
                TTL = commandDescriptor.TTL,
                CorrelationId = correlationId,
                Session = SessionManager.Current
            };

            commandResultProcessor.RegisterSimpleCommandResultHandler(correlationId, completionSource, ttl);

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

                commandResultDescriptor.HandlerServiceType = AdapterConfiguration.AdapterType;
                commandResultDescriptor.HandlerServiceName = AdapterConfiguration.AdapterName;


                var routingKey = "";

                if (commandResultDescriptor.IsSync)
                {
                    routingKey = $"{commandResultDescriptor.ResultAdapterType}#{commandResultDescriptor.ResultAdapterName}#Sync";
                }
                else
                {
                    routingKey = string.IsNullOrWhiteSpace(commandResultDescriptor.ResultAdapterName) ? commandResultDescriptor.ResultAdapterType : $"{commandResultDescriptor.ResultAdapterType}#{commandResultDescriptor.ResultAdapterName}";
                }

                var commandResultPayload = new CommandResultPayload
                {
                    Descriptor = commandResultDescriptor,
                    Payload = result
                };

                UpdateOprationId();
                salLogger.LogOutgoing(commandResultPayload);

                var transportMessage = new Message
                {
                    Type = MessageTypes.CommandResult,
                    Payload = JObject.FromObject(commandResultPayload, SalSerializer.Create()),
                    Source = $"{AdapterConfiguration.AdapterType}.{AdapterConfiguration.AdapterName}",
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
                DestinationAdapterType = handlerServiceType,
                DestinationAdapterName = handlerServiceName,
                SourceAdapterType = AdapterConfiguration.AdapterType,
                SourceAdapterName = AdapterConfiguration.AdapterName,
                PublishTimeStamp = DateTime.UtcNow,
                TTL = ttl
            };

            var eventPayload = new EventPayload()
            {
                Descriptor = eventDescriptor,
                Payload = JObject.FromObject(eventBody, SalSerializer.Create())
            };

            UpdateOprationId();
            salLogger.LogOutgoing(eventPayload);

            var routingKey = string.Empty;


            if (!string.IsNullOrWhiteSpace(handlerServiceType) && !string.IsNullOrWhiteSpace(handlerServiceName))
                routingKey = $"{handlerServiceType}#{handlerServiceName}";
            else
                routingKey = eventName;


            var transportMessage = new Message
            {
                Type = MessageTypes.Event,
                Payload = JObject.FromObject(eventPayload, SalSerializer.Create()),
                Source = $"{eventDescriptor.SourceAdapterType}.{eventDescriptor.SourceAdapterName}",
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

        private void UpdateOprationId()
        {
            var operationId = SessionManager.Current.GetSafeValue(SessionNames.OperationId, 0L);
            SessionManager.Current.AddOrUpdate(SessionNames.OperationId, ++operationId);
        }
    }
}