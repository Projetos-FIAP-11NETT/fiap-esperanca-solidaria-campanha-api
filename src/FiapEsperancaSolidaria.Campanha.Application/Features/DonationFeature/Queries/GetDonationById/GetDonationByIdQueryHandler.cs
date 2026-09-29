using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Identity;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FiapEsperancaSolidaria.Campanha.Domain.Exceptions;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.DonationFeature.Queries.GetDonationById;

public class GetDonationByIdQueryHandler(
        IDonationRepository donationRepository,
        ICurrentUserService currentUserService
    ) : IRequestHandler<GetDonationByIdQuery, DonationResponse>
{
    public async Task<DonationResponse> Handle(GetDonationByIdQuery request, CancellationToken cancellationToken)
    {
        var donation = await donationRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Doação '{request.Id}' não encontrada.");

        var isOwner = currentUserService.UserId == donation.DonorId;
        var isGestorOng = currentUserService.IsInRole("GestorONG");

        if (!isOwner && !isGestorOng)
            throw new UnauthorizedAccessException("Você não tem permissão para ver esta doação.");

        return new DonationResponse(
            donation.Id,
            donation.CampaignId,
            donation.DonorId,
            donation.Amount,
            donation.PaymentMethod,
            donation.DonationStatus,
            donation.CreatedAt);
    }
}
