using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ComplejoDeportivo.Api.Controllers;
using ComplejoDeportivo.Application.DTOs;
using ComplejoDeportivo.Application.Repositories;
using ComplejoDeportivo.Application.Services.Interfaces;
using ComplejoDeportivo.Domain;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ComplejoDeportivo.Tests.Api.Controllers
{
    // EsClienteValido's edge branches (missing/empty email claim, usuario-not-found) can never be
    // produced by a real JWT issued through the actual login flow — AuthService always stamps an
    // Email claim, and a Cliente-role token always corresponds to a real Usuario row. Unit-testing
    // the controller directly with a hand-built ClaimsPrincipal is the only way to reach them.
    public class ClienteControllerUnitTests
    {
        private static ClienteController BuildController(Mock<IUsuarioRepository> repoMock, ClaimsPrincipal user)
        {
            var controller = new ClienteController(new Mock<IClienteService>().Object, repoMock.Object);
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
        public async Task GetById_ClienteRoleWithoutEmailClaim_ReturnsForbidden()
        {
            var repo = new Mock<IUsuarioRepository>();
            var controller = BuildController(repo, ClienteUserWithoutEmailClaim());

            var result = await controller.GetById(1);

            result.Result.Should().BeOfType<ForbidResult>();
            repo.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetById_ClienteRoleWithNoMatchingUsuario_ReturnsForbidden()
        {
            var repo = new Mock<IUsuarioRepository>();
            repo.Setup(r => r.GetByEmailAsync("nadie@test.com")).ReturnsAsync((Usuario?)null);
            var controller = BuildController(repo, ClienteUserWithEmail("nadie@test.com"));

            var result = await controller.GetById(1);

            result.Result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task GetById_UserWithNoRoleClaimAtAll_IsTreatedAsValid()
        {
            // A real JWT always stamps a Role claim (AuthService.GenerateJwtToken), so
            // `User.FindFirst(ClaimTypes.Role)?.Value` returning null can only be reached this way.
            var repo = new Mock<IUsuarioRepository>();
            var controller = BuildController(repo, new ClaimsPrincipal(new ClaimsIdentity("TestAuth")));

            var result = await controller.GetById(1);

            result.Result.Should().NotBeOfType<ForbidResult>();
            repo.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Never);
        }
    }
}
