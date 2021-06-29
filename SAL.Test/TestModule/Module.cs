using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NLog;
using SAL.API;
using SAL.Infrastructure;



// ReSharper disable once CheckNamespace
namespace SAL.Test
{
    public class Module : IModule
    {
        public void Configure(ContainerBuilder builder, IConfigWatcher config)
        {
         //   builder.RegisterSalHandler<TestCommandHandler>();
     //       builder.RegisterSalHandler<CommonCommandHandler>();
     //       builder.RegisterSalHandler<CommonCommandResultHandler>();
      //      builder.RegisterSalHandler<EventHandler>();

            builder.RegisterProcessor<TestProcessor>();
        }
    }

    //  [SalCommandTypeResultHandler]
    public class TestCommand : IHaveResult<TestCommandResult>
    {
    }


    [SalServiceType("Test1")]
    [SalCommandName("Jopa")]
    public class Test2Command : IHaveResult<TestCommandResult>
    {
    }

    public class Test3Command : TestCommand
    {
    }

    public class TestCommandResult : ICommandResult
    {
        public string TestStr { get; set; }
        public DateTime TestDate { get; set; }
        public TimeSpan TestTimeSpan { get; set; }
        public int TestInt { get; set; }
    }

    [SalCommandHandler("SalTest", "Test2")]
    [SalCommandHandler("SalTest", "Test")]
    [SalCommandHandler("Test1", "Jopa")]
    public class CommonCommandHandler : ICommonCommandHandler , ICommandDtoCreator
    {
        private CommandContext commandContext;
        private ExecutingContext executingContext;

        public void SetContexts(CommandContext commandContext, ExecutingContext executingContext)
        {
            this.commandContext = commandContext;
            this.executingContext = executingContext;
        }

        public async Task Handle(JObject command)
        {
            //      await executingContext.SalClient.PublishEventAsync(new TestEvent());
            await executingContext.SalClient.PublishResultAsync(new TestCommandResult
            {
                TestTimeSpan = TimeSpan.FromHours(1.5),
                TestDate = DateTime.Now,
                TestInt = 1234567,
                TestStr = "Testtt"
            }, commandContext);

            await Task.Delay(10000);
        }

        public DtoInfo[] GetCommandDtos(string commandName)
        {
            return null;
        }

        public string GetCommandDtoName(string commandName)
        {
            return null;
        }

        public string GetResultDtoName(string commandName)
        {
            return null;
        }
    }

    public static class aa
    {
        private static int a = 0;

        public static int Get()
        {
            return a++;
        }
    }

    public class TestCommandHandler :
        ICommandHandlerAsync<TestCommand, TestCommandResult>
        , IValidator<TestCommand>
        , ICommandHandlerAsync<Test2Command, TestCommandResult>
        , IValidator<Test2Command>
        , ICommandHandlerAsync<Test3Command, TestCommandResult>
    {
        private ExecutingContext executingContext;
        private CommandContext context;


        public void SetContexts(CommandContext context, ExecutingContext executingContext)
        {
            this.context = context;
            this.executingContext = executingContext;
        }

        public Task<IEnumerable<FieldError>> Validate(TestCommand verifiable)
        {
            return Task.FromResult(new FieldError[0].AsEnumerable());
        }

        Task<IEnumerable<FieldError>> IValidator<Test2Command>.Validate(Test2Command verifiable)
        {
            return Task.FromResult(new FieldError[0].AsEnumerable());
        }


        public async Task Handle(TestCommand command)
        {
            var a = aa.Get();
          //  SessionManager.Current.AddOrUpdate(SessionNames.OrderId ,a + 100000);
          //  SessionManager.Current.AddOrUpdate(SessionNames.WfmProcessId ,a + 500);

            
      //      if(a >= 10)
      //          throw new Exception("test TestCommandHandler");
            
            await executingContext.SalClient.PublishResultAsync(new Nothing(), context);
        }

        public Task Handle(Test2Command command)
        {
            return Task.CompletedTask;
        }

        public Task Handle(Test3Command command)
        {
            return Task.CompletedTask;
        }
    }


    public class CommonCommandResultHandler : ICommonCommandResultHandler , ICommandDtoCreator
    {
        ExecutingContext executingContext;

        public void SetContexts(CommandResultContext commandContext, ExecutingContext executingContext)
        {
            this.executingContext = executingContext;
        }

        public Task<bool> ResultHandle(CommonCommandResult result)
        {
   //          executingContext.SalClient.PublishEventAsync(new TestEvent());
             return Task.FromResult(true);
             //    throw new Exception("Test result");
        }

        public DtoInfo[] GetCommandDtos(string commandName)
        {
            return null;
        }

        public string GetCommandDtoName(string commandName)
        {
            return null;
        }

        public string GetResultDtoName(string commandName)
        {
            return null;
        }
    }


    public class TestEvent : IEvent
    {
    }

    public class Test2Event : IEvent
    {
    }


    public class EventHandler : IEventHandler<TestEvent>, IEventHandler<Test2Event>
    {
        public void SetContexts(EventContext eventContext, ExecutingContext executingContext)
        {
        }

        public Task Handle(TestEvent Event)
        {
       //     throw new Exception("event Exception");
            return Task.CompletedTask;
        }

        public Task Handle(Test2Event Event)
        {
            return Task.CompletedTask;
        }
    }

    public class TestData
    {
        public string A { get; set; }
        public int B { get; set; }
        public bool? C { get; set; }
    }
    
    public class
        TestProcessor : IProcessor
    {
        private readonly ILogger<TestProcessor> logger;
        private readonly ILifetimeScope scope;
        private readonly ISalClient client;
        private readonly IRedisStore redisStore;


        public TestProcessor(ILogger<TestProcessor> logger, ILifetimeScope scope)
        {
            this.logger = logger;
            this.scope = scope;
            // this.client = scope.ResolveNamed<ISalClient>("front");
            this.client = scope.Resolve<ISalClient>();

            redisStore = scope.Resolve<IRedisStore>();
        }

        
        
        
        public void Start()
        {

            redisStore.Add("test", new TestData
            {
                A = "test",
                B = 100,
                C = true
            }, TimeSpan.FromMinutes(1));

            //  logger.LogInformation("Тестовое сообщение", new {MercId = 10});

            //   throw new Exception("test");
        }


        public void Online()
        {
            /*
            var res = client.ExecuteCommandAsync
                <GetCommandTestCasesCommand, GetCommandTestCasesResult>(
                    new GetCommandTestCasesCommand
                    {
                        //Configuration.RunSyncProcess
                        //Configuration.RunSyncProcess
                        CommandName = "Configuration.RunSyncProcess",
                    }, CommandPriority.Normal, TimeSpan.FromSeconds(15), AdapterConfiguration.AdapterType, AdapterConfiguration.AdapterName).Result;

*/
            
        }

        public void Offline()
        {
        }

        public void Stop()
        {
        }
    }



}