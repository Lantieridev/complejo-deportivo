using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ComplejoDeportivo.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace ComplejoDeportivo.Tests.Api
{
    [Collection("ApiTestCollection")]
    public class ClienteControllerTests
    {
        private readonly ApiTestFixture _fixture;

        public ClienteControllerTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        private async Task<(string token, int clienteId)> RegisterClientAsync(string email)
        {
            var unique = System.Guid.NewGuid().ToString("N")[..15];
            var registerDto = new RegisterClienteDTO
            {
                Email = email,
                Password = "Password123!",
                Nombre = "Test",
                Apellido = "Cliente",
                Telefono = unique[..10],
                Documento = unique
            };
            var registerResponse = await _fixture.Client.PostAsJsonAsync("/api/account/register", registerDto);
            registerResponse.EnsureSuccessStatusCode();

            var loginDto = new LoginRequestDTO { Email = email, Password = "Password123!" };
            var loginResponse = await _fixture.Client.PostAsJsonAsync("/api/auth/login", loginDto);
            loginResponse.EnsureSuccessStatusCode();
            var result = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();
            return (result!.Token, result.ClienteId!.Value);
        }

        [Fact]
        public async Task GetById_AsOwningClient_ReturnsOk()
        {
            var (token, clienteId) = await RegisterClientAsync("cliente-self@test.com");

            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/clientes/{clienteId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetById_AsDifferentClient_ReturnsForbidden()
        {
            var (_, otherClienteId) = await RegisterClientAsync("cliente-other@test.com");
            var (token, _) = await RegisterClientAsync("cliente-requester@test.com");

            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/clientes/{otherClienteId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Create_AsAdmin_InvalidModel_ReturnsBadRequest()
        {
            // "required" properties present as empty strings so deserialization succeeds;
            // [Required] then rejects the empty Nombre, actually failing ModelState.
            var token = await _fixture.GetAdminTokenAsync();
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/clientes")
            {
                Content = JsonContent.Create(new CrearClienteDTO { Nombre = "", Apellido = "" })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Update_AsOwningClient_ValidData_ReturnsNoContent()
        {
            var (token, clienteId) = await RegisterClientAsync("cliente-update-self@test.com");

            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/clientes/{clienteId}")
            {
                Content = JsonContent.Create(new ActualizarClienteDTO { Nombre = "Actualizado", Apellido = "Cliente", Telefono = "999", Email = "cliente-update-self@test.com", Documento = "d1" })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Create_AsAdmin_DuplicateEmail_ReturnsBadRequest()
        {
            // ClienteService.CreateAsync throws a generic Exception (not caught elsewhere)
            // when the email is already registered; controller maps it to 400.
            var email = $"cliente-create-dup-{System.Guid.NewGuid():N}@test.com";
            await RegisterClientAsync(email);
            var token = await _fixture.GetAdminTokenAsync();

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/clientes")
            {
                Content = JsonContent.Create(new CrearClienteDTO { Nombre = "Dup", Apellido = "Test", Email = email })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Update_AsAdmin_InvalidModel_ReturnsBadRequest()
        {
            // Nombre present but empty, so [Required] fails ModelState after deserialization succeeds.
            var (_, clienteId) = await RegisterClientAsync("cliente-update-invalid@test.com");
            var token = await _fixture.GetAdminTokenAsync();

            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/clientes/{clienteId}")
            {
                Content = JsonContent.Create(new ActualizarClienteDTO { Nombre = "", Apellido = "Y" })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Update_AsAdmin_DuplicateEmail_ReturnsBadRequest()
        {
            // ClienteService.UpdateAsync throws a generic Exception (not NotFoundException)
            // when the new email belongs to a different cliente; controller maps it to 400.
            var emailA = $"cliente-dup-a-{System.Guid.NewGuid():N}@test.com";
            await RegisterClientAsync(emailA);
            var (_, clienteBId) = await RegisterClientAsync($"cliente-dup-b-{System.Guid.NewGuid():N}@test.com");
            var token = await _fixture.GetAdminTokenAsync();

            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/clientes/{clienteBId}")
            {
                Content = JsonContent.Create(new ActualizarClienteDTO { Nombre = "B", Apellido = "Test", Email = emailA })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Update_AsDifferentClient_ReturnsForbidden()
        {
            var (_, otherClienteId) = await RegisterClientAsync("cliente-update-other@test.com");
            var (token, _) = await RegisterClientAsync("cliente-update-requester@test.com");

            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/clientes/{otherClienteId}")
            {
                Content = JsonContent.Create(new ActualizarClienteDTO { Nombre = "X", Apellido = "Y", Telefono = "1", Email = "z@test.com", Documento = "d2" })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Update_AsAdmin_NotFound_ReturnsNotFound()
        {
            var token = await _fixture.GetAdminTokenAsync();
            using var request = new HttpRequestMessage(HttpMethod.Put, "/api/admin/clientes/999999")
            {
                Content = JsonContent.Create(new ActualizarClienteDTO { Nombre = "X", Apellido = "Y", Telefono = "1", Email = "notfound@test.com", Documento = "d3" })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_AsAdmin_NotFound_ReturnsNotFound()
        {
            var token = await _fixture.GetAdminTokenAsync();
            using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/admin/clientes/999999");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_AsAdmin_ClienteWithUsuarioAsociado_ReturnsBadRequest()
        {
            // Borrar un Cliente que aún tiene un Usuario apuntándole viola la FK
            // Usuario.ClienteId; el repositorio deja que la SqlException burbujee como
            // Exception genérica, y el controller la mapea a 400 (no 404/500).
            var (_, clienteId) = await RegisterClientAsync("cliente-con-usuario@test.com");

            var token = await _fixture.GetAdminTokenAsync();
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/clientes/{clienteId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Crud_Cliente_Flow_ShouldSucceed()
        {
            var token = await _fixture.GetAdminTokenAsync();
            _fixture.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Create
            var createDto = new CrearClienteDTO
            {
                Nombre = "New",
                Apellido = "Client",
                Email = "crudclient@test.com",
                Telefono = "777",
                Documento = "777"
            };
            var createResponse = await _fixture.Client.PostAsJsonAsync("/api/admin/clientes", createDto);
            createResponse.EnsureSuccessStatusCode();
            var createdCliente = await createResponse.Content.ReadFromJsonAsync<ClienteDTO>();
            createdCliente.Should().NotBeNull();
            createdCliente!.ClienteId.Should().BeGreaterThan(0);

            // GetAll
            var getAllResponse = await _fixture.Client.GetAsync("/api/admin/clientes");
            getAllResponse.EnsureSuccessStatusCode();
            var all = await getAllResponse.Content.ReadFromJsonAsync<List<ClienteDTO>>();
            all.Should().Contain(c => c.ClienteId == createdCliente.ClienteId);

            // GetById
            var getByIdResponse = await _fixture.Client.GetAsync($"/api/admin/clientes/{createdCliente.ClienteId}");
            getByIdResponse.EnsureSuccessStatusCode();
            var single = await getByIdResponse.Content.ReadFromJsonAsync<ClienteDTO>();
            single!.Nombre.Should().Be("New");

            // Update
            var updateDto = new ActualizarClienteDTO
            {
                Nombre = "Updated",
                Apellido = "Client",
                Email = "crudclient@test.com",
                Telefono = "7778",
                Documento = "7778"
            };
            var updateResponse = await _fixture.Client.PutAsJsonAsync($"/api/admin/clientes/{createdCliente.ClienteId}", updateDto);
            updateResponse.EnsureSuccessStatusCode();

            // Verify Update
            var getUpdatedResponse = await _fixture.Client.GetAsync($"/api/admin/clientes/{createdCliente.ClienteId}");
            var updatedSingle = await getUpdatedResponse.Content.ReadFromJsonAsync<ClienteDTO>();
            updatedSingle!.Nombre.Should().Be("Updated");

            // Delete
            var deleteResponse = await _fixture.Client.DeleteAsync($"/api/admin/clientes/{createdCliente.ClienteId}");
            deleteResponse.EnsureSuccessStatusCode();
            
            // Delete again should fail
            var deleteAgainResponse = await _fixture.Client.DeleteAsync($"/api/admin/clientes/{createdCliente.ClienteId}");
            deleteAgainResponse.IsSuccessStatusCode.Should().BeFalse();
            
            _fixture.Client.DefaultRequestHeaders.Authorization = null;
        }
    }
}
