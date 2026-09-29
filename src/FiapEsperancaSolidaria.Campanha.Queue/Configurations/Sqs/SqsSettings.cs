namespace FiapEsperancaSolidaria.Campanha.Queue.Configurations.Sqs;

public class SqsSettings
{
    public string Region { get; set; } = "us-east-1";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string ServiceUrl { get; set; } = string.Empty;
    public string EmailQueueUrl { get; set; } = string.Empty;
}