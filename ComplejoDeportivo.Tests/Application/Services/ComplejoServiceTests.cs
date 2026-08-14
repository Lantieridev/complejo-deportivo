using ComplejoDeportivo.Application.DTOs;
using ComplejoDeportivo.Application.Repositories;
using ComplejoDeportivo.Application.Services.Implementations;
using ComplejoDeportivo.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace ComplejoDeportivo.Tests.Application.Services
{
    public class ComplejoServiceTests
    {
        private readonly Mock<IComplejoRepository> _complejoRepoMock;
        private readonly ComplejoService _complejoService;

        public ComplejoServiceTests()
        {
            _complejoRepoMock = new Mock<IComplejoRepository>();
            _complejoService = new ComplejoService(_complejoRepoMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedDTOs()
        {
            var complejos = new List<Complejo>
            {
                new Complejo { ComplejoId = 1, Nombre = "C1", Direccion = new Direccion { DireccionId = 1, Calle = "C1" } }
            };
            _complejoRepoMock.Setup(repo => repo.GetAllAsync(It.IsAny<string>())).ReturnsAsync(complejos);
            var result = await _complejoService.GetAllAsync("test");
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ShouldReturnMappedDTO()
        {
            var complejo = new Complejo { ComplejoId = 1, Nombre = "C1", Direccion = new Direccion { DireccionId = 1, Calle = "C1" } };
            _complejoRepoMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(complejo);
            var result = await _complejoService.GetByIdAsync(1);
            result.ComplejoId.Should().Be(1);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ShouldThrowNotFoundException()
        {
            _complejoRepoMock.Setup(repo => repo.GetByIdAsync(99)).ReturnsAsync((Complejo)null!);
            Func<Task> action = async () => await _complejoService.GetByIdAsync(99);
            await action.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateAndReturnDTO()
        {
            var createDto = new CrearComplejoDTO { Nombre = "C1", Calle = "C1", Numero = "1", Ciudad = "C", Provincia = "P", CodigoPostal = "0" };
            _complejoRepoMock.Setup(repo => repo.CreateAsync(It.IsAny<Complejo>(), It.IsAny<Direccion>()))
                .ReturnsAsync((Complejo c, Direccion d) => { c.ComplejoId = 5; return c; });
            var result = await _complejoService.CreateAsync(createDto);
            result.ComplejoId.Should().Be(5);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingId_ShouldThrowNotFoundException()
        {
            var updateDto = new ActualizarComplejoDTO { Nombre = "T", Calle = "T", Numero = "1", Ciudad = "C", Provincia = "P", CodigoPostal = "0" };
            _complejoRepoMock.Setup(repo => repo.GetByIdAsync(99)).ReturnsAsync((Complejo)null!);
            Func<Task> action = async () => await _complejoService.UpdateAsync(99, updateDto);
            await action.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task UpdateAsync_ValidData_ShouldUpdate()
        {
            var complejo = new Complejo { ComplejoId = 1, Nombre = "Old", Direccion = new Direccion { Calle = "Old" } };
            var updateDto = new ActualizarComplejoDTO { Nombre = "N", Calle = "N", Numero = "1", Ciudad = "C", Provincia = "P", CodigoPostal = "0" };
            _complejoRepoMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(complejo);
            _complejoRepoMock.Setup(repo => repo.UpdateAsync(It.IsAny<Complejo>(), It.IsAny<Direccion>())).ReturnsAsync(true);
            await _complejoService.UpdateAsync(1, updateDto);
            _complejoRepoMock.Verify(repo => repo.UpdateAsync(complejo, complejo.Direccion), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingId_ShouldThrowNotFoundException()
        {
            _complejoRepoMock.Setup(repo => repo.GetByIdAsync(99)).ReturnsAsync((Complejo)null!);
            Func<Task> action = async () => await _complejoService.DeleteAsync(99);
            await action.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DeleteAsync_Succeeds_ShouldCallDelete()
        {
            var complejo = new Complejo { ComplejoId = 1 };
            _complejoRepoMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(complejo);
            _complejoRepoMock.Setup(repo => repo.DeleteAsync(1)).ReturnsAsync(true);
            await _complejoService.DeleteAsync(1);
            _complejoRepoMock.Verify(repo => repo.DeleteAsync(1), Times.Once);
        }
    }
}
