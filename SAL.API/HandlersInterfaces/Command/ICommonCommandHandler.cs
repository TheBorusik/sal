using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    [Obsolete]
    public interface ICommonCommandHandler : ICommandHandler
    {
        Task Handle(JObject command);
    }
}