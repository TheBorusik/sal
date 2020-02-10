namespace SAL.Core.Rabbit.Interfaces
{
    public interface IPublisher 
    {
        void PublishEvent(RabbitMessage message);
        void PublishCommand(RabbitMessage message);
        void PublishCommandResult(RabbitMessage message);


    }
}