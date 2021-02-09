namespace SAL.API
{
    public class CommandResultHandlerInfo
    {
        public string CommandName;
        public string CommandDto;
        public string ResultDto;
        public bool IsCommon;

        public DtoInfo[] Dtos { get; set; }
    }
}