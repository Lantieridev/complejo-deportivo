using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ComplejoDeportivo.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace ComplejoDeportivo.Tests.Api
{
    [Collection("ApiTestCollection")]
    public class AuthControllerTests
    {
        private readonly ApiTestFixture _fixture;

        public AuthControllerTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Login_InvalidModel_ReturnsBadRequest()
        {
            // Email present but malformed and Password present but empty, so deserialization
            // succeeds and [EmailAddress]/[Required] actually fail ModelState.
            var response = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO { Email = "not-an-email", Password = "" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Login_WrongPassword_ReturnsUnauthorized()
        {
            var email = $"auth-wrongpass-{System.Guid.NewGuid():N}@test.com";
            var registerDto = new RegisterClienteDTO
            {
                Email = email,
                Password = "Password123!",
                Nombre = "Test",
                Apellido = "Auth",
                Telefono = System.Guid.NewGuid().ToString("N")[..10],
                Documento = System.Guid.NewGuid().ToString("N")[..10]
            };
            await _fixture.Client.PostAsJsonAsync("/api/account/register", registerDto);

            var response = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO { Email = email, Password = "WrongPassword!" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_NonExistentUser_ReturnsUnauthorized()
        {
            var response = await _fixture.Client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO { Email = "no-existe@test.com", Password = "Password123!" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
