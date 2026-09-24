using FiapEsperancaSolidaria.Campanha.Application.Features.DonationFeature.Commands.CreateDonation;
using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.CampaignAggregate;
using FluentAssertions;

namespace FiapEsperancaSolidaria.Campanha.Tests.Unit.Application.DonationFeature;

public class CreateDonationCommandValidatorTest
{
    private readonly CreateDonationCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveErrors()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WhenCampaignIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { CampaignId = Guid.Empty };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(CreateDonationCommand.CampaignId) &&
            e.ErrorMessage == "A campanha é obrigatória.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Validate_WhenAmountIsNotGreaterThanZero_ShouldHaveError(decimal invalidAmount)
    {
        // Arrange
        var command = CreateValidCommand() with { Amount = invalidAmount };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(CreateDonationCommand.Amount) &&
            e.ErrorMessage == "O valor da doação deve ser maior que zero.");
    }

    [Fact]
    public void Validate_WhenPaymentMethodIsInvalid_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { PaymentMethod = (PaymentMethod)99 };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(CreateDonationCommand.PaymentMethod) &&
            e.ErrorMessage == "O método de pagamento é inválido.");
    }

    private static CreateDonationCommand CreateValidCommand() =>
        new(
            CampaignId: Guid.NewGuid(),
            Amount: 100m,
            PaymentMethod: PaymentMethod.Pix
        );
}