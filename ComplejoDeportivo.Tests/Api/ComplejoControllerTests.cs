using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ComplejoDeportivo.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace ComplejoDeportivo.Tests.Api
{
    [Collection("ApiTestCollection")]
    public class ComplejoControllerTests
    {
        private readonly ApiTestFixture _fixture;

        public ComplejoControllerTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Crud_Complejo_Flow_ShouldSucceed()
        {
            var token = await _fixture.GetAdminTokenAsync();
            _fixture.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Create
            var createDto = new CrearComplejoDTO { Nombre = "New Complejo", Calle = "Test", Numero = "1", CodigoPostal = "1", Provincia = "P", Ciudad = "C" };
            var createResponse = await _fixture.Client.PostAsJsonAsync("/api/admin/complejos", createDto);
            createResponse.EnsureSuccessStatusCode();
            var created = await createResponse.Content.ReadFromJsonAsync<ComplejoDTO>();
            created.Should().NotBeNull();
            created!.ComplejoId.Should().BeGreaterThan(0);

            // GetAll
            var getAllResponse = await _fixture.Client.GetAsync("/api/admin/complejos");
            getAllResponse.EnsureSuccessStatusCode();
            var all = await getAllResponse.Content.ReadFromJsonAsync<List<ComplejoDTO>>();
            all.Should().Contain(c => c.ComplejoId == created.ComplejoId);

            // GetById
            var getByIdResponse = await _fixture.Client.GetAsync($"/api/admin/complejos/{created.ComplejoId}");
            getByIdResponse.EnsureSuccessStatusCode();
            var single = await getByIdResponse.Content.ReadFromJsonAsync<ComplejoDTO>();
            single!.Nombre.Should().Be("New Complejo");

            // Update
            var updateDto = new ActualizarComplejoDTO
            {
                Nombre = "Updated Complejo",
                Calle = "T2", Numero = "2", CodigoPostal = "2", Provincia = "P2", Ciudad = "C2"
            };
            var updateResponse = await _fixture.Client.PutAsJsonAsync($"/api/admin/complejos/{created.ComplejoId}", updateDto);
            updateResponse.EnsureSuccessStatusCode();

            // Delete
            var deleteResponse = await _fixture.Client.DeleteAsync($"/api/admin/complejos/{created.ComplejoId}");
            deleteResponse.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task Complejo_NotFound_ReturnsNotFound()
        {
            var token = await _fixture.GetAdminTokenAsync();
            _fixture.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            var getResp = await _fixture.Client.GetAsync("/api/admin/complejos/99999");
            getResp.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
            
            var putResp = await _fixture.Client.PutAsJsonAsync("/api/admin/complejos/99999", new ActualizarComplejoDTO { Nombre = "A", Calle = "B", Numero = "1", CodigoPostal = "1", Provincia = "P", Ciudad = "C" });
            putResp.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
            
            var delResp = await _fixture.Client.DeleteAsync("/api/admin/complejos/99999");
            delResp.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Complejo_Create_BadRequest_ReturnsBadRequest()
        {
            // All "required" properties present (empty strings) so deserialization succeeds;
            // [Required] then rejects the empty Nombre, actually failing ModelState.
            var token = await _fixture.GetAdminTokenAsync();
            _fixture.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var resp = await _fixture.Client.PostAsJsonAsync("/api/admin/complejos", new CrearComplejoDTO
            {
                Nombre = "",
                Calle = "Calle",
                Numero = "1",
                Ciudad = "Ciudad",
                Provincia = "Provincia",
                CodigoPostal = "1000"
            });
            resp.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

            _fixture.Client.DefaultRequestHeaders.Authorization = null;
        }

        [Fact]
        public async Task Complejo_Delete_WithCanchasAsociadas_ReturnsBadRequest()
        {
            // Borrar un Complejo con Canchas asociadas viola la FK y el repositorio
            // relanza InvalidOperationException, que el controller mapea a 400 (no 404/500).
            var token = await _fixture.GetAdminTokenAsync();
            _fixture.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var compResponse = await _fixture.Client.PostAsJsonAsync("/api/admin/complejos",
                new CrearComplejoDTO { Nombre = "Comp Con Cancha", Calle = "Test", Numero = "1", CodigoPostal = "1", Provincia = "P", Ciudad = "C" });
            compResponse.EnsureSuccessStatusCode();
            var comp = await compResponse.Content.ReadFromJsonAsync<ComplejoDTO>();

            var canchaResponse = await _fixture.Client.PostAsJsonAsync("/api/Cancha", new CrearCanchaDTO
            {
                Nombre = "Cancha Bloqueante",
                ComplejoId = comp!.ComplejoId,
                TipoCanchaId = 1,
                TipoSuperficieId = 1
            });
            canchaResponse.EnsureSuccessStatusCode();

            var deleteResponse = await _fixture.Client.DeleteAsync($"/api/admin/complejos/{comp.ComplejoId}");
            deleteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

            _fixture.Client.DefaultRequestHeaders.Authorization = null;
        }
    }
}
