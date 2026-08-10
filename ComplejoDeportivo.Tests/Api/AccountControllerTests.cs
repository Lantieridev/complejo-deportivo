using System;
using System.Net;
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
        public async Task RegisterEmpleado_InvalidModel_ReturnsBadRequest()
        {
            var response = await _fixture.Client.PostAsJsonAsync("/api/account/register-empleado", BuildDto("not-an-email"));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task RegisterEmpleado_DuplicateEmail_ReturnsBadRequest()
        {
            var dto = BuildDto($"dup-emp-{Guid.NewGuid():N}@test.com");
            var first = await _fixture.Client.PostAsJsonAsync("/api/account/register-empleado", dto);
            first.EnsureSuccessStatusCode();

            var second = await _fixture.Client.PostAsJsonAsync("/api/account/register-empleado", BuildDto(dto.Email));

            second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
