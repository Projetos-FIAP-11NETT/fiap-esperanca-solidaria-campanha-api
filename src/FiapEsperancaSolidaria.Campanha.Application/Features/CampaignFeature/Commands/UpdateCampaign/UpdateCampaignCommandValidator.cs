using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FluentValidation;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Commands.UpdateCampaign;

public class UpdateCampaignCommandValidator : AbstractValidator<UpdateCampaignCommand>
{
    public UpdateCampaignCommandValidator(ICampaignRepository campaignRepository)
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(200)
            .MustAsync(async (command, title, cancellationToken) =>
                !await campaignRepository.ExistsWithTitleAsync(title, command.Id, cancellationToken))
            .WithMessage("Já existe uma campanha com esse título.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A descrição é obrigatória.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("A data de término deve ser posterior à data de início.");

        RuleFor(x => x.FinancialGoal)
            .GreaterThan(0).WithMessage("A meta financeira deve ser maior que zero.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status inválido. Valores aceitos: Active, Completed, Cancelled.");
    }
}
