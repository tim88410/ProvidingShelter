using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;


namespace ProvidingShelter.Infrastructure.Service.ExternalService
{
    public interface IFileStorageService
    {
        Task<(string storedFullPath, string fileHashSha256)> SaveAsync(
            IFormFile file,
            string basePath,
            CancellationToken ct = default);
    }
    public sealed class FileStorageService : IFileStorageService
    {
        public async Task<(string storedFullPath, string fileHashSha256)> SaveAsync(
            IFormFile file,
            string basePath,
            CancellationToken ct = default)
        {
            Directory.CreateDirectory(basePath);

            var importFolder = Path.Combine(basePath, DateTime.UtcNow.ToString("yyyyMMdd"));
            Directory.CreateDirectory(importFolder);

            var ext = Path.GetExtension(file.FileName);
            var safeName = Path.GetFileNameWithoutExtension(file.FileName);
            foreach (var c in Path.GetInvalidFileNameChars()) safeName = safeName.Replace(c, '_');

            var storedName = $"{safeName}_{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(importFolder, storedName);

            using (var fs = File.Create(fullPath))
            {
                await file.CopyToAsync(fs, ct);
            }

            // 計算 SHA256
            using var sha = SHA256.Create();
            await using var s = File.OpenRead(fullPath);
            var hash = await sha.ComputeHashAsync(s, ct);
            var hex = Convert.ToHexString(hash).ToLowerInvariant();

            return (fullPath, hex);
        }
    }
}
