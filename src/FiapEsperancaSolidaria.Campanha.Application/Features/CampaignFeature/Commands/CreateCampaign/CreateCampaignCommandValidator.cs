using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FluentValidation;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Commands.CreateCampaign;

public class CreateCampaignCommandValidator : AbstractValidator<CreateCampaignCommand>
{
    public CreateCampaignCommandValidator(ICampaignRepository campaignRepository)
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(200)
            .MustAsync(async (title, cancellationToken) =>
                !await campaignRepository.ExistsWithTitleAsync(title, cancellationToken: cancellationToken))
            .WithMessage("Já existe uma campanha com esse título.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A descrição é obrigatória.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("A data de término deve ser posterior à data de início.")
            .Must(endDate => endDate.Date >= DateTime.UtcNow.Date)
            .WithMessage("A data de término não pode estar no passado.");

        RuleFor(x => x.FinancialGoal)
            .GreaterThan(0).WithMessage("A meta financeira deve ser maior que zero.");
    }
}
