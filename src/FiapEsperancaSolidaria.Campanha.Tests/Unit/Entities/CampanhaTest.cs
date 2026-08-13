using FiapEsperancaSolidaria.Campanha.Domain.Exceptions;
using FluentAssertions;
using Xunit;
using CampanhaEntity = FiapEsperancaSolidaria.Campanha.Domain.Entities.Campanha;
using StatusCampanha = FiapEsperancaSolidaria.Campanha.Domain.Enums.StatusCampanha;

namespace FiapEsperancaSolidaria.Campanha.Tests.Unit.Entities;

public class CampanhaTest
{
    [Fact]
    public void Criar_WhenDadosValidos_ShouldCriarCampanhaAtiva()
    {
        // Arrange
        var dataFim = DateTime.UtcNow.AddDays(30);

        // Act
        var campanha = CampanhaEntity.Criar("Título", "Descrição", DateTime.UtcNow, dataFim, 1000m);

        // Assert
        campanha.Status.Should().Be(StatusCampanha.Ativa);
        campanha.ValorTotalArrecadado.Should().Be(0);
    }

    [Fact]
    public void Criar_WhenDataFimNoPassado_ShouldThrowDomainException()
    {
        // Arrange
        var dataFim = DateTime.UtcNow.AddDays(-1);

        // Act
        var act = () => CampanhaEntity.Criar("Título", "Descrição", DateTime.UtcNow, dataFim, 1000m);

        // Assert
        act.Should().Throw<DomainException>().WithMessage("*data de término*");
    }

    [Fact]
    public void Criar_WhenMetaFinanceiraMenorOuIgualAZero_ShouldThrowDomainException()
    {
        // Arrange
        var dataFim = DateTime.UtcNow.AddDays(30);

        // Act
        var act = () => CampanhaEntity.Criar("Título", "Descrição", DateTime.UtcNow, dataFim, 0m);

        // Assert
        act.Should().Throw<DomainException>().WithMessage("*meta financeira*");
    }

    [Fact]
    public void PodeReceberDoacao_WhenCampanhaAtiva_ShouldReturnTrue()
    {
        // Arrange
        var campanha = CampanhaEntity.Criar("Título", "Descrição", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1000m);

        // Act & Assert
        campanha.PodeReceberDoacao().Should().BeTrue();
    }

    [Fact]
    public void PodeReceberDoacao_WhenCampanhaCancelada_ShouldReturnFalse()
    {
        // Arrange
        var campanha = CampanhaEntity.Criar("Título", "Descrição", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1000m);
        campanha.Cancelar();

        // Act & Assert
        campanha.PodeReceberDoacao().Should().BeFalse();
    }

    [Fact]
    public void Concluir_WhenCampanhaCancelada_ShouldThrowBusinessException()
    {
        // Arrange
        var campanha = CampanhaEntity.Criar("Título", "Descrição", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1000m);
        campanha.Cancelar();

        // Act
        var act = () => campanha.Concluir();

        // Assert
        act.Should().Throw<BusinessException>();
    }
}
