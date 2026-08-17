using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampanhaFeature.Commands.CriarCampanha;

public class CriarCampanhaCommandHandler : IRequestHandler<CriarCampanhaCommand, CampanhaResponse>
{
    private readonly ICampanhaRepository _campanhaRepository;

    public CriarCampanhaCommandHandler(ICampanhaRepository campanhaRepository)
    {
        _campanhaRepository = campanhaRepository;
    }

    public async Task<CampanhaResponse> Handle(CriarCampanhaCommand request, CancellationToken cancellationToken)
    {
        var campanha = Domain.Entities.Campanha.Criar(
            request.Titulo,
            request.Descricao,
            request.DataInicio,
            request.DataFim,
            request.MetaFinanceira,
            request.Imagem);

        await _campanhaRepository.AdicionarAsync(campanha, cancellationToken);

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
