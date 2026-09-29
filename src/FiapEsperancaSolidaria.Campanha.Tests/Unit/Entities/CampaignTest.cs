using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.CampaignAggregate;
using FiapEsperancaSolidaria.Campanha.Domain.Exceptions;
using FluentAssertions;

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
    public void Create_WhenStartDateIsInTheFuture_ShouldCreateScheduledCampaign()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(5);
        var endDate = DateTime.UtcNow.AddDays(30);

        // Act
        var campaign = Campaign.Create("Title", "Description", startDate, endDate, 1000m);

        // Assert
        campaign.Status.Should().Be(CampaignStatus.Scheduled);
    }

    [Fact]
    public void Create_WhenStartDateIsToday_ShouldCreateActiveCampaign()
    {
        // Arrange
        var endDate = DateTime.UtcNow.AddDays(30);

        // Act
        var campaign = Campaign.Create("Title", "Description", DateTime.UtcNow.Date, endDate, 1000m);

        // Assert
        campaign.Status.Should().Be(CampaignStatus.Active);
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
    public void AddDonation_WhenCampaignIsActive_ShouldSucceed()
    {
        // Arrange
        var campaign = Campaign.Create("Title", "Description", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1000m);

        // Act
        var donation = campaign.AddDonation(Guid.NewGuid(), 100m, PaymentMethod.Pix);

        // Assert
        donation.Should().NotBeNull();
        campaign.Donations.Should().ContainSingle();
    }

    [Fact]
    public void AddDonation_WhenCampaignIsCancelled_ShouldThrow()
    {
        // Arrange
        var campaign = Campaign.Create("Title", "Description", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1000m);
        campaign.Cancel();

        // Act
        var act = () => campaign.AddDonation(Guid.NewGuid(), 100m, PaymentMethod.Pix);

        // Assert
        act.Should().Throw<BusinessException>();
    }

    [Fact]
    public void Activate_WhenCampaignIsScheduled_ShouldSetStatusToActive()
    {
        // Arrange
        var campaign = Campaign.Create("Title", "Description", DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(30), 1000m);

        // Act
        campaign.Activate();

        // Assert
        campaign.Status.Should().Be(CampaignStatus.Active);
    }

    [Fact]
    public void Activate_WhenCampaignIsNotScheduled_ShouldThrowBusinessException()
    {
        // Arrange
        var campaign = Campaign.Create("Title", "Description", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1000m);

        // Act
        var act = () => campaign.Activate();

        // Assert
        act.Should().Throw<BusinessException>();
    }

    [Fact]
    public void Cancel_WhenFinancialGoalWasNotReached_ShouldSetStatusToCancelled()
    {
        // Arrange
        var campaign = Campaign.Create("Title", "Description", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1000m);
        campaign.AddDonation(Guid.NewGuid(), 100m, PaymentMethod.Pix);

        // Act
        campaign.Cancel();

        // Assert
        campaign.Status.Should().Be(CampaignStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenFinancialGoalWasReached_ShouldSetStatusToCompletedInstead()
    {
        // Arrange
        var campaign = Campaign.Create("Title", "Description", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1000m);
        campaign.AddDonation(Guid.NewGuid(), 600m, PaymentMethod.Pix);
        campaign.AddDonation(Guid.NewGuid(), 400m, PaymentMethod.CreditCard);

        // Act
        campaign.Cancel();

        // Assert
        campaign.Status.Should().Be(CampaignStatus.Completed);
    }

    [Fact]
    public void Cancel_WhenTotalRaisedReachedGoal_ShouldPreferTotalRaisedOverDonationsSum()
    {
        // Arrange: TotalRaised é quem o doacao-work mantém — tem que ganhar da soma
        // local das Donations mesmo quando elas (sozinhas) não bateriam a meta.
        var campaign = Campaign.Create("Title", "Description", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1000m);
        campaign.AddDonation(Guid.NewGuid(), 100m, PaymentMethod.Pix);
        typeof(Campaign).GetProperty(nameof(Campaign.TotalRaised))!.SetValue(campaign, 1500m);

        // Act
        campaign.Cancel();

        // Assert
        campaign.Status.Should().Be(CampaignStatus.Completed);
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
