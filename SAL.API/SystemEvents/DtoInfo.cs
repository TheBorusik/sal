using System;

namespace SAL.API
{
    [Obsolete("Use JSchema")]
    public class DtoInfo
    {
        public string Name { get; set; }

        public FieldInfo[] FieldsInfos { get; set; }
    }
}