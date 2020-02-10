using System;
using System.Collections.Generic;
using System.Reflection;

namespace SAL.Core.Processors
{
    internal class CommandHandlerInfo
    {
        public string CommandName;
        public Type CommandType;
        public Type ResultType;

        public Type HandlerType;

        public MethodInfo HandlerMethod;
        public MethodInfo ValidationMethod;

        public bool IsCommon;
        public bool IsInstanceHandler;

        public CommandProcessingSettings CommandProcessingSettings;
    }

    public class CommandProcessingSettings
    {
        public ushort PrefetchCount { get; set; }
    }


    internal class CommandProcessorConfig
    {
        public ushort GlobalPrefetchCount { get; set; }
        public ushort CommandPrefetchCount { get; set; }
        public Dictionary<string, CommandProcessingSettings> CommandProcessingSettings { get; set; }
    }
}