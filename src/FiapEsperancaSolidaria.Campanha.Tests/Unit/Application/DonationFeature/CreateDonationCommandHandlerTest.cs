using FiapEsperancaSolidaria.Campanha.Application.Features.DonationFeature.Commands.CreateDonation;
using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.CampaignAggregate;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Notifications;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FiapEsperancaSolidaria.Campanha.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace FiapEsperancaSolidaria.Campanha.Tests.Unit.Application.DonationFeature;

public class CreateDonationCommandHandlerTest
{
    private readonly Mock<ICampaignRepository> _campaignRepositoryMock = new();
    private readonly Mock<IDonationCreatedNotification> _donationCreatedNotificationMock = new();

    [Fact]
    public async Task Handle_WhenRequestIsValid_ShouldCreateDonationUpdateCampaignAndPublishNotification()
    {
        // Arrange
        var campaign = Campaign.Create("Campanha Teste", "Descrição", DateTime.UtcNow, DateTime.UtcNow.AddDays(10), 1000m);
        var command = new CreateDonationCommand(campaign.CampaignId, Guid.NewGuid(), 150m, PaymentMethod.Pix);

        _campaignRepositoryMock
            .Setup(r => r.GetByIdAsync(command.CampaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var handler = new CreateDonationCommandHandler(_campaignRepositoryMock.Object, _donationCreatedNotificationMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.CampaignId.Should().Be(command.CampaignId);
        result.DonorId.Should().Be(command.DonorId);
        result.Amount.Should().Be(command.Amount);
        result.PaymentMethod.Should().Be(command.PaymentMethod);
        result.Status.Should().Be(DonationStatus.Pending);

        _campaignRepositoryMock.Verify(r => r.UpdateAsync(campaign, It.IsAny<CancellationToken>()), Times.Once);
        _donationCreatedNotificationMock.Verify(n => n.PublishAsync(result.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCampaignDoesNotExist_ShouldThrowBusinessException()
    {
        // Arrange
        var command = new CreateDonationCommand(Guid.NewGuid(), Guid.NewGuid(), 100m, PaymentMethod.CreditCard);

        _campaignRepositoryMock
            .Setup(r => r.GetByIdAsync(command.CampaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Campaign?)null);

        var handler = new CreateDonationCommandHandler(_campaignRepositoryMock.Object, _donationCreatedNotificationMock.Object);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Campanha não encontrada.");

        _campaignRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Campaign>(), It.IsAny<CancellationToken>()), Times.Never);
        _donationCreatedNotificationMock.Verify(n => n.PublishAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUpdateFails_ShouldThrowBusinessException()
    {
        // Arrange
        var campaign = Campaign.Create("Campanha Teste", "Descrição", DateTime.UtcNow, DateTime.UtcNow.AddDays(10), 1000m);
        var command = new CreateDonationCommand(campaign.CampaignId, Guid.NewGuid(), 80m, PaymentMethod.Boleto);

        _campaignRepositoryMock
            .Setup(r => r.GetByIdAsync(command.CampaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        _campaignRepositoryMock
            .Setup(r => r.UpdateAsync(campaign, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var handler = new CreateDonationCommandHandler(_campaignRepositoryMock.Object, _donationCreatedNotificationMock.Object);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Doação não pôde ser salva.");

        _donationCreatedNotificationMock.Verify(n => n.PublishAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNotificationPublishFails_ShouldThrowBusinessException()
    {
        // Arrange
        var campaign = Campaign.Create("Campanha Teste", "Descrição", DateTime.UtcNow, DateTime.UtcNow.AddDays(10), 1000m);
        var command = new CreateDonationCommand(campaign.CampaignId, Guid.NewGuid(), 120m, PaymentMethod.DebitCard);

        _campaignRepositoryMock
            .Setup(r => r.GetByIdAsync(command.CampaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        _donationCreatedNotificationMock
            .Setup(n => n.PublishAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("queue error"));

        var handler = new CreateDonationCommandHandler(_campaignRepositoryMock.Object, _donationCreatedNotificationMock.Object);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Pagamento não pôde ser processado.");

        _campaignRepositoryMock.Verify(r => r.UpdateAsync(campaign, It.IsAny<CancellationToken>()), Times.Once);
    }
}