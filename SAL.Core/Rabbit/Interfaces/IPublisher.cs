using System.Threading.Tasks;

namespace SAL.Core.Rabbit.Interfaces
{
    public interface IPublisher 
    {
        Task PublishAsync(RabbitMessage message);
    }
}