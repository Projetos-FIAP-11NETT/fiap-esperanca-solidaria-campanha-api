using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampanhaFeature.Commands.AtualizarCampanha;

public sealed record AtualizarCampanhaCommand(
    Guid Id,
    string Titulo,
    string Descricao,
    DateTime DataInicio,
    DateTime DataFim,
    decimal MetaFinanceira,
    string? Imagem,
    string Status) : IRequest<CampanhaResponse>;
