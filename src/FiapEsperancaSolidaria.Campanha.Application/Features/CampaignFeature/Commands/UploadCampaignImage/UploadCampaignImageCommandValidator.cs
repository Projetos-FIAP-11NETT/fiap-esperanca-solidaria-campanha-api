using FluentValidation;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Commands.UploadCampaignImage;

public class UploadCampaignImageCommandValidator : AbstractValidator<UploadCampaignImageCommand>
{
    public UploadCampaignImageCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("O nome do arquivo é obrigatório.");

        RuleFor(x => x.ContentType)
            .Must(contentType => contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            .WithMessage("O arquivo precisa ser uma imagem.");
    }
}
