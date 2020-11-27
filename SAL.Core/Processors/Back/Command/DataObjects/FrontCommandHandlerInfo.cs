using System;
using System.Reflection;

namespace SAL.Core.Processors
{
    internal class FrontCommandHandlerInfo
    {
        public string CommandName;
        public Type CommandType;
        public Type ResultType;

        public Type HandlerType;

        public MethodInfo HandlerMethod;
        public MethodInfo ValidationMethod;

        public bool IsCommon;

        public CommandProcessingSettings CommandProcessingSettings;
    }
}