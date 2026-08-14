using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ComplejoDeportivo.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace ComplejoDeportivo.Tests.Api
{
    [Collection("ApiTestCollection")]
    public class TiposCanchaControllerTests
    {
        private readonly ApiTestFixture _fixture;

        public TiposCanchaControllerTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Crud_Tipos_Flow_ShouldSucceed()
        {
            var token = await _fixture.GetAdminTokenAsync();
            _fixture.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Create TipoCancha
            var createTcDto = new CreateTipoCanchaDTO { Nombre = "F11" };
            var createTcResponse = await _fixture.Client.PostAsJsonAsync("/api/TiposCancha", createTcDto);
            createTcResponse.EnsureSuccessStatusCode();
            var tc = await createTcResponse.Content.ReadFromJsonAsync<TipoCanchaDTO>();
            tc.Should().NotBeNull();
            tc!.TipoCanchaID.Should().BeGreaterThan(0);

            // GetAll TipoCancha
            var get1 = await _fixture.Client.GetAsync("/api/TiposCancha");
            get1.EnsureSuccessStatusCode();
            var allTc = await get1.Content.ReadFromJsonAsync<List<TipoCanchaDTO>>();
            allTc.Should().Contain(x => x.TipoCanchaID == tc.TipoCanchaID);

            // GetTipoCancha (by id)
            var getByIdResponse = await _fixture.Client.GetAsync($"/api/TiposCancha/{tc.TipoCanchaID}");
            getByIdResponse.EnsureSuccessStatusCode();
            var single = await getByIdResponse.Content.ReadFromJsonAsync<TipoCanchaDTO>();
            single!.Nombre.Should().Be("F11");
        }
    }
}
