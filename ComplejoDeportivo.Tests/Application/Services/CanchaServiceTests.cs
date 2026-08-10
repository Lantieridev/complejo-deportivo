using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ComplejoDeportivo.Application.DTOs;
using ComplejoDeportivo.Application.Repositories;
using ComplejoDeportivo.Application.Services.Implementations;
using ComplejoDeportivo.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace ComplejoDeportivo.Tests.Application.Services
{
    public class CanchaServiceTests
    {
        private readonly Mock<ICanchaRepository> _canchaRepositoryMock;
        private readonly CanchaService _canchaService;

        public CanchaServiceTests()
        {
            _canchaRepositoryMock = new Mock<ICanchaRepository>();
            _canchaService = new CanchaService(_canchaRepositoryMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllCanchas()
        {
            // Arrange
            var canchas = new List<Cancha>
            {
                new Cancha { CanchaId = 1, Nombre = "Cancha 1", ComplejoId = 1, TipoCanchaId = 1, TipoSuperficieId = 1, Activa = true },
                new Cancha { CanchaId = 2, Nombre = "Cancha 2", ComplejoId = 1, TipoCanchaId = 1, TipoSuperficieId = 1, Activa = false }
            };
            _canchaRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(canchas);

            // Act
            var result = await _canchaService.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
            result.First().Nombre.Should().Be("Cancha 1");
            result.Last().Nombre.Should().Be("Cancha 2");
        }

        [Fact]
        public async Task GetByIdAsync_WhenExists_ShouldReturnCancha()
        {
            // Arrange
            var cancha = new Cancha { CanchaId = 1, Nombre = "Cancha 1" };
            _canchaRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(cancha);

            // Act
            var result = await _canchaService.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Nombre.Should().Be("Cancha 1");
        }

        [Fact]
        public async Task GetByIdAsync_WhenDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            _canchaRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((Cancha)null!);

            // Act
            var act = async () => await _canchaService.GetByIdAsync(1);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Cancha con ID 1 no encontrado.");
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnCreatedCancha()
        {
            // Arrange
            var createDto = new CrearCanchaDTO
            {
                ComplejoId = 1,
                TipoCanchaId = 2,
                TipoSuperficieId = 3,
                Nombre = "Cancha 1"
            };

            _canchaRepositoryMock.Setup(repo => repo.CreateAsync(It.IsAny<Cancha>()))
                .ReturnsAsync((Cancha c) => 
                {
                    c.CanchaId = 1;
                    return c;
                });

            // Act
            var result = await _canchaService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.CanchaId.Should().Be(1);
            result.Nombre.Should().Be("Cancha 1");
            result.ComplejoId.Should().Be(1);
            result.TipoCanchaId.Should().Be(2);
            result.TipoSuperficieId.Should().Be(3);
            result.Activa.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsync_WhenExists_ShouldUpdateCancha()
        {
            // Arrange
            var existingCancha = new Cancha { CanchaId = 1, Nombre = "Vieja", ComplejoId = 1, TipoCanchaId = 1, TipoSuperficieId = 1 };
            var updateDto = new CrearCanchaDTO { ComplejoId = 2, TipoCanchaId = 2, TipoSuperficieId = 2, Nombre = "Nueva" };

            _canchaRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingCancha);
            _canchaRepositoryMock.Setup(repo => repo.UpdateAsync(existingCancha)).ReturnsAsync(true);

            // Act
            await _canchaService.UpdateAsync(1, updateDto);

            // Assert
            _canchaRepositoryMock.Verify(repo => repo.UpdateAsync(It.Is<Cancha>(c => 
                c.Nombre == "Nueva" && 
                c.ComplejoId == 2 && 
                c.TipoCanchaId == 2 && 
                c.TipoSuperficieId == 2
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var updateDto = new CrearCanchaDTO { Nombre = "Nueva" };
            _canchaRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((Cancha)null!);

            // Act
            var act = async () => await _canchaService.UpdateAsync(1, updateDto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Cancha con ID 1 no encontrado.");
        }

        [Fact]
        public async Task DeleteAsync_WhenExists_ShouldDeleteCancha()
        {
            // Arrange
            var existingCancha = new Cancha { CanchaId = 1 };
            _canchaRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingCancha);
            _canchaRepositoryMock.Setup(repo => repo.DeleteAsync(1)).ReturnsAsync(true);

            // Act
            await _canchaService.DeleteAsync(1);

            // Assert
            _canchaRepositoryMock.Verify(repo => repo.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            _canchaRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((Cancha)null!);

            // Act
            var act = async () => await _canchaService.DeleteAsync(1);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Cancha con ID 1 no encontrado.");
        }

        [Fact]
        public async Task ActivarAsync_WhenSuccessful_ShouldNotThrow()
        {
            // Arrange
            _canchaRepositoryMock.Setup(repo => repo.ActivarAsync(1)).ReturnsAsync(true);

            // Act
            var act = async () => await _canchaService.ActivarAsync(1);

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ActivarAsync_WhenFails_ShouldThrowNotFoundException()
        {
            // Arrange
            _canchaRepositoryMock.Setup(repo => repo.ActivarAsync(1)).ReturnsAsync(false);

            // Act
            var act = async () => await _canchaService.ActivarAsync(1);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Cancha con ID 1 no encontrado.");
        }

        [Fact]
        public async Task DesactivarAsync_WhenSuccessful_ShouldNotThrow()
        {
            // Arrange
            _canchaRepositoryMock.Setup(repo => repo.DesactivarAsync(1)).ReturnsAsync(true);

            // Act
            var act = async () => await _canchaService.DesactivarAsync(1);

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task DesactivarAsync_WhenFails_ShouldThrowNotFoundException()
        {
            // Arrange
            _canchaRepositoryMock.Setup(repo => repo.DesactivarAsync(1)).ReturnsAsync(false);

            // Act
            var act = async () => await _canchaService.DesactivarAsync(1);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Cancha con ID 1 no encontrado.");
        }

        [Fact]
        public async Task GetCanchasByComplejoAsync_ShouldReturnCanchasForComplejo()
        {
            // Arrange
            var canchas = new List<Cancha>
            {
                new Cancha { CanchaId = 1, ComplejoId = 1 },
                new Cancha { CanchaId = 2, ComplejoId = 1 }
            };
            _canchaRepositoryMock.Setup(repo => repo.GetByComplejoIdAsync(1)).ReturnsAsync(canchas);

            // Act
            var result = await _canchaService.GetCanchasByComplejoAsync(1);

            // Assert
            result.Should().HaveCount(2);
            result.All(c => c.ComplejoId == 1).Should().BeTrue();
        }
    }
}
