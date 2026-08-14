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
    public class EmpleadoServiceTests
    {
        private readonly Mock<IEmpleadoRepository> _empleadoRepoMock;
        private readonly EmpleadoService _empleadoService;

        public EmpleadoServiceTests()
        {
            _empleadoRepoMock = new Mock<IEmpleadoRepository>();
            _empleadoService = new EmpleadoService(_empleadoRepoMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedDtos()
        {
            var searchTerm = "test";
            var empleados = new List<Empleado>
            {
                new Empleado { EmpleadoId = 1, Nombre = "Juan", Apellido = "Perez", Email = "juan@test.com", Telefono = "123", Cargo = "Admin", FechaIngreso = DateOnly.FromDateTime(DateTime.Now) }
            };
            _empleadoRepoMock.Setup(r => r.GetAllAsync(searchTerm)).ReturnsAsync(empleados);

            var result = await _empleadoService.GetAllAsync(searchTerm);

            result.Should().HaveCount(1);
            var first = result.First();
            first.EmpleadoId.Should().Be(1);
        }

        [Fact]
        public async Task GetByIdAsync_WhenFound_ShouldReturnDto()
        {
            var empleado = new Empleado { EmpleadoId = 1, Nombre = "Juan", Apellido = "Perez", Email = "juan@test.com", Telefono = "123", Cargo = "Admin" };
            _empleadoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(empleado);

            var result = await _empleadoService.GetByIdAsync(1);

            result.Should().NotBeNull();
            result.EmpleadoId.Should().Be(1);
        }

        [Fact]
        public async Task GetByIdAsync_WhenNotFound_ShouldThrowNotFoundException()
        {
            _empleadoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Empleado?)null);

            Func<Task> act = async () => await _empleadoService.GetByIdAsync(1);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CreateAsync_WhenEmailAlreadyRegistered_ShouldThrowException()
        {
            var dto = new CrearEmpleadoDTO { Nombre = "Juan", Apellido = "Perez", Email = "test@test.com", Telefono = "123", Cargo = "Admin" };
            _empleadoRepoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(new Empleado());

            Func<Task> act = async () => await _empleadoService.CreateAsync(dto);

            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task CreateAsync_WhenValid_ShouldCreateAndReturnDto()
        {
            var dto = new CrearEmpleadoDTO { Nombre = "Juan", Apellido = "Perez", Email = "juan@test.com", Telefono = "123", Cargo = "Admin" };
            _empleadoRepoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Empleado?)null);
            _empleadoRepoMock.Setup(r => r.CreateAsync(It.IsAny<Empleado>())).ReturnsAsync((Empleado e) => { e.EmpleadoId = 1; return e; });

            var result = await _empleadoService.CreateAsync(dto);

            result.Should().NotBeNull();
            result.EmpleadoId.Should().Be(1);
        }

        [Fact]
        public async Task UpdateAsync_WhenNotFound_ShouldThrowNotFoundException()
        {
            _empleadoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Empleado?)null);
            
            Func<Task> act = async () => await _empleadoService.UpdateAsync(1, new ActualizarEmpleadoDTO { Nombre = "Juan", Apellido = "Perez", Email = "juan@test.com", Telefono = "123", Cargo = "Admin" });

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task UpdateAsync_WhenEmailAlreadyRegisteredByAnotherEmployee_ShouldThrowException()
        {
            var empleado = new Empleado { EmpleadoId = 1 };
            _empleadoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(empleado);
            _empleadoRepoMock.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(new Empleado { EmpleadoId = 2 });

            var dto = new ActualizarEmpleadoDTO { Nombre = "Juan", Apellido = "Perez", Email = "test@test.com", Telefono = "123", Cargo = "Admin" };

            Func<Task> act = async () => await _empleadoService.UpdateAsync(1, dto);

            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task UpdateAsync_WhenEmailIsSameEmployee_ShouldUpdate()
        {
            var empleado = new Empleado { EmpleadoId = 1, Nombre = "Old", Apellido = "Old", Email = "old@test.com", Telefono = "old", Cargo = "old" };
            _empleadoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(empleado);
            _empleadoRepoMock.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(new Empleado { EmpleadoId = 1 });

            var dto = new ActualizarEmpleadoDTO { Nombre = "New", Apellido = "New", Email = "test@test.com", Telefono = "new", Cargo = "new" };

            await _empleadoService.UpdateAsync(1, dto);

            _empleadoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Empleado>()), Times.Once);
        }
        
        [Fact]
        public async Task UpdateAsync_WhenEmailNotUsedByAnyone_ShouldUpdate()
        {
            // Email provided but GetByEmailAsync finds no owner at all (empleadoEmail == null),
            // exercising the other side of the `empleadoEmail != null && ...` branch.
            var empleado = new Empleado { EmpleadoId = 1, Nombre = "Old" };
            _empleadoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(empleado);
            _empleadoRepoMock.Setup(r => r.GetByEmailAsync("libre@test.com")).ReturnsAsync((Empleado?)null);

            var dto = new ActualizarEmpleadoDTO { Nombre = "New", Apellido = "New", Email = "libre@test.com", Telefono = "new", Cargo = "new" };

            await _empleadoService.UpdateAsync(1, dto);

            _empleadoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Empleado>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenEmailIsEmpty_ShouldUpdate()
        {
            var empleado = new Empleado { EmpleadoId = 1, Nombre = "Old" };
            _empleadoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(empleado);

            var dto = new ActualizarEmpleadoDTO { Nombre = "New", Apellido = "New", Email = "", Telefono = "new", Cargo = "new" };

            await _empleadoService.UpdateAsync(1, dto);

            _empleadoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Empleado>()), Times.Once);
            _empleadoRepoMock.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenNotFound_ShouldThrowNotFoundException()
        {
            _empleadoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Empleado?)null);

            Func<Task> act = async () => await _empleadoService.DeleteAsync(1);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DeleteAsync_WhenFound_ShouldDelete()
        {
            var empleado = new Empleado { EmpleadoId = 1 };
            _empleadoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(empleado);

            await _empleadoService.DeleteAsync(1);

            _empleadoRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
        }
    }
}
