namespace FiapEsperancaSolidaria.Campanha.Queue.Configurations.MassTransit;

public class MassTransitSettings
{
    public int RetryCount { get; set; }
    public int Interval { get; set; }
}