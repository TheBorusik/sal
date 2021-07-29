using System;
using System.Reflection;
using Newtonsoft.Json.Schema;

namespace SAL.Core.Processors
{


    internal class CommandResultHandlerInfo
    {
        public string CommandName;
        public Type ResultType;
        public Type HandlerType;
        public MethodInfo HandleMethod;
        public bool IsCommon;
        public JSchema ResultSchema;
    }
}