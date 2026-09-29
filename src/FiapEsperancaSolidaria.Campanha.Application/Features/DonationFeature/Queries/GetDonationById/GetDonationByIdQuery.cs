using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.DonationFeature.Queries.GetDonationById;

public sealed record GetDonationByIdQuery(Guid Id) : IRequest<DonationResponse>;
