using FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Commands.CancelCampaign;
using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.CampaignAggregate;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Cache;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FiapEsperancaSolidaria.Campanha.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace FiapEsperancaSolidaria.Campanha.Tests.Unit.Application.Campaigns.Commands;

public class CancelCampaignCommandHandlerTest
{
    private readonly Mock<ICampaignRepository> _campaignRepositoryMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    [Fact]
    public async Task Handle_WhenCampaignExists_ShouldCancelAndCallRepository()
    {
        // Arrange
        var campaign = Campaign.Create("Title", "Description", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1000m);
        _campaignRepositoryMock
            .Setup(r => r.GetByIdAsync(campaign.CampaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var handler = new CancelCampaignCommandHandler(_campaignRepositoryMock.Object, _cacheServiceMock.Object);

        // Act
        var result = await handler.Handle(new CancelCampaignCommand(campaign.CampaignId), CancellationToken.None);

        // Assert
        result.Status.Should().Be(CampaignStatus.Cancelled.ToString());

        _campaignRepositoryMock.Verify(
            r => r.UpdateAsync(campaign, It.IsAny<CancellationToken>()),
            Times.Once);

        _cacheServiceMock.Verify(
            c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WhenCampaignDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        _campaignRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Campaign?)null);

        var handler = new CancelCampaignCommandHandler(_campaignRepositoryMock.Object, _cacheServiceMock.Object);

        // Act
        var act = () => handler.Handle(new CancelCampaignCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
