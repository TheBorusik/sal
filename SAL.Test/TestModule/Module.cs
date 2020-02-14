using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.API.Client;
using SAL.API.Command;
using SAL.API.CommandResult;
using SAL.API.Events;
using SAL.Infrastructure;

[assembly: SalServiceType("SalTest")]

// ReSharper disable once CheckNamespace
namespace SAL.Test
{
    public class Module : IModule
    {
        public void Configure(ContainerBuilder builder)
        {
            //   builder.RegisterSalHandler<TestCommandHandler>(); 
            builder.RegisterSalHandler<CommonCommandHandler>();
            // builder.RegisterSalHandler<Test2CommonCommandHandler>();


            builder.RegisterSalHandler<CommonCommandResultHandler>();

            builder.RegisterSalHandler<EventHandler>();


            builder.RegisterProcessor<TestProcessor>();
        }
    }

    //  [SalCommandTypeResultHandler]
    public class TestCommand : IHaveResult<TestCommandResult>
    {

    }

    [SalServiceType("Test1")]
    [SalCommandName("Jopa")]
    public class Test2Command : TestCommand
    {

    }

    public class Test3Command : TestCommand
    {

    }

    public class TestCommandResult : ICommandResult
    {
        public string TestStr { get; set; }
        public DateTime TestDate { get; set; }
        public  TimeSpan TestTimeSpan { get; set; }
        public int TestInt { get; set; }

    }

    [SalCommandHandler("SalTest", "Test2")]
    [SalCommandHandler("SalTest", "Test")]
    [SalCommandHandler("SalTest", "Jopa")]
    public class CommonCommandHandler : ICommonCommandHandler
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

            await executingContext.SalClient.PublishEventAsync(new TestEvent());
            await executingContext.SalClient.PublishResultAsync(new TestCommandResult
            {
                TestTimeSpan = TimeSpan.FromHours(1.5),
                TestDate = DateTime.Now,
                TestInt = 1234567,
                TestStr = "Testtt"

            }, commandContext.Descriptor);

             await Task.Delay(10000);
        }


    }

    public class TestCommandHandler :
          ICommandHandlerAsync<TestCommand, TestCommandResult>
        , IValidator<TestCommand>
        , ICommandHandlerAsync<Test2Command, TestCommandResult>
        , IValidator<Test2Command>
        , ICommandHandlerAsync<Test3Command, TestCommandResult>
    {


        public void SetContexts(CommandContext context, ExecutingContext executingContext)
        {

        }

        public Task<IEnumerable<FieldError>> Validate(TestCommand verifiable)
        {
            return Task.FromResult(new FieldError[0].AsEnumerable());
        }

        Task<IEnumerable<FieldError>> IValidator<Test2Command>.Validate(Test2Command verifiable)
        {
            return Task.FromResult(new FieldError[0].AsEnumerable());
        }


        public Task Handle(TestCommand command)
        {
            return Task.CompletedTask;
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



    public class TestCommandResultHandler : ICommandResultHandlerAsync<Test2Command, TestCommandResult>
    {
        ExecutingContext executingContext ;

        public void SetContexts(CommandResultContext commandContext, ExecutingContext executingContext)
        {
            this.executingContext = executingContext;
        }

        public Task<bool> ResultHandle(CommandResult<TestCommandResult> result)
        {
            executingContext.SalClient.PublishEventAsync(new TestEvent());

            return Task.FromResult(true);
        }



    }



    public class CommonCommandResultHandler : ICommonCommandResultHandler
    {
        ExecutingContext executingContext;
        public void SetContexts(CommandResultContext commandContext, ExecutingContext executingContext)
        {
            this.executingContext = executingContext;
        }

        public Task<bool> ResultHandle(CommonCommandResult result)
        {
            executingContext.SalClient.PublishEventAsync(new TestEvent());
            //  throw new Exception("Test");
            return Task.FromResult(true);
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
            return Task.CompletedTask;
        }

        public Task Handle(Test2Event Event)
        {
            throw new Exception("Test");
            return Task.CompletedTask;
        }


    }

    public class
        TestProcessor : IProcessor
    {
        private readonly ILogger<TestProcessor> logger;
        private readonly ISalClient client;

        public TestProcessor(ILogger<TestProcessor> logger, ISalClient client)
        {
            this.logger = logger;
            this.client = client;
        }

        public void Start()
        {

            logger.LogInformation("Тестовое сообщение", new { MercId = 10 });


        }

        public void Online()
        {

            SessionManager.SetNewSession();

            var res = client.ExecuteCommandAsync<TestCommand, TestCommandResult>(
                new TestCommand
                {

                },
                CommandPriority.High,
                100).Result;

            var sss = res.Result;

            client.PublishEventAsync(new TestEvent());

            client.PublishEventAsync(new Test2Event());

            /*
            client.PublishCommandAsync(new TestCommand
            {

            });
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
