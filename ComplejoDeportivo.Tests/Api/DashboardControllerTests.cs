using System.Net.Http.Json;
using System.Threading.Tasks;
using ComplejoDeportivo.Application.DTOs.Dashboard;
using FluentAssertions;
using Xunit;

namespace ComplejoDeportivo.Tests.Api
{
    [Collection("ApiTestCollection")]
    public class DashboardControllerTests
    {
        private readonly ApiTestFixture _fixture;

        public DashboardControllerTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetDashboard_ShouldSucceed()
        {
            var token = await _fixture.GetAdminTokenAsync();
            _fixture.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Get complete dashboard
            var getResumen = await _fixture.Client.GetAsync("/api/Dashboard?ComplejoId=1");
            getResumen.EnsureSuccessStatusCode();
            var resumen = await getResumen.Content.ReadFromJsonAsync<DashboardCompletoDto>();
            resumen.Should().NotBeNull();

            _fixture.Client.DefaultRequestHeaders.Authorization = null;
        }

        [Fact]
        public async Task GetDashboard_WithoutComplejoId_ReturnsBadRequest()
        {
            // ComplejoId defaults to 0 when omitted, which fails the DTO's [Range(1, int.MaxValue)] validation.
            var token = await _fixture.GetAdminTokenAsync();
            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "/api/Dashboard");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetDashboard_InvalidModel_ReturnsBadRequest()
        {
            var token = await _fixture.GetAdminTokenAsync();
            // Hasta anterior a Desde viola [CustomValidation] ValidarRangoFechas
            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get,
                "/api/Dashboard?ComplejoId=1&Desde=2025-06-01&Hasta=2025-01-01");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }
    }
}
