using FluentValidation;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Queries.ListPublicCampaigns;

public class ListPublicCampaignsQueryValidator : AbstractValidator<ListPublicCampaignsQuery>
{
    public ListPublicCampaignsQueryValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("O termo de busca deve ter no máximo 200 caracteres.")
            .When(x => x.Title is not null);
    }
}
