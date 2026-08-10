using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ComplejoDeportivo.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace ComplejoDeportivo.Tests.Api
{
    [Collection("ApiTestCollection")]
    public class AccountControllerTests
    {
        private readonly ApiTestFixture _fixture;

        public AccountControllerTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        private static RegisterClienteDTO BuildDto(string email)
        {
            var unique = Guid.NewGuid().ToString("N")[..15];
            return new RegisterClienteDTO
            {
                Email = email,
                Password = "Password123!",
                Nombre = "Test",
                Apellido = "Account",
                Telefono = unique[..10],
                Documento = unique
            };
        }

        [Fact]
        public async Task Register_InvalidModel_ReturnsBadRequest()
        {
            // All "required" properties present so deserialization succeeds; Email is
            // malformed (present but invalid) so [EmailAddress] actually fails ModelState.
            var response = await _fixture.Client.PostAsJsonAsync("/api/account/register", BuildDto("not-an-email"));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsBadRequest()
        {
            var dto = BuildDto($"dup-{Guid.NewGuid():N}@test.com");
            var first = await _fixture.Client.PostAsJsonAsync("/api/account/register", dto);
            first.EnsureSuccessStatusCode();

            var second = await _fixture.Client.PostAsJsonAsync("/api/account/register", BuildDto(dto.Email));

            second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task RegisterEmpleado_WithoutAuth_ReturnsUnauthorized()
        {
            // /api/account/register-empleado requires an Admin token (fixed 2026-08-10: was
            // [AllowAnonymous], letting anyone self-register a staff account). _fixture.Client is
            // shared across the whole collection, so explicitly clear any Authorization another
            // test may have left on it -- otherwise this test's outcome depends on run order.
            _fixture.Client.DefaultRequestHeaders.Authorization = null;

            var response = await _fixture.Client.PostAsJsonAsync("/api/account/register-empleado", BuildDto("noauth@test.com"));
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task RegisterEmpleado_InvalidModel_ReturnsBadRequest()
        {
            var token = await _fixture.GetAdminTokenAsync();
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/account/register-empleado")
            {
                Content = JsonContent.Create(BuildDto("not-an-email"))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task RegisterEmpleado_DuplicateEmail_ReturnsBadRequest()
        {
            var token = await _fixture.GetAdminTokenAsync();
            var dto = BuildDto($"dup-emp-{Guid.NewGuid():N}@test.com");

            using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/account/register-empleado") { Content = JsonContent.Create(dto) };
            firstRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var first = await _fixture.Client.SendAsync(firstRequest);
            first.EnsureSuccessStatusCode();

            using var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/account/register-empleado") { Content = JsonContent.Create(BuildDto(dto.Email)) };
            secondRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var second = await _fixture.Client.SendAsync(secondRequest);

            second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
