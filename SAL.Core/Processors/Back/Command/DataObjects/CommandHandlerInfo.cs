using System;
using System.Reflection;
using Newtonsoft.Json.Schema;

namespace SAL.Core.Processors
{
    internal class CommandHandlerInfo
    {
        public string CommandName;
        public Type CommandType;
        
        public Type HandlerType;
        public MethodInfo HandleMethod;
        
        public bool IsCommon;
        public bool IsInstanceHandler;

        public CommandProcessingSettings CommandProcessingSettings;
        
        public JSchema CommandSchema;
    }

    internal class WfmResultHandlerInfo
    {
        public string HandlerName;
        public Type HandlerType;
        public MethodInfo HandleMethod;
    }
}