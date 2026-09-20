using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.CampaignAggregate;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Identity;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.DonationFeature.Queries.ListMyDonations;

public class ListMyDonationsQueryHandler(
        IDonationRepository donationRepository,
        ICampaignRepository campaignRepository,
        ICurrentUserService currentUserService
    ) : IRequestHandler<ListMyDonationsQuery, IReadOnlyList<DonationReceiptResponse>>
{
    public async Task<IReadOnlyList<DonationReceiptResponse>> Handle(ListMyDonationsQuery request, CancellationToken cancellationToken)
    {
        var donorId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Usuário não autenticado.");

        var donations = await donationRepository.ListByDonorAsync(donorId, cancellationToken);
        if (donations.Count == 0)
            return [];

        // Uma campanha, mesmo cancelada/concluída, continua existindo - só busca as
        // envolvidas (poucas, tipicamente) pra montar o recibo com o título/status delas.
        var campaigns = new Dictionary<Guid, Campaign>();
        foreach (var campaignId in donations.Select(d => d.CampaignId).Distinct())
        {
            var campaign = await campaignRepository.GetByIdAsync(campaignId, cancellationToken);
            if (campaign is not null)
                campaigns[campaignId] = campaign;
        }

        return donations
            .Where(d => campaigns.ContainsKey(d.CampaignId))
            .Select(d =>
            {
                var campaign = campaigns[d.CampaignId];
                return new DonationReceiptResponse(
                    d.Id,
                    d.CampaignId,
                    campaign.Title,
                    campaign.Status.ToString(),
                    d.Amount,
                    d.PaymentMethod,
                    d.DonationStatus,
                    d.CreatedAt);
            })
            .ToList();
    }
}
