using System.Threading.Tasks;

namespace SAL.API
{
    public interface IStore
    {
        Task<bool> Add(string key, object value);
        Task<bool> TryPeek<T>(string key, out T value);
        Task<bool> TryTake<T>(string key, out T value);
        
        Task<bool> TryPeekRaw(string key, out string value);
        Task<bool> TryTakeRaw(string key, out string value);
    }
}