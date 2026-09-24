using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.DonationFeature.Queries.ListMyDonations;

public sealed record ListMyDonationsQuery : IRequest<IReadOnlyList<DonationReceiptResponse>>;
