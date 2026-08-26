using FiapEsperancaSolidaria.Campanha.Domain.Entities;
using FiapEsperancaSolidaria.Campanha.Domain.Enums;
using FiapEsperancaSolidaria.Campanha.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace FiapEsperancaSolidaria.Campanha.Tests.Unit.Entities;

public class CampaignTest
{
    [Fact]
    public void Create_WhenDataIsValid_ShouldCreateActiveCampaign()
    {
        // Arrange
        var endDate = DateTime.UtcNow.AddDays(30);

        // Act
        var campaign = Campaign.Create("Title", "Description", DateTime.UtcNow, endDate, 1000m);

        // Assert
        campaign.Status.Should().Be(CampaignStatus.Active);
        campaign.TotalRaised.Should().Be(0);
    }

    [Fact]
    public void Create_WhenEndDateIsInThePast_ShouldThrowDomainException()
    {
        // Arrange
        var endDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var act = () => Campaign.Create("Title", "Description", DateTime.UtcNow, endDate, 1000m);

        // Assert
        act.Should().Throw<DomainException>().WithMessage("*data de término*");
    }

    [Fact]
    public void Create_WhenFinancialGoalIsLessThanOrEqualToZero_ShouldThrowDomainException()
    {
        // Arrange
        var endDate = DateTime.UtcNow.AddDays(30);

        // Act
        var act = () => Campaign.Create("Title", "Description", DateTime.UtcNow, endDate, 0m);

        // Assert
        act.Should().Throw<DomainException>().WithMessage("*meta financeira*");
    }

    [Fact]
    public void CanReceiveDonation_WhenCampaignIsActive_ShouldReturnTrue()
    {
        // Arrange
        var campaign = Campaign.Create("Title", "Description", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1000m);

        // Act & Assert
        campaign.CanReceiveDonation().Should().BeTrue();
    }

    [Fact]
    public void CanReceiveDonation_WhenCampaignIsCancelled_ShouldReturnFalse()
    {
        // Arrange
        var campaign = Campaign.Create("Title", "Description", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1000m);
        campaign.Cancel();

        // Act & Assert
        campaign.CanReceiveDonation().Should().BeFalse();
    }

    [Fact]
    public void Complete_WhenCampaignIsCancelled_ShouldThrowBusinessException()
    {
        // Arrange
        var campaign = Campaign.Create("Title", "Description", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1000m);
        campaign.Cancel();

        // Act
        var act = () => campaign.Complete();

        // Assert
        act.Should().Throw<BusinessException>();
    }
}
