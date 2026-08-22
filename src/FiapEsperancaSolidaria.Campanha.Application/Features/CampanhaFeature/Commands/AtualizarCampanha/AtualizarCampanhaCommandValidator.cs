using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FluentValidation;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampanhaFeature.Commands.AtualizarCampanha;

public class AtualizarCampanhaCommandValidator : AbstractValidator<AtualizarCampanhaCommand>
{
    public AtualizarCampanhaCommandValidator(ICampanhaRepository campanhaRepository)
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Titulo)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(200)
            .MustAsync(async (command, titulo, cancellationToken) =>
                !await campanhaRepository.ExisteComTituloAsync(titulo, command.Id, cancellationToken))
            .WithMessage("Já existe uma campanha com esse título.");

        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("A descrição é obrigatória.");

        RuleFor(x => x.DataFim)
            .GreaterThanOrEqualTo(x => x.DataInicio).WithMessage("A data de término deve ser posterior à data de início.");

        RuleFor(x => x.MetaFinanceira)
            .GreaterThan(0).WithMessage("A meta financeira deve ser maior que zero.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status inválido. Valores aceitos: Ativa, Concluida, Cancelada.");
    }
}
