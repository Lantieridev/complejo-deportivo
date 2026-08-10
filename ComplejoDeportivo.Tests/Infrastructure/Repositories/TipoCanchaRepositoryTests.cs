using ComplejoDeportivo.Domain;
using ComplejoDeportivo.Infrastructure.Persistence;
using ComplejoDeportivo.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ComplejoDeportivo.Tests.Infrastructure.Repositories
{
    [Collection("Database collection")]
    public class TipoCanchaRepositoryTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
    {
        private readonly DatabaseFixture _fixture;
        private ComplejoDeportivoContext _context = null!;
        private TipoCanchaRepository _repository = null!;

        public TipoCanchaRepositoryTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            _context = new ComplejoDeportivoContext(_fixture.Options);
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM TipoCancha");
            _repository = new TipoCanchaRepository(_context);
        }

        public async Task DisposeAsync()
        {
            if (_context != null)
            {
                await _context.DisposeAsync();
            }
        }

        [Fact]
        public async Task CreateAsync_ShouldAddTipoCancha()
        {
            var tipo = new TipoCancha { Nombre = "Futbol 5" };
            
            var result = await _repository.CreateAsync(tipo);
            
            result.TipoCanchaId.Should().BeGreaterThan(0);
            var inDb = await _context.TipoCanchas.FindAsync(result.TipoCanchaId);
            inDb.Should().NotBeNull();
            inDb!.Nombre.Should().Be("Futbol 5");
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenDeleted()
        {
            var tipo = new TipoCancha { Nombre = "Futbol 7" };
            await _context.TipoCanchas.AddAsync(tipo);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            var result = await _repository.DeleteAsync(tipo.TipoCanchaId);

            result.Should().BeTrue();
            var inDb = await _context.TipoCanchas.FindAsync(tipo.TipoCanchaId);
            inDb.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenNotExists()
        {
            var result = await _repository.DeleteAsync(999);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllTipos()
        {
            await _context.TipoCanchas.AddRangeAsync(
                new TipoCancha { Nombre = "T1" },
                new TipoCancha { Nombre = "T2" }
            );
            await _context.SaveChangesAsync();

            var result = await _repository.GetAllAsync();

            result.Should().HaveCountGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnTipo_WhenExists()
        {
            var tipo = new TipoCancha { Nombre = "Tenis" };
            await _context.TipoCanchas.AddAsync(tipo);
            await _context.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(tipo.TipoCanchaId);

            result.Should().NotBeNull();
            result.Nombre.Should().Be("Tenis");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            var result = await _repository.GetByIdAsync(999);
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyTipoCancha()
        {
            var tipo = new TipoCancha { Nombre = "Voley" };
            await _context.TipoCanchas.AddAsync(tipo);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            tipo.Nombre = "Voley Update";
            var result = await _repository.UpdateAsync(tipo);

            result.Should().BeTrue();
            var inDb = await _context.TipoCanchas.FindAsync(tipo.TipoCanchaId);
            inDb!.Nombre.Should().Be("Voley Update");
        }
    }
}
