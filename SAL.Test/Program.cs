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
          //Environment.SetEnvironmentVariable("AdapterType","AuthAdapter");
          var back = new BackAdapterRunner();
          await back.RunAsync();
        }
    }
}
