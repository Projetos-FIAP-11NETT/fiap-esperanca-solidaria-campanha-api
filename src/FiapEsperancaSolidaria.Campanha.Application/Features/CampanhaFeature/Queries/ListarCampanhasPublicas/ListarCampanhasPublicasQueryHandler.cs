using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampanhaFeature.Queries.ListarCampanhasPublicas;

public class ListarCampanhasPublicasQueryHandler
    : IRequestHandler<ListarCampanhasPublicasQuery, IReadOnlyList<CampanhaPublicaResponse>>
{
    private readonly ICampanhaRepository _campanhaRepository;

    public ListarCampanhasPublicasQueryHandler(ICampanhaRepository campanhaRepository)
    {
        _campanhaRepository = campanhaRepository;
    }

    public async Task<IReadOnlyList<CampanhaPublicaResponse>> Handle(
        ListarCampanhasPublicasQuery request,
        CancellationToken cancellationToken)
    {
        var campanhas = await _campanhaRepository.ListarAtivasAsync(request.Titulo, cancellationToken);

        return campanhas
            .Select(c => new CampanhaPublicaResponse(c.Id, c.Titulo, c.MetaFinanceira, c.ValorTotalArrecadado))
            .ToList();
    }
}
