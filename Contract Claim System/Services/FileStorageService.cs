using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Contract_Claim_System.Services
{
    public interface IFileStorageService
    {
        bool ValidateFile(IFormFile file);
        Task<string> ProcessAndStoreAsync(IFormFile file);
        Task<string> GetStoredFileContentAsync(string fileName);
        string GetFilePath(string fileName);
    }
}