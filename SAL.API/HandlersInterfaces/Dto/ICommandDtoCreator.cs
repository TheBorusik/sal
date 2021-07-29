using System;
using Newtonsoft.Json.Schema;

namespace SAL.API
{
    [Obsolete("use ICommandSchemeCreator")]
    public interface ICommandDtoCreator
    {
        DtoInfo[] GetCommandDtos(string commandName);
        string GetCommandDtoName(string commandName);
        string GetResultDtoName(string commandName);
    }
    
    [Obsolete("use IEventSchemeCreator")]
    public interface IEventDtoCreator
    {
        DtoInfo[] GetEventDtos(string eventName);
        string GetEventDtoName(string eventName);
    }
    
    public interface ICommandSchemeCreator
    {
        JSchema GetCommandSchema(string commandName);
        JSchema GetResultSchema(string commandName);
    }
    
    public interface IEventSchemeCreator
    {
        JSchema GetEventScheme(string eventName);
    }

    public interface ICommandNameResolver
    {
        string Resolve(Type handlerInterfaceType);
    }
}