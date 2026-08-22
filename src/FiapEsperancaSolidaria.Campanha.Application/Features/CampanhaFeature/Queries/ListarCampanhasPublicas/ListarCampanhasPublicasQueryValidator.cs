using FluentValidation;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampanhaFeature.Queries.ListarCampanhasPublicas;

public class ListarCampanhasPublicasQueryValidator : AbstractValidator<ListarCampanhasPublicasQuery>
{
    public ListarCampanhasPublicasQueryValidator()
    {
        RuleFor(x => x.Titulo)
            .MaximumLength(200).WithMessage("O termo de busca deve ter no máximo 200 caracteres.")
            .When(x => x.Titulo is not null);
    }
}
