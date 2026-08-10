using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ComplejoDeportivo.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace ComplejoDeportivo.Tests.Api
{
    [Collection("ApiTestCollection")]
    public class NegativeControllerTests
    {
        private readonly ApiTestFixture _fixture;

        public NegativeControllerTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task AllControllers_NotFound_ShouldReturn404()
        {
            var token = await _fixture.GetAdminTokenAsync();
            _fixture.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // AdminUsuario
            (await _fixture.Client.GetAsync("/api/admin/usuarios/99999")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await _fixture.Client.PutAsJsonAsync("/api/admin/usuarios/99999", new { UsuarioId = 99999, Email = "a@a.com", TipoUsuario = "A", Activo = true })).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await _fixture.Client.DeleteAsync("/api/admin/usuarios/99999")).StatusCode.Should().Be(HttpStatusCode.NotFound);

            // Cancha
            (await _fixture.Client.GetAsync("/api/Cancha/99999")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await _fixture.Client.PutAsJsonAsync("/api/Cancha/99999", new { Nombre = "A", ComplejoId = 1, TipoCanchaId = 1, TipoSuperficieId = 1, PrecioReservaLuz = (decimal?)1, PrecioReservaSinLuz = (decimal?)1 })).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await _fixture.Client.DeleteAsync("/api/Cancha/99999")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await _fixture.Client.PutAsync("/api/Cancha/99999/activar", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await _fixture.Client.PutAsync("/api/Cancha/99999/desactivar", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);

            // Cliente
            (await _fixture.Client.GetAsync("/api/admin/clientes/99999")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await _fixture.Client.PutAsJsonAsync("/api/admin/clientes/99999", new { Nombre = "A", Apellido = "B", Telefono = "1", Documento = "1" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await _fixture.Client.DeleteAsync("/api/admin/clientes/99999")).StatusCode.Should().Be(HttpStatusCode.NotFound);

            // Empleado
            (await _fixture.Client.GetAsync("/api/admin/empleados/99999")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await _fixture.Client.PutAsJsonAsync("/api/admin/empleados/99999", new { Nombre = "A", Apellido = "B", Telefono = "1", Documento = "1", Cargo = "A", ComplejoId = 1 })).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await _fixture.Client.DeleteAsync("/api/admin/empleados/99999")).StatusCode.Should().Be(HttpStatusCode.NotFound);

            // TipoCancha
            (await _fixture.Client.GetAsync("/api/TiposCancha/99999")).StatusCode.Should().Be(HttpStatusCode.NotFound);

            // Reserva
            (await _fixture.Client.PostAsJsonAsync("/api/Reserva/99999/cancelar", new { })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task AllControllers_BadRequest_ShouldReturn400()
        {
            var token = await _fixture.GetAdminTokenAsync();
            _fixture.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Cancha
            (await _fixture.Client.PostAsJsonAsync("/api/Cancha", new { })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Cliente
            (await _fixture.Client.PostAsJsonAsync("/api/admin/clientes", new { })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Empleado
            (await _fixture.Client.PostAsJsonAsync("/api/admin/empleados", new { })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // AdminUsuario
            (await _fixture.Client.PostAsJsonAsync("/api/admin/usuarios", new { })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // TipoCancha
            (await _fixture.Client.PostAsJsonAsync("/api/TiposCancha", new { })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Reserva
            (await _fixture.Client.PostAsJsonAsync("/api/Reserva", new { })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            
            // Auth / Account
            (await _fixture.Client.PostAsJsonAsync("/api/auth/login", new { })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await _fixture.Client.PostAsJsonAsync("/api/account/register", new { })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await _fixture.Client.PostAsJsonAsync("/api/account/register-empleado", new { })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
