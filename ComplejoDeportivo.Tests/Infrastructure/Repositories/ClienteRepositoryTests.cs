using System;
using System.Linq;
using System.Threading.Tasks;
using ComplejoDeportivo.Domain;
using ComplejoDeportivo.Infrastructure.Persistence;
using ComplejoDeportivo.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ComplejoDeportivo.Tests.Infrastructure.Repositories
{
    public class ClienteRepositoryTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
    {
        private readonly DatabaseFixture _fixture;
        private ComplejoDeportivoContext _context = null!;
        private ClienteRepository _repository = null!;

        public ClienteRepositoryTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public Task InitializeAsync()
        {
            _context = new ComplejoDeportivoContext(_fixture.Options);
            _context.Database.BeginTransaction();
            _repository = new ClienteRepository(_context);
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            if (_context.Database.CurrentTransaction != null)
            {
                await _context.Database.CurrentTransaction.RollbackAsync();
            }
            await _context.DisposeAsync();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllClientes()
        {
            _context.Clientes.Add(new Cliente { Nombre = "Juan", Apellido = "Perez", Email = "juan@test.com", FechaRegistro = DateTime.Now });
            _context.Clientes.Add(new Cliente { Nombre = "Maria", Apellido = "Gomez", Email = "maria@test.com", FechaRegistro = DateTime.Now });
            await _context.SaveChangesAsync();

            var result = await _repository.GetAllAsync();

            result.Should().NotBeEmpty();
            result.Count().Should().BeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCliente_WhenExists()
        {
            var cliente = new Cliente { Nombre = "Carlos", Apellido = "Lopez", Email = "carlos@test.com", FechaRegistro = DateTime.Now };
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(cliente.ClienteId);

            result.Should().NotBeNull();
            result.Nombre.Should().Be("Carlos");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            var result = await _repository.GetByIdAsync(9999);
            result.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task GetByEmailAsync_ShouldReturnNull_WhenEmailIsNullOrEmpty(string? email)
        {
            var result = await _repository.GetByEmailAsync(email);
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByEmailAsync_ShouldReturnCliente_WhenExistsAndCaseInsensitive()
        {
            var cliente = new Cliente { Nombre = "Ana", Apellido = "Martinez", Email = "ANA@TEST.COM", FechaRegistro = DateTime.Now };
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            var result = await _repository.GetByEmailAsync("ana@test.com");

            result.Should().NotBeNull();
            result.Nombre.Should().Be("Ana");
        }

        [Fact]
        public async Task GetByEmailAsync_ShouldReturnNull_WhenNotExists()
        {
            var result = await _repository.GetByEmailAsync("notfound@test.com");
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_ShouldAddCliente()
        {
            var cliente = new Cliente { Nombre = "Luis", Apellido = "Gcia", Email = "luis@test.com", FechaRegistro = DateTime.Now };
            
            var result = await _repository.CreateAsync(cliente);

            result.Should().NotBeNull();
            result.ClienteId.Should().BeGreaterThan(0);
            
            var dbCliente = await _context.Clientes.FindAsync(result.ClienteId);
            dbCliente.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateClienteButNotFechaRegistro()
        {
            var originalDate = new DateTime(2020, 1, 1);
            var cliente = new Cliente { Nombre = "Pedro", Apellido = "Diaz", Email = "pedro@test.com", FechaRegistro = originalDate };
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            var toUpdate = await _context.Clientes.FindAsync(cliente.ClienteId);
            toUpdate!.Nombre = "Pedro Modificado";
            toUpdate.FechaRegistro = new DateTime(2025, 1, 1); // Attempt to change date

            var result = await _repository.UpdateAsync(toUpdate);

            result.Should().BeTrue();
            
            _context.ChangeTracker.Clear();
            var dbCliente = await _context.Clientes.FindAsync(cliente.ClienteId);
            dbCliente!.Nombre.Should().Be("Pedro Modificado");
            dbCliente.FechaRegistro.Should().Be(originalDate);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenExists()
        {
            var cliente = new Cliente { Nombre = "Laura", Apellido = "Ruiz", Email = "laura@test.com", FechaRegistro = DateTime.Now };
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            var result = await _repository.DeleteAsync(cliente.ClienteId);

            result.Should().BeTrue();
            var dbCliente = await _context.Clientes.FindAsync(cliente.ClienteId);
            dbCliente.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenNotExists()
        {
            var result = await _repository.DeleteAsync(9999);
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task DoesDocumentoExistAsync_ShouldReturnFalse_WhenNullOrEmpty(string? doc)
        {
            var result = await _repository.DoesDocumentoExistAsync(doc);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DoesDocumentoExistAsync_ShouldReturnTrue_WhenExists()
        {
            _context.Clientes.Add(new Cliente { Nombre = "Doc", Apellido = "Test", Documento = "12345678", FechaRegistro = DateTime.Now });
            await _context.SaveChangesAsync();

            var result = await _repository.DoesDocumentoExistAsync("12345678");
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DoesDocumentoExistAsync_ShouldReturnFalse_WhenNotExists()
        {
            var result = await _repository.DoesDocumentoExistAsync("99999999");
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task DoesTelefonoExistAsync_ShouldReturnFalse_WhenNullOrEmpty(string? tel)
        {
            var result = await _repository.DoesTelefonoExistAsync(tel);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DoesTelefonoExistAsync_ShouldReturnTrue_WhenExists()
        {
            _context.Clientes.Add(new Cliente { Nombre = "Tel", Apellido = "Test", Telefono = "555-1234", FechaRegistro = DateTime.Now });
            await _context.SaveChangesAsync();

            var result = await _repository.DoesTelefonoExistAsync("555-1234");
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DoesTelefonoExistAsync_ShouldReturnFalse_WhenNotExists()
        {
            var result = await _repository.DoesTelefonoExistAsync("555-9999");
            result.Should().BeFalse();
        }
    }
}
