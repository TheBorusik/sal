using System;
using System.Threading.Tasks;
using SAL.Core.Service;

namespace SAL.Test
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            Environment.SetEnvironmentVariable("AdapterType","SalTest");
            var back = new BackAdapterRunner();
           await back.RunAsync();
        }
    }
}
