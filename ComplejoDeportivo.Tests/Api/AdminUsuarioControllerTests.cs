using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ComplejoDeportivo.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace ComplejoDeportivo.Tests.Api
{
    [Collection("ApiTestCollection")]
    public class AdminUsuarioControllerTests
    {
        private readonly ApiTestFixture _fixture;

        public AdminUsuarioControllerTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Crud_AdminUsuario_Flow_ShouldSucceed()
        {
            var token = await _fixture.GetAdminTokenAsync();
            _fixture.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // GetAll
            var getAllResponse = await _fixture.Client.GetAsync("/api/admin/usuarios");
            getAllResponse.EnsureSuccessStatusCode();
            var all = await getAllResponse.Content.ReadFromJsonAsync<List<UsuarioDTO>>();
            all.Should().NotBeNull();
            
            // Assuming there is at least one (admin)
            var adminUser = all!.First();

            // GetById
            var getByIdResponse = await _fixture.Client.GetAsync($"/api/admin/usuarios/{adminUser.UsuarioId}");
            getByIdResponse.EnsureSuccessStatusCode();
            var single = await getByIdResponse.Content.ReadFromJsonAsync<UsuarioDTO>();
            single!.Email.Should().Be(adminUser.Email);

            var updateDto = new UsuarioDTO
            {
                UsuarioId = adminUser.UsuarioId,
                Email = "updated@test.com",
                TipoUsuario = adminUser.TipoUsuario,
                ClienteId = adminUser.ClienteId,
                EmpleadoId = adminUser.EmpleadoId
            };
            var updateResponse = await _fixture.Client.PutAsJsonAsync($"/api/admin/usuarios/{adminUser.UsuarioId}", updateDto);
            updateResponse.EnsureSuccessStatusCode();

            // Verify Update
            var getUpdatedResponse = await _fixture.Client.GetAsync($"/api/admin/usuarios/{adminUser.UsuarioId}");
            var updatedSingle = await getUpdatedResponse.Content.ReadFromJsonAsync<UsuarioDTO>();
            updatedSingle!.Email.Should().Be("updated@test.com");

            // Delete
            var deleteResponse = await _fixture.Client.DeleteAsync($"/api/admin/usuarios/{adminUser.UsuarioId}");
            deleteResponse.EnsureSuccessStatusCode();

            _fixture.Client.DefaultRequestHeaders.Authorization = null;
        }

        [Fact]
        public async Task Create_ValidAdministrador_ReturnsCreated()
        {
            var token = await _fixture.GetAdminTokenAsync();
            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, "/api/admin/usuarios")
            {
                Content = JsonContent.Create(new CreateUsuarioDTO
                {
                    Email = $"nuevo-admin-{System.Guid.NewGuid():N}@test.com",
                    Password = "Password123!",
                    TipoUsuario = "Administrador",
                    ClienteId = null,
                    EmpleadoId = null
                })
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        }

        [Fact]
        public async Task Create_InvalidModel_ReturnsBadRequest()
        {
            // All "required" (C#) properties present so deserialization succeeds; Email is
            // malformed (present but invalid) so [EmailAddress] actually fails ModelState,
            // instead of the request dying at JSON deserialization before reaching the action.
            var token = await _fixture.GetAdminTokenAsync();
            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, "/api/admin/usuarios")
            {
                Content = JsonContent.Create(new CreateUsuarioDTO
                {
                    Email = "not-an-email",
                    Password = "Password123!",
                    TipoUsuario = "Administrador",
                    ClienteId = null,
                    EmpleadoId = null
                })
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Update_IdMismatch_ReturnsBadRequest()
        {
            var token = await _fixture.GetAdminTokenAsync();
            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Put, "/api/admin/usuarios/1")
            {
                Content = JsonContent.Create(new UsuarioDTO
                {
                    UsuarioId = 999,
                    Email = "mismatch@test.com",
                    TipoUsuario = "Administrador"
                })
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }
    }
}
