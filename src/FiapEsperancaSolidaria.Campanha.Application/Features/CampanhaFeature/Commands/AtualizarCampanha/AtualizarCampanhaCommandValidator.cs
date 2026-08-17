using FiapEsperancaSolidaria.Campanha.Domain.Enums;
using FluentValidation;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampanhaFeature.Commands.AtualizarCampanha;

public class AtualizarCampanhaCommandValidator : AbstractValidator<AtualizarCampanhaCommand>
{
    public AtualizarCampanhaCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Titulo)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(200);

        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("A descrição é obrigatória.");

        RuleFor(x => x.DataFim)
            .GreaterThanOrEqualTo(x => x.DataInicio).WithMessage("A data de término deve ser posterior à data de início.");

        RuleFor(x => x.MetaFinanceira)
            .GreaterThan(0).WithMessage("A meta financeira deve ser maior que zero.");

        RuleFor(x => x.Status)
            .Must(status => Enum.TryParse<StatusCampanha>(status, ignoreCase: true, out _))
            .WithMessage("Status inválido. Valores aceitos: Ativa, Concluida, Cancelada.");
    }
}
