using System.Threading.Tasks;

namespace SAL.API
{
    public interface IS3Store
    {
        Task UploadFileAsync(string filePath, string fileId);
        Task DownloadFileAsync(string fileId, string filePath, long? byteLimit = null);
        Task DeleteFileAsync(string fileId);
        Task<bool> CheckSizeLimitAsync(string fileId, long byteLimit);

        Task<bool> IsFilePresent(string fileId);
    }
}