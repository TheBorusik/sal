namespace SAL.API
{
    public class CommandHandlerInfo
    {
        public string CommandName;
        public bool IsCommon;
        public bool IsInstanceHandler;
        public string CommandDto;
        public string ResultDto;

        public DtoInfo[] Dtos { get; set; }
    }
}