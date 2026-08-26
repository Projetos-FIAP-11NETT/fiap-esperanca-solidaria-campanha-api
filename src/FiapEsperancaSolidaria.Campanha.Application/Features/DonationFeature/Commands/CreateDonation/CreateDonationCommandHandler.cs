using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FiapEsperancaSolidaria.Campanha.Domain.Exceptions;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.DonationFeature.Commands.CreateDonation;

public class CreateDonationCommandHandler(
        ICampaignRepository campaignRepository
    ) : IRequestHandler<CreateDonationCommand, DonationResponse>
{
    public async Task<DonationResponse> Handle(CreateDonationCommand request, CancellationToken cancellationToken)
    {
        var campaign = await campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken)
            ?? throw new NotFoundException($"Campanha '{request.CampaignId}' não encontrada.");

        var donation = campaign.AddDonation(request.DonorId, request.Amount, request.PaymentMethod);

        await campaignRepository.UpdateAsync(campaign, cancellationToken);

        return new DonationResponse
        (
            donation.Id,
            donation.CampaignId,
            donation.DonorId,
            donation.Amount,
            donation.PaymentMethod,
            donation.DonationStatus,
            donation.CreatedAt
        );
    }
}
