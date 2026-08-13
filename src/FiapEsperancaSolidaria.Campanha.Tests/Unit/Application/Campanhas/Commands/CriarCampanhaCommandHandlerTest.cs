using FiapEsperancaSolidaria.Campanha.Application.Features.CampanhaFeature.Commands.CriarCampanha;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FiapEsperancaSolidaria.Campanha.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;
using CampanhaEntity = FiapEsperancaSolidaria.Campanha.Domain.Entities.Campanha;

namespace FiapEsperancaSolidaria.Campanha.Tests.Unit.Application.Campanhas.Commands;

public class CriarCampanhaCommandHandlerTest
{
    private readonly Mock<ICampanhaRepository> _campanhaRepositoryMock = new();

    [Fact]
    public async Task Handle_WhenDadosValidos_ShouldCriarCampanhaEChamarRepositorio()
    {
        // Arrange
        var handler = new CriarCampanhaCommandHandler(_campanhaRepositoryMock.Object);
        var command = new CriarCampanhaCommand(
            "Título",
            "Descrição",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            1000m,
            null);

        // Act
        var resultado = await handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.Titulo.Should().Be(command.Titulo);
        resultado.Status.Should().Be(StatusCampanha.Ativa.ToString());
        resultado.ValorTotalArrecadado.Should().Be(0);

        _campanhaRepositoryMock.Verify(
            r => r.AdicionarAsync(It.IsAny<CampanhaEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
