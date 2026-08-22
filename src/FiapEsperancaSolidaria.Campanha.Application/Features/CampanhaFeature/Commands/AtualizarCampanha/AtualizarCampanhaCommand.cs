using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Domain.Enums;
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
    StatusCampanha Status) : IRequest<CampanhaResponse>;
