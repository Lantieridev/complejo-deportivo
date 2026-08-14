using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ComplejoDeportivo.Api.Controllers;
using ComplejoDeportivo.Application.DTOs;
using ComplejoDeportivo.Application.Repositories;
using ComplejoDeportivo.Application.Services;
using ComplejoDeportivo.Domain;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ComplejoDeportivo.Tests.Api.Controllers
{
    // Same rationale as ClienteControllerUnitTests: the ownership-check edge branches (missing
    // email claim, usuario-not-found) can't be produced by a real JWT from the actual login flow,
    // so they're exercised here with a hand-built ClaimsPrincipal instead of an HTTP round trip.
    public class ReservaControllerUnitTests
    {
        private static ReservaController BuildController(Mock<IUsuarioRepository> repoMock, ClaimsPrincipal user, Mock<IReservaService>? serviceMock = null)
        {
            var controller = new ReservaController((serviceMock ?? new Mock<IReservaService>()).Object, repoMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            return controller;
        }

        private static ClaimsPrincipal ClienteUserWithoutEmailClaim()
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Cliente") }, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        private static ClaimsPrincipal ClienteUserWithEmail(string email)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Role, "Cliente"),
                new Claim(ClaimTypes.Email, email)
            }, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        [Fact]
        public async Task GetReservasCliente_ClienteRoleWithoutEmailClaim_ReturnsUnauthorized()
        {
            var repo = new Mock<IUsuarioRepository>();
            var controller = BuildController(repo, ClienteUserWithoutEmailClaim());

            var result = await controller.GetReservasCliente(1);

            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetReservasCliente_ClienteRoleWithNoMatchingUsuario_ReturnsForbidden()
        {
            var repo = new Mock<IUsuarioRepository>();
            repo.Setup(r => r.GetByEmailAsync("nadie@test.com")).ReturnsAsync((Usuario?)null);
            var controller = BuildController(repo, ClienteUserWithEmail("nadie@test.com"));

            var result = await controller.GetReservasCliente(1);

            result.Result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task CrearReserva_ClienteRoleWithoutEmailClaim_ReturnsUnauthorized()
        {
            var repo = new Mock<IUsuarioRepository>();
            var controller = BuildController(repo, ClienteUserWithoutEmailClaim());

            var result = await controller.CrearReserva(new CrearReservaDTO { ClienteId = 1 });

            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task CrearReserva_ClienteRoleWithNoMatchingUsuario_ReturnsForbidden()
        {
            var repo = new Mock<IUsuarioRepository>();
            repo.Setup(r => r.GetByEmailAsync("nadie@test.com")).ReturnsAsync((Usuario?)null);
            var controller = BuildController(repo, ClienteUserWithEmail("nadie@test.com"));

            var result = await controller.CrearReserva(new CrearReservaDTO { ClienteId = 1 });

            result.Result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task CancelarReserva_ClienteRoleWithoutEmailClaim_ReturnsUnauthorized()
        {
            var repo = new Mock<IUsuarioRepository>();
            var controller = BuildController(repo, ClienteUserWithoutEmailClaim());

            var result = await controller.CancelarReserva(new CancelarReservaDTO { ReservaId = 1, ClienteId = 1 });

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task CancelarReserva_ClienteRoleWithNoMatchingUsuario_ReturnsForbidden()
        {
            var repo = new Mock<IUsuarioRepository>();
            repo.Setup(r => r.GetByEmailAsync("nadie@test.com")).ReturnsAsync((Usuario?)null);
            var controller = BuildController(repo, ClienteUserWithEmail("nadie@test.com"));

            var result = await controller.CancelarReserva(new CancelarReservaDTO { ReservaId = 1, ClienteId = 1 });

            result.Should().BeOfType<ForbidResult>();
        }

        // A real JWT always stamps a Role claim (AuthService.GenerateJwtToken), so
        // `User.FindFirst(ClaimTypes.Role)?.Value` returning null can only be reached this way.
        private static ClaimsPrincipal UserWithNoRoleClaimAtAll() => new(new ClaimsIdentity("TestAuth"));

        [Fact]
        public async Task GetReservasCliente_UserWithNoRoleClaimAtAll_IsTreatedAsValid()
        {
            var repo = new Mock<IUsuarioRepository>();
            var controller = BuildController(repo, UserWithNoRoleClaimAtAll());

            var result = await controller.GetReservasCliente(1);

            result.Result.Should().NotBeOfType<ForbidResult>();
            repo.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CrearReserva_UserWithNoRoleClaimAtAll_IsTreatedAsValid()
        {
            var repo = new Mock<IUsuarioRepository>();
            var service = new Mock<IReservaService>();
            service.Setup(s => s.CrearReserva(It.IsAny<CrearReservaDTO>())).ReturnsAsync(new ReservaDTO { ReservaId = 1, ClienteId = 1, Estado = "Confirmada" });
            var controller = BuildController(repo, UserWithNoRoleClaimAtAll(), service);

            var result = await controller.CrearReserva(new CrearReservaDTO { ClienteId = 1 });

            result.Result.Should().NotBeOfType<ForbidResult>();
            repo.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CancelarReserva_UserWithNoRoleClaimAtAll_IsTreatedAsValid()
        {
            var repo = new Mock<IUsuarioRepository>();
            var service = new Mock<IReservaService>();
            service.Setup(s => s.CancelarReserva(It.IsAny<CancelarReservaDTO>())).ReturnsAsync(true);
            var controller = BuildController(repo, UserWithNoRoleClaimAtAll(), service);

            var result = await controller.CancelarReserva(new CancelarReservaDTO { ReservaId = 1, ClienteId = 1 });

            result.Should().NotBeOfType<ForbidResult>();
            repo.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CrearReserva_ServiceThrows_ReturnsBadRequest()
        {
            var repo = new Mock<IUsuarioRepository>();
            var service = new Mock<IReservaService>();
            service.Setup(s => s.CrearReserva(It.IsAny<CrearReservaDTO>())).ThrowsAsync(new Exception("Horario ocupado"));
            var controller = BuildController(repo, UserWithNoRoleClaimAtAll(), service);

            var result = await controller.CrearReserva(new CrearReservaDTO { ClienteId = 1 });

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
