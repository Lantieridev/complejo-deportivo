using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ComplejoDeportivo.Application.DTOs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ComplejoDeportivo.Tests.Api
{
    [Collection("ApiTestCollection")]
    public class ReservaControllerTests
    {
        private readonly ApiTestFixture _fixture;

        public ReservaControllerTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        private async Task<(string token, int clienteId)> RegisterClientAsync(string email)
        {
            var unique = Guid.NewGuid().ToString("N")[..15];
            var registerDto = new RegisterClienteDTO
            {
                Email = email,
                Password = "Password123!",
                Nombre = "Test",
                Apellido = "Reserva",
                Telefono = unique[..10],
                Documento = unique
            };
            var registerResponse = await _fixture.Client.PostAsJsonAsync("/api/account/register", registerDto);
            registerResponse.EnsureSuccessStatusCode();

            var loginResponse = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO { Email = email, Password = "Password123!" });
            loginResponse.EnsureSuccessStatusCode();
            var result = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();
            return (result!.Token, result.ClienteId!.Value);
        }

        [Fact]
        public async Task GetComplejos_ReturnsOk()
        {
            var response = await _fixture.Client.GetAsync("/api/Reserva/complejos");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetCanchasPorComplejo_ReturnsOk()
        {
            var response = await _fixture.Client.GetAsync("/api/Reserva/canchas/1");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CrearReserva_InvalidModel_ReturnsBadRequest()
        {
            // Deserializes fine (no "required" props), but ClienteId=0 fails [Range(1, ...)],
            // actually reaching the controller's own ModelState check.
            var (token, _) = await RegisterClientAsync($"reserva-malformed-{Guid.NewGuid():N}@test.com");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Reserva")
            {
                Content = JsonContent.Create(new CrearReservaDTO
                {
                    ClienteId = 0,
                    CanchaIds = new List<int> { 1 },
                    Fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    HoraInicio = new TimeOnly(10, 0),
                    HoraFin = new TimeOnly(11, 0),
                    Ambito = "Web"
                })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CrearReserva_AsClienteForAnotherCliente_ReturnsForbidden()
        {
            var (_, otherClienteId) = await RegisterClientAsync($"reserva-other-{Guid.NewGuid():N}@test.com");
            var (token, _) = await RegisterClientAsync($"reserva-requester-{Guid.NewGuid():N}@test.com");

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Reserva")
            {
                Content = JsonContent.Create(new CrearReservaDTO
                {
                    ClienteId = otherClienteId,
                    CanchaIds = new List<int> { 1 },
                    Fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    HoraInicio = new TimeOnly(10, 0),
                    HoraFin = new TimeOnly(11, 0),
                    Ambito = "Web"
                })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetReservasCliente_AsClienteForAnotherCliente_ReturnsForbidden()
        {
            var (_, otherClienteId) = await RegisterClientAsync($"reserva-list-other-{Guid.NewGuid():N}@test.com");
            var (token, _) = await RegisterClientAsync($"reserva-list-requester-{Guid.NewGuid():N}@test.com");

            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Reserva/cliente/{otherClienteId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CancelarReserva_InvalidModel_ReturnsBadRequest()
        {
            // ReservaId=0 fails [Range(1, ...)], actually reaching the controller's own ModelState check.
            var (token, clienteId) = await RegisterClientAsync($"cancel-malformed-{Guid.NewGuid():N}@test.com");
            using var request = new HttpRequestMessage(HttpMethod.Put, "/api/Reserva/cancelar")
            {
                Content = JsonContent.Create(new CancelarReservaDTO { ReservaId = 0, ClienteId = clienteId, Motivo = "Test" })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CancelarReserva_AsClienteForAnotherCliente_ReturnsForbidden()
        {
            var (_, otherClienteId) = await RegisterClientAsync($"cancel-other-{Guid.NewGuid():N}@test.com");
            var (token, _) = await RegisterClientAsync($"cancel-requester-{Guid.NewGuid():N}@test.com");

            using var request = new HttpRequestMessage(HttpMethod.Put, "/api/Reserva/cancelar")
            {
                Content = JsonContent.Create(new CancelarReservaDTO { ReservaId = 1, ClienteId = otherClienteId, Motivo = "Test" })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CancelarReserva_MenosDe24Horas_ReturnsBadRequest()
        {
            // Reservar para "hoy" siempre deja menos de 24hs de anticipación,
            // disparando la Exception genérica del servicio que el controller mapea a 400.
            // ApiTestFixture no seedea Tarifas (nacen dinámicamente con cada cancha), así que
            // creamos cancha + tarifa propias en vez de depender de una cancha ya creada por otro test.
            var (token, clienteId) = await RegisterClientAsync($"cancel-24h-{Guid.NewGuid():N}@test.com");

            var adminToken = await _fixture.GetAdminTokenAsync();
            using var compRequest = new HttpRequestMessage(HttpMethod.Post, "/api/admin/complejos")
            {
                Content = JsonContent.Create(new CrearComplejoDTO { Nombre = "Comp Cancelar24h", Calle = "T", Numero = "1", CodigoPostal = "1", Provincia = "P", Ciudad = "C" })
            };
            compRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var compResponse = await _fixture.Client.SendAsync(compRequest);
            compResponse.EnsureSuccessStatusCode();
            var comp = await compResponse.Content.ReadFromJsonAsync<ComplejoDTO>();

            using var canchaRequest = new HttpRequestMessage(HttpMethod.Post, "/api/Cancha")
            {
                Content = JsonContent.Create(new CrearCanchaDTO { Nombre = "Cancha Cancelar24h", ComplejoId = comp!.ComplejoId, TipoCanchaId = 1, TipoSuperficieId = 1 })
            };
            canchaRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var canchaResponse = await _fixture.Client.SendAsync(canchaRequest);
            canchaResponse.EnsureSuccessStatusCode();
            var cancha = await canchaResponse.Content.ReadFromJsonAsync<CanchaDTO>();

            var optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ComplejoDeportivo.Infrastructure.Persistence.ComplejoDeportivoContext>();
            optionsBuilder.UseSqlServer(_fixture.ConnectionString);
            using (var context = new ComplejoDeportivo.Infrastructure.Persistence.ComplejoDeportivoContext(optionsBuilder.Options))
            {
                context.Tarifas.Add(new ComplejoDeportivo.Domain.Tarifa
                {
                    CanchaId = cancha!.CanchaId,
                    Precio = 1000,
                    ContratoLuz = true,
                    FechaVigencia = new DateOnly(2020, 1, 1),
                    EsActual = true
                });
                await context.SaveChangesAsync();
            }

            using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/Reserva")
            {
                Content = JsonContent.Create(new CrearReservaDTO
                {
                    ClienteId = clienteId,
                    CanchaIds = new List<int> { cancha.CanchaId },
                    Fecha = DateOnly.FromDateTime(DateTime.Today),
                    HoraInicio = new TimeOnly(21, 0),
                    HoraFin = new TimeOnly(22, 0),
                    Ambito = "Web"
                })
            };
            createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var createResponse = await _fixture.Client.SendAsync(createRequest);
            createResponse.EnsureSuccessStatusCode();
            var reserva = await createResponse.Content.ReadFromJsonAsync<ReservaDTO>();

            using var cancelRequest = new HttpRequestMessage(HttpMethod.Put, "/api/Reserva/cancelar")
            {
                Content = JsonContent.Create(new CancelarReservaDTO { ReservaId = reserva!.ReservaId, ClienteId = clienteId, Motivo = "Muy tarde" })
            };
            cancelRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(cancelRequest);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CancelarReserva_NotFound_ReturnsNotFound()
        {
            var (token, clienteId) = await RegisterClientAsync($"cancel-notfound-{Guid.NewGuid():N}@test.com");

            using var request = new HttpRequestMessage(HttpMethod.Put, "/api/Reserva/cancelar")
            {
                Content = JsonContent.Create(new CancelarReservaDTO { ReservaId = 999999, ClienteId = clienteId, Motivo = "Test" })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
