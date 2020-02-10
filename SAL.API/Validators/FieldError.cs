namespace SAL.API
{
    public class FieldError
    {
        public string FieldName { get; set; }

        public string Path { get; set; }

        public string ErrorCode { get; set; }

        public string Description { get; set; }
    }

    public static class ValidationCode
    {
        public const string RequiredElementMissing = "RequiredElementMissing";
        public const string InvalidElementFormat = "InvalidElementFormat";
    }

}