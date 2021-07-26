using System;

namespace SAL.API
{
    [Obsolete]
    public class DtoInfo
    {
        public string Name { get; set; }

        public FieldInfo[] FieldsInfos { get; set; }
    }
}