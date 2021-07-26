using System;
using System.Reflection;
using Newtonsoft.Json.Schema;

namespace SAL.Core.Processors
{
    internal class FrontCommandHandlerInfo
    {
        public string CommandName;
        public Type CommandType;
        
        public Type HandlerType;

        public MethodInfo HandlerMethod;
        
        public bool IsCommon;

        public CommandProcessingSettings CommandProcessingSettings;
        public JSchema CommandSchema;
    }
}