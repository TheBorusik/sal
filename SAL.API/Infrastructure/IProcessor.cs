namespace SAL.API
{
    public interface IProcessor
    {
        void Start();
        void Online();
        void Offline();
        void Stop();
    }
}