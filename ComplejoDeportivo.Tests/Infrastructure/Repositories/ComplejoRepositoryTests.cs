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
    public class ComplejoRepositoryTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
    {
        private readonly DatabaseFixture _fixture;
        private ComplejoDeportivoContext _context = null!;
        private ComplejoRepository _repository = null!;

        public ComplejoRepositoryTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public Task InitializeAsync()
        {
            _context = new ComplejoDeportivoContext(_fixture.Options);
            _context.Database.BeginTransaction();
            _repository = new ComplejoRepository(_context);
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
        public async Task GetAllAsync_ShouldReturnAllComplejos()
        {
            var dir1 = new Direccion { Calle = "Calle A", Ciudad = "Madrid", Numero = "1", CodigoPostal = "28001", Provincia = "Provincia" };
            var dir2 = new Direccion { Calle = "Calle B", Ciudad = "Barcelona", Numero = "2", CodigoPostal = "28001", Provincia = "Provincia" };
            _context.Direccions.AddRange(dir1, dir2);
            await _context.SaveChangesAsync();

            _context.Complejos.Add(new Complejo { Nombre = "Complejo 1", DireccionId = dir1.DireccionId });
            _context.Complejos.Add(new Complejo { Nombre = "Centro 2", DireccionId = dir2.DireccionId });
            await _context.SaveChangesAsync();

            var result = await _repository.GetAllAsync();

            result.Should().NotBeEmpty();
            result.Count().Should().BeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task GetAllAsync_ShouldFilterBySearchTerm()
        {
            var dir1 = new Direccion { Calle = "Calle A", Ciudad = "Madrid", Numero = "1", CodigoPostal = "28001", Provincia = "Provincia" };
            var dir2 = new Direccion { Calle = "Calle B", Ciudad = "Barcelona", Numero = "2", CodigoPostal = "28001", Provincia = "Provincia" };
            _context.Direccions.AddRange(dir1, dir2);
            await _context.SaveChangesAsync();

            _context.Complejos.Add(new Complejo { Nombre = "Complejo Madrid", DireccionId = dir1.DireccionId });
            _context.Complejos.Add(new Complejo { Nombre = "Centro Barcelona", DireccionId = dir2.DireccionId });
            await _context.SaveChangesAsync();

            var resultMadrid = await _repository.GetAllAsync("madrid");
            resultMadrid.Should().ContainSingle(c => c.Nombre == "Complejo Madrid");

            var resultCentro = await _repository.GetAllAsync("centro");
            resultCentro.Should().ContainSingle(c => c.Nombre == "Centro Barcelona");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnComplejo_WhenExists()
        {
            var dir = new Direccion { Calle = "Calle X", Ciudad = "Sevilla", Numero = "10", CodigoPostal = "28001", Provincia = "Provincia" };
            _context.Direccions.Add(dir);
            await _context.SaveChangesAsync();

            var complejo = new Complejo { Nombre = "Complejo X", DireccionId = dir.DireccionId };
            _context.Complejos.Add(complejo);
            await _context.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(complejo.ComplejoId);

            result.Should().NotBeNull();
            result.Nombre.Should().Be("Complejo X");
            result.Direccion.Should().NotBeNull();
            result.Direccion.Ciudad.Should().Be("Sevilla");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            var result = await _repository.GetByIdAsync(9999);
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_ShouldAddComplejoAndDireccion()
        {
            var complejo = new Complejo { Nombre = "Nuevo Complejo" };
            var direccion = new Direccion { Calle = "Nueva Calle", Ciudad = "Valencia", Numero = "5", CodigoPostal = "28001", Provincia = "Provincia" };

            var result = await _repository.CreateAsync(complejo, direccion);

            result.Should().NotBeNull();
            result.ComplejoId.Should().BeGreaterThan(0);
            result.DireccionId.Should().BeGreaterThan(0);

            var dbComplejo = await _context.Complejos.Include(c => c.Direccion).FirstOrDefaultAsync(c => c.ComplejoId == result.ComplejoId);
            dbComplejo.Should().NotBeNull();
            dbComplejo.Direccion.Calle.Should().Be("Nueva Calle");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowAndRollback_OnError()
        {
            var complejo = new Complejo { Nombre = "Fallo Complejo" };
            // Pass empty/invalid Direccion to trigger DbUpdateException in SaveChanges
            var invalidDir = new Direccion(); 
            await Assert.ThrowsAnyAsync<Exception>(() => _repository.CreateAsync(complejo, invalidDir));

            var dbComplejo = await _context.Complejos.FirstOrDefaultAsync(c => c.Nombre == "Fallo Complejo");
            dbComplejo.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowAndRollback_WhenOwnsTransaction_OnError()
        {
            // The fixture wraps every test in an ambient transaction, so ownsTransaction is always
            // false elsewhere in this file. Ending it here lets CreateAsync open (and roll back)
            // its own transaction, exercising the ownsTransaction==true branch.
            await _context.Database.RollbackTransactionAsync();

            var complejo = new Complejo { Nombre = "Fallo Complejo Propio" };
            var invalidDir = new Direccion();

            await Assert.ThrowsAnyAsync<Exception>(() => _repository.CreateAsync(complejo, invalidDir));

            var dbComplejo = await _context.Complejos.FirstOrDefaultAsync(c => c.Nombre == "Fallo Complejo Propio");
            dbComplejo.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateComplejoAndDireccion()
        {
            var dir = new Direccion { Calle = "Old Calle", Ciudad = "Zaragoza", Numero = "1", CodigoPostal = "28001", Provincia = "Provincia" };
            _context.Direccions.Add(dir);
            await _context.SaveChangesAsync();

            var complejo = new Complejo { Nombre = "Old Complejo", DireccionId = dir.DireccionId };
            _context.Complejos.Add(complejo);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            var toUpdateComplejo = await _context.Complejos.FindAsync(complejo.ComplejoId);
            var toUpdateDireccion = await _context.Direccions.FindAsync(dir.DireccionId);

            toUpdateComplejo!.Nombre = "Updated Complejo";
            toUpdateDireccion!.Calle = "Updated Calle";

            var result = await _repository.UpdateAsync(toUpdateComplejo, toUpdateDireccion);

            result.Should().BeTrue();

            _context.ChangeTracker.Clear();
            var dbComplejo = await _context.Complejos.Include(c => c.Direccion).FirstOrDefaultAsync(c => c.ComplejoId == complejo.ComplejoId);
            dbComplejo!.Nombre.Should().Be("Updated Complejo");
            dbComplejo.Direccion.Calle.Should().Be("Updated Calle");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowAndRollback_OnError()
        {
            var dir = new Direccion { Calle = "Calle 1", Ciudad = "Zaragoza", Numero = "1", CodigoPostal = "28001", Provincia = "Provincia" };
            _context.Direccions.Add(dir);
            await _context.SaveChangesAsync();

            var complejo = new Complejo { Nombre = "Complejo 1", DireccionId = dir.DireccionId };
            _context.Complejos.Add(complejo);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            var toUpdateComplejo = await _context.Complejos.FindAsync(complejo.ComplejoId);
            var toUpdateDireccion = await _context.Direccions.FindAsync(dir.DireccionId);
            toUpdateDireccion!.CodigoPostal = null!; // Trigger DbUpdateException

            await Assert.ThrowsAnyAsync<Exception>(() => _repository.UpdateAsync(toUpdateComplejo!, toUpdateDireccion!));
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowAndRollback_WhenOwnsTransaction_OnError()
        {
            // Ending the fixture's ambient transaction up front lets UpdateAsync open (and roll
            // back) its own, exercising the ownsTransaction==true branch instead of always
            // skipping it. The insert below is then a durable, autocommitted write (not covered
            // by the fixture's per-test rollback), so it's cleaned up manually at the end.
            await _context.Database.RollbackTransactionAsync();

            var dir = new Direccion { Calle = "Calle 1", Ciudad = "Zaragoza", Numero = "1", CodigoPostal = "28001", Provincia = "Provincia" };
            _context.Direccions.Add(dir);
            await _context.SaveChangesAsync();

            var complejo = new Complejo { Nombre = "Complejo Propio", DireccionId = dir.DireccionId };
            _context.Complejos.Add(complejo);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            var toUpdateComplejo = await _context.Complejos.FindAsync(complejo.ComplejoId);
            var toUpdateDireccion = await _context.Direccions.FindAsync(dir.DireccionId);
            toUpdateDireccion!.CodigoPostal = null!; // Trigger DbUpdateException

            try
            {
                await Assert.ThrowsAnyAsync<Exception>(() => _repository.UpdateAsync(toUpdateComplejo!, toUpdateDireccion!));
            }
            finally
            {
                _context.ChangeTracker.Clear();
                _context.Complejos.Remove(await _context.Complejos.FindAsync(complejo.ComplejoId) ?? complejo);
                _context.Direccions.Remove(await _context.Direccions.FindAsync(dir.DireccionId) ?? dir);
                await _context.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenExists()
        {
            var dir = new Direccion { Calle = "Borrar Calle", Ciudad = "Zaragoza", Numero = "1", CodigoPostal = "28001", Provincia = "Provincia" };
            _context.Direccions.Add(dir);
            await _context.SaveChangesAsync();

            var complejo = new Complejo { Nombre = "Borrar Complejo", DireccionId = dir.DireccionId };
            _context.Complejos.Add(complejo);
            await _context.SaveChangesAsync();

            var result = await _repository.DeleteAsync(complejo.ComplejoId);

            result.Should().BeTrue();

            var dbComplejo = await _context.Complejos.FindAsync(complejo.ComplejoId);
            dbComplejo.Should().BeNull();
            var dbDir = await _context.Direccions.FindAsync(dir.DireccionId);
            dbDir.Should().BeNull(); // Direccion is deleted with Complejo
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenNotExists()
        {
            var result = await _repository.DeleteAsync(9999);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowInvalidOperationException_OnError()
        {
            var dir = new Direccion { Calle = "Calle 1", Ciudad = "Zaragoza", Numero = "1", CodigoPostal = "28001", Provincia = "Provincia" };
            _context.Direccions.Add(dir);
            await _context.SaveChangesAsync();

            var complejo = new Complejo { Nombre = "Complejo Error", DireccionId = dir.DireccionId };
            _context.Complejos.Add(complejo);
            
            var tipoCancha = new TipoCancha { Nombre = "T1" };
            var tipoSuperficie = new TipoSuperficie { Nombre = "S1" };
            _context.TipoCanchas.Add(tipoCancha);
            _context.TipoSuperficies.Add(tipoSuperficie);
            await _context.SaveChangesAsync();

            var cancha = new Cancha { Nombre = "Cancha 1", ComplejoId = complejo.ComplejoId, TipoCanchaId = tipoCancha.TipoCanchaId, TipoSuperficieId = tipoSuperficie.TipoSuperficieId, Activa = true };
            _context.Canchas.Add(cancha);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            // Should throw due to FK constraint with Cancha
            await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.DeleteAsync(complejo.ComplejoId));
        }
    }
}
