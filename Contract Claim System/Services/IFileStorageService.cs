using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contract_Claim_System.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private const string UploadsFolder = "UploadedFiles";
        private const int MaxFileSize = 5 * 1024 * 1024; // 5MB in bytes
        private static readonly string[] AllowedExtensions = new[] { ".pdf", ".docx", ".xlsx", ".txt" };

        public FileStorageService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        private string GetUploadPath()
        {
            var path = Path.Combine(_webHostEnvironment.ContentRootPath, UploadsFolder);
            Directory.CreateDirectory(path);
            return path;
        }

        public bool ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0 || file.Length > MaxFileSize)
            {
                return false;
            }
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            {
                return false;
            }
            return true;
        }

        public async Task<string> ProcessAndStoreAsync(IFormFile file)
        {
            var folderPath = GetUploadPath();
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.FileName);

            // Create a unique filename with the original file name and a guid, saving as .txt
            var uniqueFileName = $"{fileNameWithoutExt}_{Guid.NewGuid().ToString().Substring(0, 8)}.txt";
            var filePath = Path.Combine(folderPath, uniqueFileName);

            // Logic to extract and save ONLY text content
            string fileContent = string.Empty;

            try
            {
                using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, true, 1024, true))
                {
                    fileContent = await reader.ReadToEndAsync();
                }
            }
            catch (Exception)
            {
                // just save a placeholder message indicating the attempt.
                fileContent = $"[Could not extract text content from binary file: {file.FileName}]";
            }

            // Write the extracted text (or placeholder) to the new .txt file
            await System.IO.File.WriteAllTextAsync(filePath, fileContent);

            return uniqueFileName;
        }

        public async Task<string> GetStoredFileContentAsync(string fileName)
        {
            var filePath = GetFilePath(fileName);
            if (System.IO.File.Exists(filePath))
            {
                return await System.IO.File.ReadAllTextAsync(filePath);
            }
            return $"File not found: {fileName}";
        }

        public string GetFilePath(string fileName)
        {
            return Path.Combine(GetUploadPath(), fileName);
        }
    }
}