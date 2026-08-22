using FluentValidation;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.DonationFeature.Commands.CreateDonation;

public class CreateDonationCommandValidator : AbstractValidator<CreateDonationCommand>
{
    public CreateDonationCommandValidator() 
    {
        RuleFor(x => x.CampaignId)
            .NotEmpty()
            .WithMessage("A campanha é obrigatória.");
        
        RuleFor(x => x.DonorId)
            .NotEmpty()
            .WithMessage("O doador é obrigatório.");
        
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("O valor da doação deve ser maior que zero.");
        
        RuleFor(x => x.PaymentMethod)
            .IsInEnum()
            .WithMessage("O método de pagamento é inválido.");
    }
}