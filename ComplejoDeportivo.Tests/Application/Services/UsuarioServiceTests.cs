using System;
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
    public class UsuarioServiceTests
    {
        private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
        private readonly Mock<IClienteRepository> _clienteRepoMock;
        private readonly Mock<IEmpleadoRepository> _empleadoRepoMock;
        private readonly UsuarioService _usuarioService;

        public UsuarioServiceTests()
        {
            _usuarioRepoMock = new Mock<IUsuarioRepository>();
            _clienteRepoMock = new Mock<IClienteRepository>();
            _empleadoRepoMock = new Mock<IEmpleadoRepository>();
            
            _usuarioService = new UsuarioService(
                _usuarioRepoMock.Object, 
                _clienteRepoMock.Object, 
                _empleadoRepoMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedDtos()
        {
            var list = new List<Usuario> { new Usuario { UsuarioId = 1, Email = "test@test.com", TipoUsuario = "Cliente", ClienteId = 1, EmpleadoId = null } };
            _usuarioRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(list);

            var result = await _usuarioService.GetAllAsync();

            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetByIdAsync_WhenFound_ShouldReturnDto()
        {
            var u = new Usuario { UsuarioId = 1, Email = "test@test.com", TipoUsuario = "Cliente", ClienteId = 1, EmpleadoId = null };
            _usuarioRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(u);

            var result = await _usuarioService.GetByIdAsync(1);

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WhenNotFound_ShouldThrowNotFoundException()
        {
            _usuarioRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Usuario?)null);

            Func<Task> act = async () => await _usuarioService.GetByIdAsync(1);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CreateAsync_ShouldHashPasswordAndReturnDto()
        {
            var dto = new CreateUsuarioDTO { Email = "test@test.com", Password = "password123", TipoUsuario = "Admin", EmpleadoId = 0, ClienteId = 0 };
            _usuarioRepoMock.Setup(r => r.CreateAsync(It.IsAny<Usuario>())).ReturnsAsync((Usuario u) => { u.UsuarioId = 1; return u; });

            var result = await _usuarioService.CreateAsync(dto);

            result.Should().NotBeNull();
            _usuarioRepoMock.Verify(r => r.CreateAsync(It.IsAny<Usuario>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenNotFound_ShouldThrowNotFoundException()
        {
            _usuarioRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Usuario?)null);

            Func<Task> act = async () => await _usuarioService.UpdateAsync(1, new UsuarioDTO { Email = "test@test.com", TipoUsuario = "Admin" });

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task UpdateAsync_WhenFound_ShouldUpdate()
        {
            var u = new Usuario { UsuarioId = 1, Email = "old@test.com" };
            _usuarioRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(u);

            var dto = new UsuarioDTO { Email = "new@test.com", TipoUsuario = "Admin", ClienteId = 2, EmpleadoId = 3 };

            await _usuarioService.UpdateAsync(1, dto);

            _usuarioRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Usuario>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenNotFound_ShouldThrowNotFoundException()
        {
            _usuarioRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Usuario?)null);

            Func<Task> act = async () => await _usuarioService.DeleteAsync(1);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DeleteAsync_WhenFound_ShouldDelete()
        {
            var u = new Usuario { UsuarioId = 1 };
            _usuarioRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(u);

            await _usuarioService.DeleteAsync(1);

            _usuarioRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task RegisterClienteAsync_WhenEmailExists_ShouldThrowException()
        {
            var dto = new RegisterClienteDTO { Nombre = "Juan", Apellido = "Perez", Email = "test@test.com", Documento = "123", Telefono = "456", Password = "password123" };
            _usuarioRepoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(new Usuario());

            Func<Task> act = async () => await _usuarioService.RegisterClienteAsync(dto);

            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task RegisterClienteAsync_WhenDocumentoExists_ShouldThrowException()
        {
            var dto = new RegisterClienteDTO { Nombre = "Juan", Apellido = "Perez", Email = "test@test.com", Documento = "123", Telefono = "456", Password = "password123" };
            _usuarioRepoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Usuario?)null);
            _clienteRepoMock.Setup(r => r.DoesDocumentoExistAsync(dto.Documento)).ReturnsAsync(true);

            Func<Task> act = async () => await _usuarioService.RegisterClienteAsync(dto);

            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task RegisterClienteAsync_WhenTelefonoExists_ShouldThrowException()
        {
            var dto = new RegisterClienteDTO { Nombre = "Juan", Apellido = "Perez", Email = "test@test.com", Documento = "123", Telefono = "456", Password = "password123" };
            _usuarioRepoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Usuario?)null);
            _clienteRepoMock.Setup(r => r.DoesDocumentoExistAsync(dto.Documento)).ReturnsAsync(false);
            _clienteRepoMock.Setup(r => r.DoesTelefonoExistAsync(dto.Telefono)).ReturnsAsync(true);

            Func<Task> act = async () => await _usuarioService.RegisterClienteAsync(dto);

            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task RegisterClienteAsync_WhenValid_ShouldCreateClienteAndUsuario()
        {
            var dto = new RegisterClienteDTO { Nombre = "Juan", Apellido = "Perez", Email = "test@test.com", Documento = "123", Telefono = "456", Password = "password123" };
            
            _usuarioRepoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Usuario?)null);
            _clienteRepoMock.Setup(r => r.DoesDocumentoExistAsync(dto.Documento)).ReturnsAsync(false);
            _clienteRepoMock.Setup(r => r.DoesTelefonoExistAsync(dto.Telefono)).ReturnsAsync(false);
            
            _clienteRepoMock.Setup(r => r.CreateAsync(It.IsAny<Cliente>())).ReturnsAsync((Cliente c) => { c.ClienteId = 1; return c; });
            _usuarioRepoMock.Setup(r => r.CreateAsync(It.IsAny<Usuario>())).ReturnsAsync((Usuario u) => { u.UsuarioId = 2; return u; });

            var result = await _usuarioService.RegisterClienteAsync(dto);

            result.Should().NotBeNull();
            result.UsuarioId.Should().Be(2);
        }

        [Fact]
        public async Task RegisterEmpleadoAsync_WhenEmailExists_ShouldThrowException()
        {
            var dto = new RegisterClienteDTO { Nombre = "Juan", Apellido = "Perez", Email = "test@test.com", Documento = "123", Telefono = "456", Password = "password123" };
            _usuarioRepoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(new Usuario());

            Func<Task> act = async () => await _usuarioService.RegisterEmpleadoAsync(dto);

            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task RegisterEmpleadoAsync_WhenValid_ShouldCreateEmpleadoAndUsuario()
        {
            var dto = new RegisterClienteDTO { Nombre = "Admin", Apellido = "Test", Email = "admin@test.com", Password = "pass", Documento = "123", Telefono = "456" };
            
            _usuarioRepoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Usuario?)null);
            _empleadoRepoMock.Setup(r => r.CreateAsync(It.IsAny<Empleado>())).ReturnsAsync((Empleado e) => { e.EmpleadoId = 1; return e; });
            _usuarioRepoMock.Setup(r => r.CreateAsync(It.IsAny<Usuario>())).ReturnsAsync((Usuario u) => { u.UsuarioId = 2; return u; });

            var result = await _usuarioService.RegisterEmpleadoAsync(dto);

            result.Should().NotBeNull();
            result.UsuarioId.Should().Be(2);
        }
    }
}
