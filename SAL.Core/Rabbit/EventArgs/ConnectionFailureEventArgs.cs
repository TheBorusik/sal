namespace SAL.Core.Rabbit.EventArgs
{
    public class ConnectionFailureEventArgs
    {
        public string Message { get; private set; }

        public ConnectionFailureEventArgs(string message)
        {
            Message = message;
        }
    }
}