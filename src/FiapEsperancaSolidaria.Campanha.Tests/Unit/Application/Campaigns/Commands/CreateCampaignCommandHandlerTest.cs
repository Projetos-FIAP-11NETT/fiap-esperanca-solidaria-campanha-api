using FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Commands.CreateCampaign;
using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.CampaignAggregate;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Cache;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FluentAssertions;
using Moq;

namespace FiapEsperancaSolidaria.Campanha.Tests.Unit.Application.Campaigns.Commands;

public class CreateCampaignCommandHandlerTest
{
    private readonly Mock<ICampaignRepository> _campaignRepositoryMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    [Fact]
    public async Task Handle_WhenDataIsValid_ShouldCreateCampaignAndCallRepository()
    {
        // Arrange
        var handler = new CreateCampaignCommandHandler(_campaignRepositoryMock.Object, _cacheServiceMock.Object);
        var command = new CreateCampaignCommand(
            "Title",
            "Description",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            1000m,
            null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Title.Should().Be(command.Title);
        result.Status.Should().Be(CampaignStatus.Active.ToString());
        result.TotalRaised.Should().Be(0);

        _campaignRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Campaign>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _cacheServiceMock.Verify(
            c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
