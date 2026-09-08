using FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Commands.UpdateCampaignStatuses;
using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.CampaignAggregate;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Cache;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FluentAssertions;
using Moq;

namespace FiapEsperancaSolidaria.Campanha.Tests.Unit.Application.Campaigns.Commands;

public class UpdateCampaignStatusesCommandHandlerTest
{
    private readonly Mock<ICampaignRepository> _campaignRepositoryMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    [Fact]
    public async Task Handle_ShouldActivateScheduledAndCompleteActiveCampaigns()
    {
        // Arrange
        var scheduledCampaign = Campaign.Create("Scheduled", "Description", DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(30), 1000m);
        var activeCampaign = Campaign.Create("Active", "Description", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1000m);

        _campaignRepositoryMock
            .Setup(r => r.ListPendingStatusUpdateAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([scheduledCampaign, activeCampaign]);

        var handler = new UpdateCampaignStatusesCommandHandler(_campaignRepositoryMock.Object, _cacheServiceMock.Object);

        // Act
        var result = await handler.Handle(new UpdateCampaignStatusesCommand(), CancellationToken.None);

        // Assert
        result.Should().Be(2);
        scheduledCampaign.Status.Should().Be(CampaignStatus.Active);
        activeCampaign.Status.Should().Be(CampaignStatus.Completed);

        _campaignRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task Handle_WhenNoCampaignsPending_ShouldNotSaveOrInvalidateCache()
    {
        // Arrange
        _campaignRepositoryMock
            .Setup(r => r.ListPendingStatusUpdateAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new UpdateCampaignStatusesCommandHandler(_campaignRepositoryMock.Object, _cacheServiceMock.Object);

        // Act
        var result = await handler.Handle(new UpdateCampaignStatusesCommand(), CancellationToken.None);

        // Assert
        result.Should().Be(0);

        _campaignRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _cacheServiceMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
