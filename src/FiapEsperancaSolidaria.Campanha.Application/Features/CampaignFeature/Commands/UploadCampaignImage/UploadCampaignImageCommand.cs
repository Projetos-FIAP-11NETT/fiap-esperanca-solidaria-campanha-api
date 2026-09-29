using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Commands.UploadCampaignImage;

public sealed record UploadCampaignImageCommand(
    Stream Content,
    string FileName,
    string ContentType) : IRequest<UploadCampaignImageResponse>;
