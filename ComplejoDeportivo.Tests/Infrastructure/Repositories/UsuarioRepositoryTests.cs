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
    public class UsuarioRepositoryTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
    {
        private readonly DatabaseFixture _fixture;
        private ComplejoDeportivoContext _context = null!;
        private UsuarioRepository _repository = null!;

        public UsuarioRepositoryTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            _context = new ComplejoDeportivoContext(_fixture.Options);
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM Usuario");
            _repository = new UsuarioRepository(_context);
        }

        public async Task DisposeAsync()
        {
            if (_context != null)
            {
                await _context.DisposeAsync();
            }
        }

        [Fact]
        public async Task GetByEmailAsync_ShouldReturnUsuario_WhenExists()
        {
            var empleado = new Empleado { Nombre = "Emp", Apellido = "1", Cargo = "C1" };
            await _context.Empleados.AddAsync(empleado);
            var usuario = new Usuario { Email = "u1@test.com", PasswordHash = "hash", TipoUsuario = "Empleado", Empleado = empleado, FechaRegistro = DateTime.UtcNow };
            await _context.Usuarios.AddAsync(usuario);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            var result = await _repository.GetByEmailAsync("U1@TEST.com"); // check ToLower

            result.Should().NotBeNull();
            result!.Email.Should().Be("u1@test.com");
            result.Empleado.Should().NotBeNull();
            result.Empleado!.Nombre.Should().Be("Emp");
        }

        [Fact]
        public async Task GetByEmailAsync_ShouldReturnNull_WhenNotExists()
        {
            var result = await _repository.GetByEmailAsync("notfound@test.com");
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllUsuarios()
        {
            await _context.Usuarios.AddRangeAsync(
                new Usuario { Email = "a@test.com", PasswordHash = "123", TipoUsuario = "Administrador", FechaRegistro = DateTime.UtcNow },
                new Usuario { Email = "b@test.com", PasswordHash = "123", TipoUsuario = "Administrador", FechaRegistro = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            var result = await _repository.GetAllAsync();
            result.Should().HaveCountGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnUsuario_WhenExists()
        {
            var usuario = new Usuario { Email = "id@test.com", PasswordHash = "123", TipoUsuario = "Administrador", FechaRegistro = DateTime.UtcNow };
            await _context.Usuarios.AddAsync(usuario);
            await _context.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(usuario.UsuarioId);

            result.Should().NotBeNull();
            result!.Email.Should().Be("id@test.com");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            var result = await _repository.GetByIdAsync(999);
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_ShouldAddUsuario()
        {
            var usuario = new Usuario { Email = "create@test.com", PasswordHash = "123", TipoUsuario = "Administrador", FechaRegistro = DateTime.UtcNow };

            var result = await _repository.CreateAsync(usuario);

            result.UsuarioId.Should().BeGreaterThan(0);
            var inDb = await _context.Usuarios.FindAsync(result.UsuarioId);
            inDb.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyUsuario_ExceptPasswordAndFecha()
        {
            var dt = new DateTime(2025, 1, 1);
            var usuario = new Usuario { Email = "upd@test.com", PasswordHash = "oldhash", TipoUsuario = "Administrador", FechaRegistro = dt };
            await _context.Usuarios.AddAsync(usuario);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            usuario.Email = "upd_new@test.com";
            usuario.PasswordHash = "newhash";
            usuario.FechaRegistro = new DateTime(2025, 2, 2);

            var result = await _repository.UpdateAsync(usuario);

            result.Should().BeTrue();
            var inDb = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.UsuarioId == usuario.UsuarioId);
            inDb!.Email.Should().Be("upd_new@test.com");
            inDb.PasswordHash.Should().Be("oldhash"); // Not modified
            inDb.FechaRegistro.Should().Be(dt); // Not modified
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenDeleted()
        {
            var usuario = new Usuario { Email = "del@test.com", PasswordHash = "123", TipoUsuario = "Administrador", FechaRegistro = DateTime.UtcNow };
            await _context.Usuarios.AddAsync(usuario);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            var result = await _repository.DeleteAsync(usuario.UsuarioId);

            result.Should().BeTrue();
            var inDb = await _context.Usuarios.FindAsync(usuario.UsuarioId);
            inDb.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenNotExists()
        {
            var result = await _repository.DeleteAsync(999);
            result.Should().BeFalse();
        }
    }
}
