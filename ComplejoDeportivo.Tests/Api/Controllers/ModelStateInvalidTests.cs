using System.Threading.Tasks;
using ComplejoDeportivo.Api.Controllers;
using ComplejoDeportivo.Application.DTOs;
using ComplejoDeportivo.Application.DTOs.Dashboard;
using ComplejoDeportivo.Application.Repositories;
using ComplejoDeportivo.Application.Services;
using ComplejoDeportivo.Application.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ComplejoDeportivo.Tests.Api.Controllers
{
    // These bypass the HTTP pipeline on purpose: [ApiController]'s automatic model-validation
    // filter (SuppressModelStateInvalidFilter is never set in Program.cs, so it stays enabled)
    // intercepts every invalid ModelState request BEFORE the action body runs, no matter how the
    // JSON payload is crafted. That makes every controller's own inline
    // "if (!ModelState.IsValid) return BadRequest(ModelState);" line structurally unreachable via
    // a real HTTP request. Calling the action directly with a manually-forced ModelState error is
    // the only way to genuinely exercise that line.
    public class ModelStateInvalidTests
    {
        [Fact]
        public async Task AccountController_Register_InvalidModel_ReturnsBadRequest()
        {
            var controller = new AccountController(new Mock<IUsuarioService>().Object);
            controller.ModelState.AddModelError("Email", "Requerido");

            var result = await controller.Register(new RegisterClienteDTO { Email = "a@a.com", Password = "123456", Nombre = "N", Apellido = "A", Telefono = "1", Documento = "1" });

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task AccountController_RegisterEmpleado_InvalidModel_ReturnsBadRequest()
        {
            var controller = new AccountController(new Mock<IUsuarioService>().Object);
            controller.ModelState.AddModelError("Email", "Requerido");

            var result = await controller.RegisterEmpleado(new RegisterClienteDTO { Email = "a@a.com", Password = "123456", Nombre = "N", Apellido = "A", Telefono = "1", Documento = "1" });

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task AdminUsuarioController_Create_InvalidModel_ReturnsBadRequest()
        {
            var controller = new AdminUsuarioController(new Mock<IUsuarioService>().Object);
            controller.ModelState.AddModelError("Email", "Requerido");

            var result = await controller.Create(new CreateUsuarioDTO { Email = "a@a.com", Password = "123456", TipoUsuario = "Administrador" });

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task AuthController_Login_InvalidModel_ReturnsBadRequest()
        {
            var controller = new AuthController(new Mock<IAuthService>().Object);
            controller.ModelState.AddModelError("Email", "Requerido");

            var result = await controller.Login(new LoginRequestDTO { Email = "a@a.com", Password = "123456" });

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CanchaController_Create_InvalidModel_ReturnsBadRequest()
        {
            var controller = new CanchaController(new Mock<ICanchaService>().Object);
            controller.ModelState.AddModelError("Nombre", "Requerido");

            var result = await controller.Create(new CrearCanchaDTO { Nombre = "X", ComplejoId = 1, TipoCanchaId = 1, TipoSuperficieId = 1 });

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ClienteController_Create_InvalidModel_ReturnsBadRequest()
        {
            var controller = new ClienteController(new Mock<IClienteService>().Object, new Mock<IUsuarioRepository>().Object);
            controller.ModelState.AddModelError("Nombre", "Requerido");

            var result = await controller.Create(new CrearClienteDTO { Nombre = "X", Apellido = "Y" });

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ClienteController_Update_InvalidModel_ReturnsBadRequest()
        {
            var controller = new ClienteController(new Mock<IClienteService>().Object, new Mock<IUsuarioRepository>().Object);
            controller.ModelState.AddModelError("Nombre", "Requerido");

            var result = await controller.Update(1, new ActualizarClienteDTO { Nombre = "X", Apellido = "Y" });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ComplejoController_Create_InvalidModel_ReturnsBadRequest()
        {
            var controller = new ComplejoController(new Mock<IComplejoService>().Object);
            controller.ModelState.AddModelError("Nombre", "Requerido");

            var result = await controller.Create(new CrearComplejoDTO { Nombre = "X", Calle = "C", Numero = "1", CodigoPostal = "1", Provincia = "P", Ciudad = "C" });

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task DashboardController_GetDashboardCompleto_InvalidModel_ReturnsBadRequest()
        {
            var controller = new DashboardController(new Mock<IDashboardService>().Object);
            controller.ModelState.AddModelError("ComplejoId", "Requerido");

            var result = await controller.GetDashboardCompleto(new FiltrosDashboardDto());

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task EmpleadoController_Create_InvalidModel_ReturnsBadRequest()
        {
            var controller = new EmpleadoController(new Mock<IEmpleadoService>().Object);
            controller.ModelState.AddModelError("Cargo", "Requerido");

            var result = await controller.Create(new CrearEmpleadoDTO { Nombre = "X", Apellido = "Y", Cargo = "Z" });

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ReservaController_CrearReserva_InvalidModel_ReturnsBadRequest()
        {
            var controller = new ReservaController(new Mock<IReservaService>().Object, new Mock<IUsuarioRepository>().Object);
            controller.ModelState.AddModelError("ClienteId", "Requerido");

            var result = await controller.CrearReserva(new CrearReservaDTO { ClienteId = 1 });

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ReservaController_CancelarReserva_InvalidModel_ReturnsBadRequest()
        {
            var controller = new ReservaController(new Mock<IReservaService>().Object, new Mock<IUsuarioRepository>().Object);
            controller.ModelState.AddModelError("ReservaId", "Requerido");

            var result = await controller.CancelarReserva(new CancelarReservaDTO { ReservaId = 1, ClienteId = 1 });

            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
