using FiapEsperancaSolidaria.Campanha.Application.Behaviors;
using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Cache;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FiapEsperancaSolidaria.Campanha.Domain.Exceptions;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampanhaFeature.Commands.AtualizarCampanha;

public class AtualizarCampanhaCommandHandler : IRequestHandler<AtualizarCampanhaCommand, CampanhaResponse>
{
    private readonly ICampanhaRepository _campanhaRepository;
    private readonly ICacheService _cacheService;

    public AtualizarCampanhaCommandHandler(ICampanhaRepository campanhaRepository, ICacheService cacheService)
    {
        _campanhaRepository = campanhaRepository;
        _cacheService = cacheService;
    }

    public async Task<CampanhaResponse> Handle(AtualizarCampanhaCommand request, CancellationToken cancellationToken)
    {
        var campanha = await _campanhaRepository.ObterPorIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Campanha '{request.Id}' não encontrada.");

        campanha.AtualizarDados(
            request.Titulo,
            request.Descricao,
            request.DataInicio,
            request.DataFim,
            request.MetaFinanceira,
            request.Imagem);

        campanha.AlterarStatus(request.Status);

        await _campanhaRepository.AtualizarAsync(campanha, cancellationToken);

        await _cacheService.RemoverAsync(CacheKeys.CampanhasPublicas(), cancellationToken);
        await _cacheService.RemoverAsync(CacheKeys.Campanha(campanha.Id), cancellationToken);

        return new CampanhaResponse(
            campanha.Id,
            campanha.Titulo,
            campanha.Descricao,
            campanha.DataInicio,
            campanha.DataFim,
            campanha.Imagem,
            campanha.MetaFinanceira,
            campanha.Status.ToString(),
            campanha.ValorTotalArrecadado);
    }
}
