using FluentValidation;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampanhaFeature.Commands.CriarCampanha;

public class CriarCampanhaCommandValidator : AbstractValidator<CriarCampanhaCommand>
{
    public CriarCampanhaCommandValidator()
    {
        RuleFor(x => x.Titulo)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(200);

        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("A descrição é obrigatória.");

        RuleFor(x => x.DataFim)
            .GreaterThanOrEqualTo(x => x.DataInicio).WithMessage("A data de término deve ser posterior à data de início.")
            .Must(dataFim => dataFim.Date >= DateTime.UtcNow.Date)
            .WithMessage("A data de término não pode estar no passado.");

        RuleFor(x => x.MetaFinanceira)
            .GreaterThan(0).WithMessage("A meta financeira deve ser maior que zero.");
    }
}
