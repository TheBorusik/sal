namespace SAL.API
{
    public class FieldInfo
    {
        public bool IsRequired { get; set; }
        public string Name { get; set; }
        public FieldType Type { get; set; }
        public string ObjectName { get; set; }

        public FieldType ElementType { get; set; }
        public string ElementObjectName { get; set; }
    }
}