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
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Npgsql;
using SAL.API;
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
            //builder.RegisterProcessor<TestFront>();
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

        public void Online()
        {
            using var scope = lifetimeScope.BeginLifetimeScope();

            var client = scope.Resolve<ISalClient>();

            if (true)
            {
                for (var ii = 0; ii < 10; ii++)
                    Task.Run(async () =>
                    {
                        var payload = new string('*', 1*1024);
                        //var payload = "";

                        for (int i = 0; i < 100000; i++)
                        {
                            var cContext = new CommandContext
                            {
                                ContextInfo = new ContextInfo
                                {
                                    AuthId = 0,
                                    OperationId = "Test",
                                },
                                Descriptor = new CommandDescriptor
                                {
                                    Contour = Contour.Back.ToString(),
                                    Priority = CommandPriority.Normal,
                                    CommandName = "SalTester.TestCommand",
                                    CorrelationId = Guid.NewGuid().ToString("N"),
                                    IsSync = false,
                                    ResultAdapterType = "SalTest",
                                    PublishTimeStamp = DateTime.UtcNow,
                                    HandlerTimeStamp = DateTime.UtcNow,
                                    SourceAdapterType = AdapterConfiguration.AdapterType,
                                    SourceAdapterName = AdapterConfiguration.AdapterName,
                                }
                            };

                            await client.PublishResultAsync(CommonCommandResult.Create(new CommandResult
                            {
                                Payload = payload,
                            }), cContext);
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
        public string Payload { get; set; }
    }


    [SalContourHandler(Contour.Back)]
    public class CommandResultHandler : ICommandResultHandle2Async<CommandResult>
    {
        [SalCommandName("SalTester.TestCommand")]
        public async Task<bool> ResultHandle(CommandResult<CommandResult> result, CommandResultContext commandContext, ExecutingContext executingContext)
        {
            await Task.Delay(0);
            return true;
        }
    }
}