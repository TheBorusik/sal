namespace SAL.API
{
    public interface ICommandDtoCreator
    {
        DtoInfo[] GetCommandDtos(string commandName);
        string GetCommandDtoName(string commandName);
        string GetResultDtoName(string commandName);
    }
    
    public interface IEventDtoCreator
    {
        DtoInfo[] GetEventDtos(string eventName);
        string GetEventDtoName(string eventName);
    }
}