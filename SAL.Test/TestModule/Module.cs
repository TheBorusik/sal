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
    public class CommonCommandHandler : ICommonCommandHandlerAsync
    {
        public async Task Handle(JObject command, CommandContext context, ExecutingContext executingContext)
        {


            await executingContext.SalClient.PublishResultAsync(new TestCommandResult
            {
                TestTimeSpan = TimeSpan.FromHours(1.5),
                TestDate = DateTime.Now,
                TestInt = 1234567,
                TestStr = "Testtt"

            }, context.Descriptor);

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




        public Task Handle(TestCommand command, CommandContext context)
        {

            return Task.FromResult(new TestCommandResult());

        }

        public Task Handle(Test2Command command, CommandContext context)
        {
            return Task.FromResult(new TestCommandResult());
        }

        public Task Handle(Test3Command command, CommandContext context)
        {
            return Task.FromResult(new TestCommandResult());
        }


        public Task<IEnumerable<FieldError>> Validate(TestCommand verifiable)
        {
            return Task.FromResult(new FieldError[0].AsEnumerable());
        }

        Task<IEnumerable<FieldError>> IValidator<Test2Command>.Validate(Test2Command verifiable)
        {
            return Task.FromResult(new FieldError[0].AsEnumerable());
        }


        public Task Handle(TestCommand command, CommandContext context, ExecutingContext executingContext)
        {
            return Task.CompletedTask;
        }

        public Task Handle(Test2Command command, CommandContext context, ExecutingContext executingContext)
        {
            return Task.CompletedTask;
        }

        public Task Handle(Test3Command command, CommandContext context, ExecutingContext executingContext)
        {
            return Task.CompletedTask;
        }
    }



    public class TestCommandResultHandler : ICommandResultHandlerAsync<Test2Command, TestCommandResult>
    {
        public Task<bool> ResultHandle(CommandResult<TestCommandResult> result, CommandResultDescriptor context, ExecutingContext executingContext)
        {
            

            return Task.FromResult(true);
        }
    }



    public class CommonCommandResultHandler : ICommonCommandResultHandlerAsync
    {
        public Task<bool> ResultHandle(CommonCommandResult result, CommandResultDescriptor context, ExecutingContext executingContext)
        {
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
        public Task Handle(TestEvent Event, EventDescriptor eventDescriptor, ExecutingContext executingContext)
        {
            return Task.CompletedTask;
        }

        public Task Handle(Test2Event Event, EventDescriptor eventDescriptor, ExecutingContext executingContext)
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
