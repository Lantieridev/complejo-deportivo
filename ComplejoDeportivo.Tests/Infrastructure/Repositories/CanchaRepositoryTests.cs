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
    public class CanchaRepositoryTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
    {
        private readonly DatabaseFixture _fixture;
        private ComplejoDeportivoContext _context = null!;
        private CanchaRepository _repository = null!;

        public CanchaRepositoryTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public Task InitializeAsync()
        {
            _context = new ComplejoDeportivoContext(_fixture.Options);
            // Ensure clean state or use transactions per test?
            // Testcontainers will be shared, so let's start a transaction and roll it back after each test
            _context.Database.BeginTransaction();
            _repository = new CanchaRepository(_context);
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

        private async Task<(int complejoId, int tipoCanchaId, int tipoSuperficieId)> CreateDependenciesAsync()
        {
            var direccion = new Direccion { Calle = "Test", Numero = "123", Ciudad = "Ciudad", CodigoPostal = "28001", Provincia = "Provincia" };
            _context.Direccions.Add(direccion);
            await _context.SaveChangesAsync();

            var complejo = new Complejo { Nombre = "Complejo Test", DireccionId = direccion.DireccionId };
            var tipoCancha = new TipoCancha { Nombre = "Fútbol 5" };
            var tipoSuperficie = new TipoSuperficie { Nombre = "Sintético" };

            _context.Complejos.Add(complejo);
            _context.TipoCanchas.Add(tipoCancha);
            _context.TipoSuperficies.Add(tipoSuperficie);
            await _context.SaveChangesAsync();

            return (complejo.ComplejoId, tipoCancha.TipoCanchaId, tipoSuperficie.TipoSuperficieId);
        }

        [Fact]
        public async Task CreateAsync_ShouldAddCancha()
        {
            var deps = await CreateDependenciesAsync();
            var cancha = new Cancha
            {
                Nombre = "Cancha 1",
                ComplejoId = deps.complejoId,
                TipoCanchaId = deps.tipoCanchaId,
                TipoSuperficieId = deps.tipoSuperficieId,
                Activa = true
            };

            var result = await _repository.CreateAsync(cancha);

            result.Should().NotBeNull();
            result.CanchaId.Should().BeGreaterThan(0);
            
            var dbCancha = await _context.Canchas.FindAsync(result.CanchaId);
            dbCancha.Should().NotBeNull();
            dbCancha.Nombre.Should().Be("Cancha 1");
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenExists()
        {
            var deps = await CreateDependenciesAsync();
            var cancha = new Cancha { Nombre = "Cancha 2", ComplejoId = deps.complejoId, TipoCanchaId = deps.tipoCanchaId, TipoSuperficieId = deps.tipoSuperficieId, Activa = true };
            _context.Canchas.Add(cancha);
            await _context.SaveChangesAsync();

            var result = await _repository.DeleteAsync(cancha.CanchaId);

            result.Should().BeTrue();
            var dbCancha = await _context.Canchas.FindAsync(cancha.CanchaId);
            dbCancha.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenNotExists()
        {
            var result = await _repository.DeleteAsync(9999);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllCanchas()
        {
            var deps = await CreateDependenciesAsync();
            _context.Canchas.Add(new Cancha { Nombre = "Cancha A", ComplejoId = deps.complejoId, TipoCanchaId = deps.tipoCanchaId, TipoSuperficieId = deps.tipoSuperficieId, Activa = true });
            _context.Canchas.Add(new Cancha { Nombre = "Cancha B", ComplejoId = deps.complejoId, TipoCanchaId = deps.tipoCanchaId, TipoSuperficieId = deps.tipoSuperficieId, Activa = true });
            await _context.SaveChangesAsync();

            var result = await _repository.GetAllAsync();

            result.Should().NotBeEmpty();
            result.Count().Should().BeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCancha_WhenExists()
        {
            var deps = await CreateDependenciesAsync();
            var cancha = new Cancha { Nombre = "Cancha C", ComplejoId = deps.complejoId, TipoCanchaId = deps.tipoCanchaId, TipoSuperficieId = deps.tipoSuperficieId, Activa = true };
            _context.Canchas.Add(cancha);
            await _context.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(cancha.CanchaId);

            result.Should().NotBeNull();
            result.Nombre.Should().Be("Cancha C");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            var result = await _repository.GetByIdAsync(9999);
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateCancha()
        {
            var deps = await CreateDependenciesAsync();
            var cancha = new Cancha { Nombre = "Cancha D", ComplejoId = deps.complejoId, TipoCanchaId = deps.tipoCanchaId, TipoSuperficieId = deps.tipoSuperficieId, Activa = true };
            _context.Canchas.Add(cancha);
            await _context.SaveChangesAsync();

            cancha.Nombre = "Cancha Modificada";
            var result = await _repository.UpdateAsync(cancha);

            result.Should().BeTrue();
            
            // Clear tracker to verify update
            _context.ChangeTracker.Clear();
            var dbCancha = await _context.Canchas.FindAsync(cancha.CanchaId);
            dbCancha!.Nombre.Should().Be("Cancha Modificada");
        }

        [Fact]
        public async Task ActivarAsync_ShouldReturnTrue_WhenExists()
        {
            var deps = await CreateDependenciesAsync();
            var cancha = new Cancha { Nombre = "Cancha E", ComplejoId = deps.complejoId, TipoCanchaId = deps.tipoCanchaId, TipoSuperficieId = deps.tipoSuperficieId, Activa = false };
            _context.Canchas.Add(cancha);
            await _context.SaveChangesAsync();

            var result = await _repository.ActivarAsync(cancha.CanchaId);

            result.Should().BeTrue();
            var dbCancha = await _context.Canchas.FindAsync(cancha.CanchaId);
            dbCancha!.Activa.Should().BeTrue();
        }

        [Fact]
        public async Task ActivarAsync_ShouldReturnFalse_WhenNotExists()
        {
            var result = await _repository.ActivarAsync(9999);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DesactivarAsync_ShouldReturnTrue_WhenExists()
        {
            var deps = await CreateDependenciesAsync();
            var cancha = new Cancha { Nombre = "Cancha F", ComplejoId = deps.complejoId, TipoCanchaId = deps.tipoCanchaId, TipoSuperficieId = deps.tipoSuperficieId, Activa = true };
            _context.Canchas.Add(cancha);
            await _context.SaveChangesAsync();

            var result = await _repository.DesactivarAsync(cancha.CanchaId);

            result.Should().BeTrue();
            var dbCancha = await _context.Canchas.FindAsync(cancha.CanchaId);
            dbCancha!.Activa.Should().BeFalse();
        }

        [Fact]
        public async Task DesactivarAsync_ShouldReturnFalse_WhenNotExists()
        {
            var result = await _repository.DesactivarAsync(9999);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetByComplejoIdAsync_ShouldReturnCanchasForComplejo()
        {
            var deps1 = await CreateDependenciesAsync();
            var deps2 = await CreateDependenciesAsync(); // another complejo

            _context.Canchas.Add(new Cancha { Nombre = "Cancha G1", ComplejoId = deps1.complejoId, TipoCanchaId = deps1.tipoCanchaId, TipoSuperficieId = deps1.tipoSuperficieId, Activa = true });
            _context.Canchas.Add(new Cancha { Nombre = "Cancha G2", ComplejoId = deps1.complejoId, TipoCanchaId = deps1.tipoCanchaId, TipoSuperficieId = deps1.tipoSuperficieId, Activa = true });
            _context.Canchas.Add(new Cancha { Nombre = "Cancha H1", ComplejoId = deps2.complejoId, TipoCanchaId = deps2.tipoCanchaId, TipoSuperficieId = deps2.tipoSuperficieId, Activa = true });
            await _context.SaveChangesAsync();

            var result = await _repository.GetByComplejoIdAsync(deps1.complejoId);

            result.Should().HaveCount(2);
            result.All(c => c.ComplejoId == deps1.complejoId).Should().BeTrue();
        }
    }
}
