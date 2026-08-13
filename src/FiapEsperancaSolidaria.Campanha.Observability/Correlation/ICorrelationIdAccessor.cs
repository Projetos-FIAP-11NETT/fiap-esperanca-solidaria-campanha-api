namespace FiapEsperancaSolidaria.Campanha.Observability.Correlation;

public interface ICorrelationIdAccessor
{
    string CorrelationId { get; }
}
