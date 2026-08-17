using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FiapEsperancaSolidaria.Campanha.Domain.Exceptions;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampanhaFeature.Queries.ObterCampanhaPorId;

public class ObterCampanhaPorIdQueryHandler : IRequestHandler<ObterCampanhaPorIdQuery, CampanhaResponse>
{
    private readonly ICampanhaRepository _campanhaRepository;

    public ObterCampanhaPorIdQueryHandler(ICampanhaRepository campanhaRepository)
    {
        _campanhaRepository = campanhaRepository;
    }

    public async Task<CampanhaResponse> Handle(ObterCampanhaPorIdQuery request, CancellationToken cancellationToken)
    {
        var campanha = await _campanhaRepository.ObterPorIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Campanha '{request.Id}' não encontrada.");

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
