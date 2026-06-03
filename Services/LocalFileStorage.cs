using TaskFlow.Api.Models;

namespace TaskFlow.Api.Services;

public class LocalFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(IConfiguration configuration)
    {
        _rootPath = configuration["FileStorage:Path"] ?? "uploads";
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<StoredFile> SaveAsync(Guid taskId, IFormFile file, CancellationToken cancellationToken)
    {
        var fileId = Guid.NewGuid();
        var extension = Path.GetExtension(file.FileName);
        var storedName = $"{fileId}{extension}";
        var relativePath = Path.Combine(taskId.ToString(), storedName);
        var fullPath = Path.Combine(_rootPath, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream, cancellationToken);

        return new StoredFile
        {
            Id = fileId,
            TaskId = taskId,
            FileName = Path.GetFileName(file.FileName),
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType,
            StoredPath = relativePath,
            SizeBytes = file.Length,
            CreatedAt = DateTime.UtcNow
        };
    }

    public string GetFullPath(StoredFile file) => Path.Combine(_rootPath, file.StoredPath);

    public void Delete(StoredFile file)
    {
        var fullPath = GetFullPath(file);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
