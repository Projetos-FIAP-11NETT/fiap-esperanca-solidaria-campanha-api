namespace FiapEsperancaSolidaria.Campanha.Application.DTOs;

public record CampanhaPublicaResponse(
    Guid Id,
    string Titulo,
    decimal MetaFinanceira,
    decimal ValorTotalArrecadado);
