namespace FiapEsperancaSolidaria.Campanha.Domain.Contracts.Storage;

public interface IImageStorageService
{
    Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
