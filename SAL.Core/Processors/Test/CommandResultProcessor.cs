using System;
using System.Threading.Tasks;
using SAL.API;


namespace SAL.Core.Processors
{
    internal class TestCommandResultProcessor : IProcessor, ICommandResultProcessor
    {
        public void Start()
        {

        }

        public void Online()
        {

        }

        public void Offline()
        {

        }

        public void Stop()
        {

        }

        public void RegisterSimpleCommandResultHandler(string correlationId, TaskCompletionSource<SimpleCommandResult> completionSource, TimeSpan timeOut, bool throwIfTimeout)
        {

        }
    }
}