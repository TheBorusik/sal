using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.Core.Service;

namespace SAL.Test
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var back = new FrontAdapterRunner();
           await back.RunAsync();
        }
    }
}
