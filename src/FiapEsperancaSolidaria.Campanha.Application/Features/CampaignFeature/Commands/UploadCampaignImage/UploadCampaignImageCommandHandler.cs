using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Storage;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Commands.UploadCampaignImage;

public class UploadCampaignImageCommandHandler(
        IImageStorageService imageStorageService
    ) : IRequestHandler<UploadCampaignImageCommand, UploadCampaignImageResponse>
{
    public async Task<UploadCampaignImageResponse> Handle(
        UploadCampaignImageCommand request,
        CancellationToken cancellationToken)
    {
        var url = await imageStorageService.UploadAsync(
            request.Content,
            request.FileName,
            request.ContentType,
            cancellationToken);

        return new UploadCampaignImageResponse(url);
    }
}
