namespace SAL.API
{
    public class FrontCommandHandlerInfo 
    {
        public string CommandName;
        public string CommandDto;
        public string ResultDto;
        public string ExternalMethod;
        public string[] ExternalUri;
        public bool HandlerAuth;


        public DtoInfo[] Dtos;
    }
}