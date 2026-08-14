using ComplejoDeportivo.Domain;
using ComplejoDeportivo.Infrastructure.Persistence;
using ComplejoDeportivo.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Xunit;

namespace ComplejoDeportivo.Tests.Infrastructure.Repositories
{
    [Collection("Database collection")]
    public class EmpleadoRepositoryTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
    {
        private readonly DatabaseFixture _fixture;
        private ComplejoDeportivoContext _context = null!;
        private EmpleadoRepository _repository = null!;

        public EmpleadoRepositoryTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            _context = new ComplejoDeportivoContext(_fixture.Options);
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM Empleado");
            _repository = new EmpleadoRepository(_context);
        }

        public async Task DisposeAsync()
        {
            if (_context != null)
            {
                await _context.DisposeAsync();
            }
        }

        [Fact]
        public async Task CreateAsync_ShouldAddEmpleado()
        {
            // Arrange
            var empleado = new Empleado { Nombre = "Juan", Apellido = "Perez", Email = "juan@test.com", Cargo = "Gerente" };

            // Act
            var result = await _repository.CreateAsync(empleado);

            // Assert
            result.EmpleadoId.Should().BeGreaterThan(0);
            var inDb = await _context.Empleados.FindAsync(result.EmpleadoId);
            inDb.Should().NotBeNull();
            inDb!.Nombre.Should().Be("Juan");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnEmpleado_WhenExists()
        {
            // Arrange
            var empleado = new Empleado { Nombre = "Ana", Apellido = "Gomez", Email = "ana@test.com", Cargo = "Vendedora" };
            await _context.Empleados.AddAsync(empleado);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(empleado.EmpleadoId);

            // Assert
            result.Should().NotBeNull();
            result!.Nombre.Should().Be("Ana");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            // Act
            var result = await _repository.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByEmailAsync_ShouldReturnEmpleado_WhenExists()
        {
            // Arrange
            var empleado = new Empleado { Nombre = "Luis", Apellido = "Lopez", Email = "luis@test.com", Cargo = "Cajero" };
            await _context.Empleados.AddAsync(empleado);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByEmailAsync("LUIS@TEST.com"); // check case insensitivity

            // Assert
            result.Should().NotBeNull();
            result!.Nombre.Should().Be("Luis");
        }

        [Fact]
        public async Task GetByEmailAsync_ShouldReturnNull_WhenEmailIsEmpty()
        {
            // Act
            var result = await _repository.GetByEmailAsync("");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAll_WhenNoSearchTerm()
        {
            // Arrange
            await _context.Empleados.AddRangeAsync(
                new Empleado { Nombre = "E1", Apellido = "A1", Email = "1@t.com", Cargo = "C1" },
                new Empleado { Nombre = "E2", Apellido = "A2", Email = "2@t.com", Cargo = "C2" }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            result.Should().HaveCountGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task GetAllAsync_ShouldFilterBySearchTerm()
        {
            // Arrange
            await _context.Empleados.AddRangeAsync(
                new Empleado { Nombre = "Carlos", Apellido = "Santana", Email = "c@t.com", Cargo = "Mantenimiento" },
                new Empleado { Nombre = "Maria", Apellido = "Becerra", Email = "m@t.com", Cargo = "Gerente" }
            );
            await _context.SaveChangesAsync();

            // Act
            var result1 = await _repository.GetAllAsync("Carlos");
            var result2 = await _repository.GetAllAsync("Gerente");
            var result3 = await _repository.GetAllAsync("Carlos Santana");

            // Assert
            result1.Should().Contain(e => e.Nombre == "Carlos");
            result2.Should().Contain(e => e.Nombre == "Maria");
            result3.Should().Contain(e => e.Nombre == "Carlos");
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyEmpleado()
        {
            // Arrange
            var empleado = new Empleado { Nombre = "Pedro", Apellido = "Diaz", Email = "p@t.com", Cargo = "Limpieza" };
            await _context.Empleados.AddAsync(empleado);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            // Act
            empleado.Nombre = "Pedro Editado";
            var result = await _repository.UpdateAsync(empleado);

            // Assert
            result.Should().BeTrue();
            var inDb = await _context.Empleados.FindAsync(empleado.EmpleadoId);
            inDb!.Nombre.Should().Be("Pedro Editado");
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveEmpleado_WhenExists()
        {
            // Arrange
            var empleado = new Empleado { Nombre = "Jose", Apellido = "Garcia", Email = "j@t.com", Cargo = "Seguridad" };
            await _context.Empleados.AddAsync(empleado);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.DeleteAsync(empleado.EmpleadoId);

            // Assert
            result.Should().BeTrue();
            var inDb = await _context.Empleados.FindAsync(empleado.EmpleadoId);
            inDb.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenNotExists()
        {
            // Act
            var result = await _repository.DeleteAsync(999);

            // Assert
            result.Should().BeFalse();
        }
    }
}
