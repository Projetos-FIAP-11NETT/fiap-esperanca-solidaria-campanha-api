namespace FiapEsperancaSolidaria.Campanha.Application.DTOs;

public record CampanhaResponse(
    Guid Id,
    string Titulo,
    string Descricao,
    DateTime DataInicio,
    DateTime DataFim,
    string? Imagem,
    decimal MetaFinanceira,
    string Status,
    decimal ValorTotalArrecadado);
