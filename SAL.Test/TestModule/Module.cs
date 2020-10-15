using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Autofac;
using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.API.Client;
using SAL.API.Command;
using SAL.API.CommandResult;
using SAL.API.Events;
using SAL.API.FrontCommand;
using SAL.Core.Rabbit.Interfaces;
using SAL.Core.Rabbit.Subscription;
using SAL.Infrastructure;
using SAL.Infrastructure.FrontAttributes;
using SAL.Infrastructure.ValidationAttribute;

[assembly: SalAdapterType("SalTest")]
[assembly: SalServiceType("SalTest")]

// ReSharper disable once CheckNamespace
namespace SAL.Test
{
    public class Module : IModule
    {
        public void Configure(ContainerBuilder builder)
        {
            //           builder.RegisterSalHandler<TestFrontHandler>();
            //          builder.RegisterSalHandler<TestFrontHandler2>();
            builder.RegisterSalHandler<TestCommandHandler>();
            //   builder.RegisterSalHandler<CommonCommandHandler>();
            //      / builder.RegisterSalHandler<Test2CommonCommandHandler>();


       //     builder.RegisterSalHandler<CommonCommandResultHandler>();

       //     builder.RegisterSalHandler<TestCommandResultHandler>();

             // builder.RegisterSalHandler<EventHandler>();


            builder.RegisterProcessor<TestProcessor>();
        }
    }

    //  [SalCommandTypeResultHandler]
    public class TestCommand : IHaveResult<TestCommandResult>
    {

    }




    public class DistributorCategory
    {
        public long DistributorId { get; set; }

        public decimal Decimal { get; set; }

        public double Double { get; set; }

        public float Float { get; set; }

        public DateTime DateTime { get; set; }
        public TimeSpan TimeSpan { get; set; }


        public string CategoryId { get; set; }
        public object AllData { get; set; }


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

      //      await executingContext.SalClient.PublishEventAsync(new TestEvent());
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


        public Task Handle(TestCommand command)
        {
          //  throw new Exception("test ex");
          executingContext.SalClient.PublishResultAsync(new Nothing(), context.Descriptor);  
          
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
        ExecutingContext executingContext;

        public void SetContexts(CommandResultContext commandContext, ExecutingContext executingContext)
        {
            this.executingContext = executingContext;
        }

        public Task<bool> ResultHandle(CommandResult<TestCommandResult> result)
        {
     //       executingContext.SalClient.PublishEventAsync(new TestEvent());

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
            throw new Exception("event Exception");
            return Task.CompletedTask;
        }

        public Task Handle(Test2Event Event)
        {
           // throw new Exception("Test");
            return Task.CompletedTask;
        }


    }

    public class
        TestProcessor : IProcessor
    {
        private readonly ILogger<TestProcessor> logger;
        private readonly ILifetimeScope scope;
        private readonly ISalClient client;



        public TestProcessor(ILogger<TestProcessor> logger, ILifetimeScope scope)
        {
            this.logger = logger;
            this.scope = scope;
            // this.client = scope.ResolveNamed<ISalClient>("front");
            this.client = scope.Resolve<ISalClient>();
        }

        public void Start()
        {

            logger.LogInformation("Тестовое сообщение", new { MercId = 10 });




        }



        public void Online()
        {


            client.PublishEventAsync(new TestEvent());


        }

        public void Offline()
        {

        }

        public void Stop()
        {

        }



    }




    [SalExternalMethod("Test.J1")]
    [SalExternalUri("/api/v1/test/j1")]
    [SalExternalUri("/api/v1/test/j")]
    public class TestFrontHandler : IFrontCommandHandlerAsync<Test2Command, TestCommandResult>
    {
        private CommandContext commandContext;
        private ExecutingContext executingContext;

        public void SetContexts(CommandContext commandContext, ExecutingContext executingContext)
        {
            this.commandContext = commandContext;
            this.executingContext = executingContext;
        }

        public async Task Handle(Test2Command command)
        {
            var str = SessionManager.Current.ToIndentedJson();
            SessionManager.Current.AddOrUpdate("Test", "TestValue");
            SessionManager.Current.AddOrUpdate("_temporary", "Temporary value");
            SessionManager.Current.AddOrUpdate("AuthId",234);
            str = SessionManager.Current.ToIndentedJson();


            //        await executingContext.SalClient.PublishEventAsync(new TestEvent());
            await executingContext.SalClient.PublishResultAsync(new TestCommandResult
            {
                TestTimeSpan = TimeSpan.FromHours(1.5),
                TestDate = DateTime.Now,
                TestInt = 1234567,
                TestStr = "Testtt.J1"

            }, commandContext.Descriptor);

        }

    }

    [SalExternalMethod("Test.J2")]
    [SalExternalUri("/api/v1/test/j2")]
    [SalExternalUri("/api/v1/test/j")]
    public class TestFrontHandler2 : IFrontCommandHandlerAsync<Test2Command, TestCommandResult>
    {
        private CommandContext commandContext;
        private ExecutingContext executingContext;

        public void SetContexts(CommandContext commandContext, ExecutingContext executingContext)
        {
            this.commandContext = commandContext;
            this.executingContext = executingContext;
        }

        public async Task Handle(Test2Command command)
        {


            //        await executingContext.SalClient.PublishEventAsync(new TestEvent());
            await executingContext.SalClient.PublishResultAsync(new TestCommandResult
            {
                TestTimeSpan = TimeSpan.FromHours(1.5),
                TestDate = DateTime.Now,
                TestInt = 1234567,
                TestStr = "Testtt.J2"

            }, commandContext.Descriptor);

        }

    }









}
