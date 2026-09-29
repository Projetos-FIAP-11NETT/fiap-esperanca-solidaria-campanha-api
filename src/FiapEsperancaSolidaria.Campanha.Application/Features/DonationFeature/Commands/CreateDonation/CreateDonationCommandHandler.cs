using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Identity;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Notifications;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FiapEsperancaSolidaria.Campanha.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.DonationFeature.Commands.CreateDonation;

public class CreateDonationCommandHandler(
        ICampaignRepository campaignRepository,
        IDonationCreatedNotification donationCreatedNotification,
        ICurrentUserService currentUserService,
        ILogger<CreateDonationCommandHandler> logger
    ) : IRequestHandler<CreateDonationCommand, DonationResponse>
{
    public async Task<DonationResponse> Handle(CreateDonationCommand request, CancellationToken cancellationToken)
    {
        var donorId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Usuário não autenticado.");

        var campaign = await campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken);
        if (campaign is null)
            throw new BusinessException("Campanha não encontrada.");

        var donation = campaign.AddDonation(donorId, request.Amount, request.PaymentMethod);

        try
        {
            await campaignRepository.UpdateAsync(campaign, cancellationToken);
        }
        catch
        {
            throw new BusinessException("Doação não pôde ser salva.");
        }

        try
        {
            await donationCreatedNotification.PublishAsync(donation.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao publicar notificação da doação {DonationId}.", donation.Id);
        }

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