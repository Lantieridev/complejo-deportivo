using ComplejoDeportivo.Application.DTOs;
using ComplejoDeportivo.Application.Repositories;
using ComplejoDeportivo.Application.Services.Implementations;
using ComplejoDeportivo.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace ComplejoDeportivo.Tests.Application.Services
{
    public class ClienteServiceTests
    {
        private readonly Mock<IClienteRepository> _clienteRepoMock;
        private readonly ClienteService _clienteService;

        public ClienteServiceTests()
        {
            _clienteRepoMock = new Mock<IClienteRepository>();
            _clienteService = new ClienteService(_clienteRepoMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedDTOs()
        {
            // Arrange
            var clientes = new List<Cliente>
            {
                new Cliente { ClienteId = 1, Nombre = "Juan", Apellido = "Perez", Email = "juan@test.com", Telefono = "123", Documento = "111", FechaRegistro = DateTime.UtcNow }
            };

            _clienteRepoMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(clientes);

            // Act
            var result = await _clienteService.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Nombre.Should().Be("Juan");
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ShouldReturnMappedDTO()
        {
            // Arrange
            var cliente = new Cliente { ClienteId = 1, Nombre = "Juan", Apellido = "Perez" };
            _clienteRepoMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(cliente);

            // Act
            var result = await _clienteService.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Nombre.Should().Be("Juan");
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ShouldThrowNotFoundException()
        {
            // Arrange
            _clienteRepoMock.Setup(repo => repo.GetByIdAsync(99)).ReturnsAsync((Cliente)null!);

            // Act
            Func<Task> action = async () => await _clienteService.GetByIdAsync(99);

            // Assert
            await action.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Cliente con ID 99 no encontrado.");
        }

        [Fact]
        public async Task CreateAsync_DuplicateEmail_ShouldThrowException()
        {
            // Arrange
            var createDto = new CrearClienteDTO { Email = "test@test.com", Nombre = "Test", Apellido = "Test" };
            _clienteRepoMock.Setup(repo => repo.GetByEmailAsync("test@test.com")).ReturnsAsync(new Cliente());

            // Act
            Func<Task> action = async () => await _clienteService.CreateAsync(createDto);

            // Assert
            await action.Should().ThrowAsync<Exception>().WithMessage("El email ya está registrado.");
        }

        [Fact]
        public async Task CreateAsync_DuplicateDocumento_ShouldThrowException()
        {
            // Arrange
            var createDto = new CrearClienteDTO { Documento = "12345", Nombre = "Test", Apellido = "Test" };
            _clienteRepoMock.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Cliente)null!);
            _clienteRepoMock.Setup(repo => repo.DoesDocumentoExistAsync("12345")).ReturnsAsync(true);

            // Act
            Func<Task> action = async () => await _clienteService.CreateAsync(createDto);

            // Assert
            await action.Should().ThrowAsync<Exception>().WithMessage("El documento ya se encuentra registrado.");
        }

        [Fact]
        public async Task CreateAsync_DuplicateTelefono_ShouldThrowException()
        {
            // Arrange
            var createDto = new CrearClienteDTO { Telefono = "555-5555", Nombre = "Test", Apellido = "Test" };
            _clienteRepoMock.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Cliente)null!);
            _clienteRepoMock.Setup(repo => repo.DoesDocumentoExistAsync(It.IsAny<string>())).ReturnsAsync(false);
            _clienteRepoMock.Setup(repo => repo.DoesTelefonoExistAsync("555-5555")).ReturnsAsync(true);

            // Act
            Func<Task> action = async () => await _clienteService.CreateAsync(createDto);

            // Assert
            await action.Should().ThrowAsync<Exception>().WithMessage("El número de teléfono ya se encuentra registrado.");
        }

        [Fact]
        public async Task CreateAsync_ValidData_ShouldCreateAndReturnDTO()
        {
            // Arrange
            var createDto = new CrearClienteDTO
            {
                Nombre = "Maria",
                Apellido = "Gomez",
                Email = "maria@test.com",
                Telefono = "123",
                Documento = "456"
            };

            _clienteRepoMock.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Cliente)null!);
            _clienteRepoMock.Setup(repo => repo.DoesDocumentoExistAsync(It.IsAny<string>())).ReturnsAsync(false);
            _clienteRepoMock.Setup(repo => repo.DoesTelefonoExistAsync(It.IsAny<string>())).ReturnsAsync(false);

            _clienteRepoMock.Setup(repo => repo.CreateAsync(It.IsAny<Cliente>()))
                .ReturnsAsync((Cliente c) => { c.ClienteId = 10; return c; });

            // Act
            var result = await _clienteService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.ClienteId.Should().Be(10);
            result.Nombre.Should().Be("Maria");
            _clienteRepoMock.Verify(repo => repo.CreateAsync(It.Is<Cliente>(c => c.Nombre == "Maria")), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingId_ShouldThrowNotFoundException()
        {
            // Arrange
            var updateDto = new ActualizarClienteDTO { Nombre = "Test", Apellido = "Test" };
            _clienteRepoMock.Setup(repo => repo.GetByIdAsync(99)).ReturnsAsync((Cliente)null!);

            // Act
            Func<Task> action = async () => await _clienteService.UpdateAsync(99, updateDto);

            // Assert
            await action.Should().ThrowAsync<NotFoundException>().WithMessage("Cliente con ID 99 no encontrado.");
        }

        [Fact]
        public async Task UpdateAsync_DuplicateEmailByOtherClient_ShouldThrowException()
        {
            // Arrange
            var existingCliente = new Cliente { ClienteId = 1 };
            var updateDto = new ActualizarClienteDTO { Nombre = "Test", Apellido = "Test", Email = "other@test.com" };
            
            _clienteRepoMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingCliente);
            _clienteRepoMock.Setup(repo => repo.GetByEmailAsync("other@test.com"))
                .ReturnsAsync(new Cliente { ClienteId = 2 }); // Otro cliente tiene el email

            // Act
            Func<Task> action = async () => await _clienteService.UpdateAsync(1, updateDto);

            // Assert
            await action.Should().ThrowAsync<Exception>().WithMessage("El email ya está registrado por otro cliente.");
        }
        
        [Fact]
        public async Task UpdateAsync_DuplicateEmailBySameClient_ShouldSucceed()
        {
            // Arrange
            var existingCliente = new Cliente { ClienteId = 1, Email = "same@test.com" };
            var updateDto = new ActualizarClienteDTO { Nombre = "Nuevo", Apellido = "Test", Email = "same@test.com" };
            
            _clienteRepoMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingCliente);
            _clienteRepoMock.Setup(repo => repo.GetByEmailAsync("same@test.com"))
                .ReturnsAsync(new Cliente { ClienteId = 1 }); // El mismo cliente tiene el email

            _clienteRepoMock.Setup(repo => repo.UpdateAsync(It.IsAny<Cliente>())).ReturnsAsync(true);

            // Act
            await _clienteService.UpdateAsync(1, updateDto);

            // Assert
            _clienteRepoMock.Verify(repo => repo.UpdateAsync(It.Is<Cliente>(c => c.Nombre == "Nuevo")), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ValidData_ShouldUpdate()
        {
            // Arrange
            var existingCliente = new Cliente { ClienteId = 1 };
            var updateDto = new ActualizarClienteDTO { Nombre = "Actualizado", Apellido = "Test" };
            
            _clienteRepoMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingCliente);
            _clienteRepoMock.Setup(repo => repo.UpdateAsync(It.IsAny<Cliente>())).ReturnsAsync(true);

            // Act
            await _clienteService.UpdateAsync(1, updateDto);

            // Assert
            _clienteRepoMock.Verify(repo => repo.UpdateAsync(It.Is<Cliente>(c => c.Nombre == "Actualizado")), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_EmailNotUsedByAnyone_ShouldSucceed()
        {
            // Arrange: Email provided but GetByEmailAsync finds no owner at all (clienteEmail == null),
            // exercising the other side of the `clienteEmail != null && ...` branch.
            var existingCliente = new Cliente { ClienteId = 1 };
            var updateDto = new ActualizarClienteDTO { Nombre = "Nuevo", Apellido = "Test", Email = "libre@test.com" };

            _clienteRepoMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingCliente);
            _clienteRepoMock.Setup(repo => repo.GetByEmailAsync("libre@test.com")).ReturnsAsync((Cliente)null!);
            _clienteRepoMock.Setup(repo => repo.UpdateAsync(It.IsAny<Cliente>())).ReturnsAsync(true);

            // Act
            await _clienteService.UpdateAsync(1, updateDto);

            // Assert
            _clienteRepoMock.Verify(repo => repo.UpdateAsync(It.Is<Cliente>(c => c.Email == "libre@test.com")), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingId_ShouldThrowNotFoundException()
        {
            // Arrange
            _clienteRepoMock.Setup(repo => repo.GetByIdAsync(99)).ReturnsAsync((Cliente)null!);

            // Act
            Func<Task> action = async () => await _clienteService.DeleteAsync(99);

            // Assert
            await action.Should().ThrowAsync<NotFoundException>().WithMessage("Cliente con ID 99 no encontrado.");
        }

        [Fact]
        public async Task DeleteAsync_Fails_ShouldThrowException()
        {
            // Arrange
            var cliente = new Cliente { ClienteId = 1 };
            _clienteRepoMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(cliente);
            _clienteRepoMock.Setup(repo => repo.DeleteAsync(1)).ReturnsAsync(false);

            // Act
            Func<Task> action = async () => await _clienteService.DeleteAsync(1);

            // Assert
            await action.Should().ThrowAsync<Exception>().WithMessage("No se pudo eliminar el cliente. Verifique que no tenga reservas asociadas.");
        }

        [Fact]
        public async Task DeleteAsync_Succeeds_ShouldNotThrow()
        {
            // Arrange
            var cliente = new Cliente { ClienteId = 1 };
            _clienteRepoMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(cliente);
            _clienteRepoMock.Setup(repo => repo.DeleteAsync(1)).ReturnsAsync(true);

            // Act
            await _clienteService.DeleteAsync(1);

            // Assert
            _clienteRepoMock.Verify(repo => repo.DeleteAsync(1), Times.Once);
        }
    }
}
