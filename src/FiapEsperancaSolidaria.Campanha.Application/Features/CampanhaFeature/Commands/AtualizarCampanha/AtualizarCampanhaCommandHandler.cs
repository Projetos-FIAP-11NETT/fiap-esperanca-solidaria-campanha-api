using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FiapEsperancaSolidaria.Campanha.Domain.Enums;
using FiapEsperancaSolidaria.Campanha.Domain.Exceptions;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampanhaFeature.Commands.AtualizarCampanha;

public class AtualizarCampanhaCommandHandler : IRequestHandler<AtualizarCampanhaCommand, CampanhaResponse>
{
    private readonly ICampanhaRepository _campanhaRepository;

    public AtualizarCampanhaCommandHandler(ICampanhaRepository campanhaRepository)
    {
        _campanhaRepository = campanhaRepository;
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

        var novoStatus = Enum.Parse<StatusCampanha>(request.Status, ignoreCase: true);
        campanha.AlterarStatus(novoStatus);

        await _campanhaRepository.AtualizarAsync(campanha, cancellationToken);

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
