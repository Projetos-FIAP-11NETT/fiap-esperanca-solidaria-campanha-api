namespace FiapEsperancaSolidaria.Campanha.Infrastructure.Storage;

public class S3Settings
{
    public string Region { get; set; } = "us-east-1";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string ServiceUrl { get; set; } = string.Empty;

    // URL que o navegador do usuário consegue resolver — pode divergir do ServiceUrl
    // quando a API roda em container e usa host.docker.internal pra falar com o
    // LocalStack, mas o browser (fora do container) precisa de localhost. Se vazio,
    // cai no próprio ServiceUrl (caso de dotnet run direto no host).
    public string? PublicBaseUrl { get; set; }

    public string BucketName { get; set; } = "campanha-images";
}
