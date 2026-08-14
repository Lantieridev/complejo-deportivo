using System;
using System.Threading.Tasks;
using ComplejoDeportivo.Application.DTOs;
using ComplejoDeportivo.Application.Repositories;
using ComplejoDeportivo.Application.Services.Implementations;
using ComplejoDeportivo.Domain;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace ComplejoDeportivo.Tests.Application.Services
{
    public class AuthServiceTests
    {
        [Fact]
        public async Task LoginAsync_WrongPassword_ThrowsUnauthorized()
        {
            var repo = new Mock<IUsuarioRepository>();
            repo.Setup(r => r.GetByEmailAsync("a@a.com")).ReturnsAsync(new Usuario
            {
                Email = "a@a.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct"),
                TipoUsuario = "Cliente"
            });
            var config = new Mock<IConfiguration>();
            var service = new AuthService(repo.Object, config.Object);

            var act = () => service.LoginAsync(new LoginRequestDTO { Email = "a@a.com", Password = "wrong" });

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task LoginAsync_UserNotFound_ThrowsUnauthorized()
        {
            var repo = new Mock<IUsuarioRepository>();
            repo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Usuario?)null);
            var config = new Mock<IConfiguration>();
            var service = new AuthService(repo.Object, config.Object);

            var act = () => service.LoginAsync(new LoginRequestDTO { Email = "nadie@a.com", Password = "x" });

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task LoginAsync_MissingJwtKey_ThrowsInvalidOperation()
        {
            // GenerateJwtToken's `_configuration["Jwt:Key"] ?? throw new InvalidOperationException(...)`
            // is only reachable once credentials are valid; this is the only realistic way to hit it.
            var repo = new Mock<IUsuarioRepository>();
            repo.Setup(r => r.GetByEmailAsync("a@a.com")).ReturnsAsync(new Usuario
            {
                Email = "a@a.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct"),
                TipoUsuario = "Cliente"
            });
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["Jwt:Key"]).Returns((string?)null);
            var service = new AuthService(repo.Object, config.Object);

            var act = () => service.LoginAsync(new LoginRequestDTO { Email = "a@a.com", Password = "correct" });

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsTokenWithEmpleadoRole()
        {
            var repo = new Mock<IUsuarioRepository>();
            repo.Setup(r => r.GetByEmailAsync("emp@a.com")).ReturnsAsync(new Usuario
            {
                UsuarioId = 1,
                Email = "emp@a.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct"),
                TipoUsuario = "Empleado",
                Empleado = new Empleado { Nombre = "N", Apellido = "A", Cargo = "Manager" }
            });
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["Jwt:Key"]).Returns("una-clave-de-prueba-suficientemente-larga-123456");
            config.Setup(c => c["Jwt:Issuer"]).Returns("test-issuer");
            config.Setup(c => c["Jwt:Audience"]).Returns("test-audience");
            var service = new AuthService(repo.Object, config.Object);

            var result = await service.LoginAsync(new LoginRequestDTO { Email = "emp@a.com", Password = "correct" });

            result.Token.Should().NotBeNullOrEmpty();
            result.Rol.Should().Be("Manager");
        }
    }
}
