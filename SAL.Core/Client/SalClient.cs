using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Newtonsoft.Json.Linq;
using SAL.API;
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
        public string Contour { get;  }
        public string ContourName { get; }

        public SalClient(ILifetimeScope scope, ISalLogger salLogger, string prefix)
        {
            ITransport transport;
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                transport = scope.ResolveNamed<ITransport>(prefix);
                Contour = prefix.ToUpper();
                ContourName = transport.CounterName;
            }
            else
            {
                transport = scope.Resolve<ITransport>();
                Contour = "BACK";
                ContourName = transport.CounterName;
            }
            
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

            SessionManager.IncOperationId();

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
            SessionManager.IncOperationId();
            var result = await ExecuteCommandAsync(
                commandType.GetRouteKey(),
                command,
                priority,
                ttl.Value,
                handlerServiceType,
                handlerServiceName
            );

            return new CommandResult<TCommandResult>(result);
        }


        public Task PublishResultAsync(ICommandResult result, CommandDescriptor commandDescriptor)
        {
            SessionManager.IncOperationId();

            return PublishResultAsync(new CommonCommandResult
            {
                Error = null,
                Result = JObject.FromObject(result, SalSerializer.Create()),
                ResultCode = ResultCodes.Success
            }, commandDescriptor);
        }

        public Task PublishResultAsync(object result, string code, CommandDescriptor commandDescriptor)
        {
            SessionManager.IncOperationId();
            return PublishResultAsync(new CommonCommandResult
            {
                Error = null,
                Result = JObject.FromObject(result, SalSerializer.Create()),
                ResultCode = code
            }, commandDescriptor);
        }

        public Task PublishResultAsync(InternalExceptionDTO exceptionDTO, CommandDescriptor commandDescriptor)
        {
            SessionManager.IncOperationId();
            return PublishResultAsync(new CommonCommandResult
            {
                Error = exceptionDTO,
                Result = null,
                ResultCode = ResultCodes.Error
            }, commandDescriptor);
        }

        public Task PublishResultAsync(IList<FieldError> validationErrors, CommandDescriptor commandDescriptor)
        {
            SessionManager.IncOperationId();
            return PublishResultAsync(new CommonCommandResult
            {
                Error = SalError.CreateValidationDto(validationErrors),
                Result = null,
                ResultCode = ResultCodes.Error
            }, commandDescriptor);
        }

        public Task PublishResultAsync<TCommandResult>(CommandResult<TCommandResult> result, CommandDescriptor commandDescriptor) where TCommandResult : class, ICommandResult, new()
        {
            if (typeof(TCommandResult) == typeof(None))
                return Task.CompletedTask;

            SessionManager.IncOperationId();
            return PublishResultAsync(new CommonCommandResult
            {
                Error = result.Error,
                Result = result.Result.Clone(),
                ResultCode = result.ResultCode
            }, commandDescriptor);
        }


        public Task PublishEventAsync(IEvent evnt, TimeSpan? ttl = null, string handlerServiceType = null, string handlerServiceName = null)
        {
            if (evnt == null)
                return Task.CompletedTask;
            SessionManager.IncOperationId();
            
            return PublishEventAsync(
                evnt.GetType().GetRouteKey(),
                evnt,
                ttl,
                evnt.GetType().IsSystemEvent(),
                handlerServiceType,
                handlerServiceName
            );
        }



        public Task RaiseExceptionDetectEvent(string cid, InternalExceptionDTO exceptionDTO)
        {
            SessionManager.IncOperationId();
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
            SessionManager.IncOperationId();
            return PublishEventAsync(new ExceptionDetectedEvent
            {
                CorrelationId = cid,
                ExceptionDto = ex.ToDto(SalErrorCodes.Fatal),
                AdapterType = AdapterConfiguration.AdapterType,
                AdapterName = AdapterConfiguration.AdapterName

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

        public async Task<string> PublishCommandAsync(string commandName, object commandBody)
        {
            var correlationId = Guid.NewGuid().ToString("N");
            await PublishCommandAsync(commandName, commandBody, correlationId, 
                CommandPriority.Normal, null,
                null, null, 
                AdapterConfiguration.AdapterType,  AdapterConfiguration.AdapterName);

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
                IsSync = false,
                Contour = ContourName
            };
            
            return PublishCommandAsync(commandDescriptor, JObject.FromObject(commandBody, SalSerializer.Create()));
        }

        public async Task<CommonCommandResult> ExecuteCommandAsync(
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
                Contour = ContourName
            };

            var commandPayload = new CommandPayload
            {
                Descriptor = commandDescriptor,
                Payload = JObject.FromObject(commandBody, SalSerializer.Create())
            };

            return await ExecuteCommandAsync(commandPayload, routingKey);
            
        }

        public async Task<CommonCommandResult> ExecuteExternalHttp(
            ExternalHttpRequest request,
            TimeSpan ttl,
            string handlerAdapterType,
            string handlerAdapterName)
        {
            
            var correlationId = Guid.NewGuid().ToString("N");
            
            var routingKey = request.Path;



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
                Contour = ContourName
            };

            var commandPayload = new CommandPayload
            {
                Descriptor = commandDescriptor,
                Payload = JObject.FromObject(request, SalSerializer.Create())
            };

            return await ExecuteCommandAsync(commandPayload, routingKey);
        }
        
        
        public async Task<CommonCommandResult> ExecuteCommandAsync(CommandPayload commandPayload, string routingKey)
        {

            commandPayload.Descriptor.TTL ??= TimeSpan.FromMinutes(1);
            
            salLogger.LogOutgoing(commandPayload);
            
            var transportMessage = new Message
            {
                Type = MessageTypes.Command,
                Payload = JObject.FromObject(commandPayload, SalSerializer.Create()),
                Source = $"{commandPayload.Descriptor.SourceAdapterType}.{commandPayload.Descriptor.SourceAdapterName}",
                Priority = (byte) commandPayload.Descriptor.Priority,
                TimeStamp = commandPayload.Descriptor.PublishTimeStamp,
                Destination = routingKey,
                TTL = commandPayload.Descriptor.TTL,
                CorrelationId = commandPayload.Descriptor.CorrelationId,
                Session = SessionManager.Current.Clone()
            };
            
            var completionSource = new TaskCompletionSource<SimpleCommandResult>();
            
            commandResultProcessor.RegisterSimpleCommandResultHandler(commandPayload.Descriptor.CorrelationId, completionSource,
                commandPayload.Descriptor.TTL.Value);

            publisher.PublishCommand(Pack(transportMessage));

            var result = await completionSource.Task;

            SessionManager.Set(result.CommandResultContext.Session);
            
            return result.CommandResult;
        }

        public Task PublishCommandAsync(CommandDescriptor commandDescriptor, JObject commandBody)
        {
            var routingKey = "";
            if (string.IsNullOrWhiteSpace(commandDescriptor.DestinationAdapterType))
                routingKey = commandDescriptor.CommandName;
            else if(string.IsNullOrWhiteSpace(commandDescriptor.DestinationAdapterName))
                routingKey = commandDescriptor.DestinationAdapterType;
            else
                routingKey = $"{commandDescriptor.DestinationAdapterType}#{commandDescriptor.DestinationAdapterName}";
            
            var commandPayload = new CommandPayload
            {
                Descriptor = commandDescriptor,
                Payload = commandBody.Clone()
            };
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
                CorrelationId = commandDescriptor.CorrelationId,
                Session = SessionManager.Current.Clone()
            };
            publisher.PublishCommand(Pack(transportMessage)); 
            return Task.CompletedTask;
        }

        public Task PublishResultAsync(CommonCommandResult result, CommandDescriptor commandDescriptor)
        {
            var commandResultDescriptor = new CommandResultDescriptor(commandDescriptor);
            
            commandResultDescriptor.HandlerAdapterType = AdapterConfiguration.AdapterType;
            commandResultDescriptor.HandlerAdatpterName = AdapterConfiguration.AdapterName;
            commandResultDescriptor.Contour = ContourName;

            return PublishResultAsync(commandResultDescriptor, result);
        }

        public Task PublishResultAsync(CommandResultDescriptor commandResultDescriptor, CommonCommandResult result)
        {
            TimeSpan? ttl = null;

            if (commandResultDescriptor.TTL.HasValue && commandResultDescriptor.IsSync)
            {
                ttl = commandResultDescriptor.PublishTimeStamp + commandResultDescriptor.TTL.Value - DateTime.UtcNow;
            }

            if (ttl == null || ttl > TimeSpan.Zero)
            {
                if (commandResultDescriptor.HandlerTimeStamp.HasValue)
                    commandResultDescriptor.HandlerDuration = DateTime.UtcNow - commandResultDescriptor.HandlerTimeStamp.Value;

                
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
                    Session = SessionManager.Current.Clone()
                };

                publisher.PublishCommandResult(Pack(transportMessage));
            }
            else
            {
                salLogger.LogNullOutgoing(commandResultDescriptor);
            }

            return Task.CompletedTask;
        }
        
        public Task PublishEventAsync(string eventName, object eventBody, TimeSpan? ttl, bool isSystem, string handlerServiceType, string handlerServiceName)
        {
            return PublishEventAsync(eventName,eventBody ,Guid.NewGuid().ToString("N"),ttl,isSystem,handlerServiceType,handlerServiceName);
        }
        
        public Task PublishEventAsync(string eventName, object eventBody, string correlationId ,TimeSpan? ttl, bool isSystem, string handlerServiceType, string handlerServiceName)
        {
            var eventDescriptor = new EventDescriptor
            {
                CorrelationId = correlationId,
                EventName = eventName,
                DestinationAdapterType = handlerServiceType,
                DestinationAdapterName = handlerServiceName,
                SourceAdapterType = AdapterConfiguration.AdapterType,
                SourceAdapterName = AdapterConfiguration.AdapterName,
                PublishTimeStamp = DateTime.UtcNow,
                TTL = ttl,
                Contour = ContourName,
                IsSystem = isSystem
            };
            
            return PublishEventAsync(eventDescriptor, JObject.FromObject(eventBody, SalSerializer.Create()));
        }

        public Task PublishEventAsync(EventDescriptor eventDescriptor, JObject eventBody)
        {
            var eventPayload = new EventPayload()
            {
                Descriptor = eventDescriptor,
                Payload = eventBody
            };

            salLogger.LogOutgoing(eventPayload);

            string routingKey;

            
            if (string.IsNullOrWhiteSpace(eventDescriptor.DestinationAdapterType))
                routingKey = eventDescriptor.EventName;
            else
            {
                routingKey = string.IsNullOrWhiteSpace(eventDescriptor.DestinationAdapterName) 
                    ? eventDescriptor.DestinationAdapterType 
                    : $"{eventDescriptor.DestinationAdapterType}#{eventDescriptor.DestinationAdapterName}";

                if (eventDescriptor.IsSystem)
                {
                    routingKey = $"System#{routingKey}";
                }
            }
            

            
            var transportMessage = new Message
            {
                Type = MessageTypes.Event,
                Payload = JObject.FromObject(eventPayload, SalSerializer.Create()),
                Source = $"{eventDescriptor.SourceAdapterType}.{eventDescriptor.SourceAdapterName}",
                Priority = 0,
                TimeStamp = eventDescriptor.PublishTimeStamp,
                Destination = routingKey,
                TTL = eventDescriptor.TTL,
                CorrelationId = eventDescriptor.CorrelationId,
                Session = SessionManager.Current.Clone()
            };

            publisher.PublishEvent(Pack(transportMessage));

            return Task.CompletedTask;
        }

    }
}