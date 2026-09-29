using FluentValidation;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Commands.CancelCampaign;

public class CancelCampaignCommandValidator : AbstractValidator<CancelCampaignCommand>
{
    public CancelCampaignCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
