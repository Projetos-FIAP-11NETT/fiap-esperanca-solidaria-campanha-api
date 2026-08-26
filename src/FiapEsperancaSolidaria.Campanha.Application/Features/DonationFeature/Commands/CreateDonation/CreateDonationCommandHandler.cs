using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.DonationAggregate;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.DonationFeature.Commands.CreateDonation;

public class CreateDonationCommandHandler(
        IDonationRepository donationRepository
    ) : IRequestHandler<CreateDonationCommand, DonationResponse>
{
    public async Task<DonationResponse> Handle(CreateDonationCommand request, CancellationToken cancellationToken)
    {
        var donation = new Donation(
            request.CampaignId,
            request.DonorId,
            request.Amount,
            request.PaymentMethod
        );

        await donationRepository.AddAsync(donation);

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