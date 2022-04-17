using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Npgsql;
using SAL.API;
using SAL.Core.Rabbit.Consts;
using SAL.Core.S3;
using SAL.Infrastructure;

// ReSharper disable once CheckNamespace
namespace SAL.Test.Front
{
    public class Front : IModule
    {
        public void Configure(ContainerBuilder builder, IConfigWatcher config)
        {
            builder.RegisterSalHandler<CommandResultHandler>();
            builder.RegisterProcessor<TestFront>();
        }
    }

    public class TestFront : IProcessor
    {
        private ILifetimeScope lifetimeScope;

        public TestFront(ILifetimeScope lifetimeScope)
        {
            this.lifetimeScope = lifetimeScope;
        }


        public void Start()
        {
        }


        static int __index = 0;

        public void Online()
        {
            using var scope = lifetimeScope.BeginLifetimeScope();
            var logger = lifetimeScope.Resolve<ILoggerProvider>().CreateLogger("Tester");

            var client = scope.Resolve<ISalClient>();


            if (true)
            {
                for (var ii = 0; ii < 300; ii++)
                    Task.Run(async () =>
                    {
                        try
                        {
                            //var payload = new string('*', 5*1024);
                            var payload = "";

                            for (int i = 0; i < 1000; i++)
                            {
                                var cContext = new CommandContext
                                {
                                    ContextInfo = new ContextInfo("",0,null,"Test"),
                                    Descriptor = new CommandDescriptor
                                    {
                                        Contour = Contour.Back.ToString(),
                                        Priority = CommandPriority.Normal,
                                        CommandName = "SalTester.TestCommand",
                                        CorrelationId = Guid.NewGuid().ToString("N"),
                                        PublishTimeStamp = DateTime.UtcNow,
                                        HandlerTimeStamp = DateTime.UtcNow,
                                        SourceAdapterType = AdapterConfiguration.AdapterType,
                                        SourceAdapterName = AdapterConfiguration.AdapterName,
                                        ResultExchangeName = ExchangeNames.CommandResultExchange,
                                        ResultRoutingKey = $"{AdapterConfiguration.AdapterType}@{AdapterConfiguration.AdapterName}"
                                    
                                    }
                                };

                                await client.PublishResultAsync(CommonCommandResult.Create(new CommandResult
                                {
                                    Id = __index++,
                                    Payload = payload,
                                }), cContext);
                            
                            }
                            
                        }
                        catch (Exception ex)
                        {
                            logger.Error("Какето ошибка",ex);
                            throw;
                        }
                    });
            }
        }

        public void Offline()
        {
        }

        public void Stop()
        {
        }
    }


    public class CommandResult
    {
        public int Id { get; set; }
        public string Payload { get; set; }
    }


    [SalContourHandler(Contour.Back)]
    public class CommandResultHandler : ICommandResultHandle2Async<CommandResult>
    {
        [SalCommandName("SalTester.TestCommand")]
        public async Task<bool> ResultHandle(CommandResult<CommandResult> result, CommandResultContext commandContext, ExecutingContext executingContext)
        {
            return true;
        }
    }
}