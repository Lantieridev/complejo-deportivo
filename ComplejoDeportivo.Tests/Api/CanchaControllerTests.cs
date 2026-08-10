using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ComplejoDeportivo.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace ComplejoDeportivo.Tests.Api
{
    [Collection("ApiTestCollection")]
    public class CanchaControllerTests
    {
        private readonly ApiTestFixture _fixture;

        public CanchaControllerTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Crud_Cancha_Flow_ShouldSucceed()
        {
            var token = await _fixture.GetAdminTokenAsync();
            _fixture.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var createComplejoDto = new CrearComplejoDTO { Nombre = "Comp Cancha", Calle = "Test", Numero = "1", CodigoPostal = "1", Provincia = "P", Ciudad = "C" };
            var compResponse = await _fixture.Client.PostAsJsonAsync("/api/admin/complejos", createComplejoDto);
            var comp = await compResponse.Content.ReadFromJsonAsync<ComplejoDTO>();

            // Create
            var createDto = new CrearCanchaDTO
            {
                Nombre = "New Cancha",
                ComplejoId = comp!.ComplejoId,
                TipoCanchaId = 1,
                TipoSuperficieId = 1
            };
            var createResponse = await _fixture.Client.PostAsJsonAsync("/api/Cancha", createDto);
            createResponse.EnsureSuccessStatusCode();
            var created = await createResponse.Content.ReadFromJsonAsync<CanchaDTO>();
            created.Should().NotBeNull();
            created!.CanchaId.Should().BeGreaterThan(0);

            // GetAll
            var getAllResponse = await _fixture.Client.GetAsync("/api/Cancha");
            getAllResponse.EnsureSuccessStatusCode();
            var all = await getAllResponse.Content.ReadFromJsonAsync<List<CanchaDTO>>();
            all.Should().Contain(c => c.CanchaId == created.CanchaId);

            // GetById
            var getByIdResponse = await _fixture.Client.GetAsync($"/api/Cancha/{created.CanchaId}");
            getByIdResponse.EnsureSuccessStatusCode();
            var single = await getByIdResponse.Content.ReadFromJsonAsync<CanchaDTO>();
            single!.Nombre.Should().Be("New Cancha");

            // Update
            var updateDto = new CrearCanchaDTO
            {
                Nombre = "Updated Cancha",
                ComplejoId = comp.ComplejoId,
                TipoCanchaId = 1,
                TipoSuperficieId = 1
            };
            var updateResponse = await _fixture.Client.PutAsJsonAsync($"/api/Cancha/{created.CanchaId}", updateDto);
            updateResponse.EnsureSuccessStatusCode();

            // Activar/Desactivar
            var descResponse = await _fixture.Client.PutAsync($"/api/Cancha/{created.CanchaId}/desactivar", null);
            descResponse.EnsureSuccessStatusCode();
            
            var actResponse = await _fixture.Client.PutAsync($"/api/Cancha/{created.CanchaId}/activar", null);
            actResponse.EnsureSuccessStatusCode();

            // GetByComplejoId
            var byComplejoResponse = await _fixture.Client.GetAsync($"/api/Cancha/complejo/{comp.ComplejoId}");
            byComplejoResponse.EnsureSuccessStatusCode();
            var byComplejo = await byComplejoResponse.Content.ReadFromJsonAsync<List<CanchaDTO>>();
            byComplejo.Should().Contain(c => c.CanchaId == created.CanchaId);

            // Delete
            var deleteResponse = await _fixture.Client.DeleteAsync($"/api/Cancha/{created.CanchaId}");
            deleteResponse.EnsureSuccessStatusCode();

            _fixture.Client.DefaultRequestHeaders.Authorization = null;
        }

        [Fact]
        public async Task Create_InvalidModel_ReturnsBadRequest()
        {
            // ComplejoId=0 fails [Range(1, ...)], reaching the controller's own ModelState check.
            var token = await _fixture.GetAdminTokenAsync();
            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, "/api/Cancha")
            {
                Content = JsonContent.Create(new CrearCanchaDTO { Nombre = "X", ComplejoId = 0, TipoCanchaId = 1, TipoSuperficieId = 1 })
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }
    }
}
