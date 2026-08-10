using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ComplejoDeportivo.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace ComplejoDeportivo.Tests.Api
{
    [Collection("ApiTestCollection")]
    public class EmpleadoControllerTests
    {
        private readonly ApiTestFixture _fixture;

        public EmpleadoControllerTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Crud_Empleado_Flow_ShouldSucceed()
        {
            var token = await _fixture.GetAdminTokenAsync();
            _fixture.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Create
            var createDto = new CrearEmpleadoDTO
            {
                Nombre = "New",
                Apellido = "Empleado",
                Email = "emp@test.com",
                Cargo = "Staff"
            };
            var createResponse = await _fixture.Client.PostAsJsonAsync("/api/admin/empleados", createDto);
            createResponse.EnsureSuccessStatusCode();
            var created = await createResponse.Content.ReadFromJsonAsync<EmpleadoDTO>();
            created.Should().NotBeNull();
            created!.EmpleadoId.Should().BeGreaterThan(0);

            // GetAll
            var getAllResponse = await _fixture.Client.GetAsync("/api/admin/empleados");
            getAllResponse.EnsureSuccessStatusCode();
            var all = await getAllResponse.Content.ReadFromJsonAsync<List<EmpleadoDTO>>();
            all.Should().Contain(e => e.EmpleadoId == created.EmpleadoId);

            // GetById
            var getByIdResponse = await _fixture.Client.GetAsync($"/api/admin/empleados/{created.EmpleadoId}");
            getByIdResponse.EnsureSuccessStatusCode();
            var single = await getByIdResponse.Content.ReadFromJsonAsync<EmpleadoDTO>();
            single!.Nombre.Should().Be("New");

            // Update
            var updateDto = new ActualizarEmpleadoDTO
            {
                Nombre = "Updated",
                Apellido = "Empleado",
                Cargo = "Manager"
            };
            var updateResponse = await _fixture.Client.PutAsJsonAsync($"/api/admin/empleados/{created.EmpleadoId}", updateDto);
            updateResponse.EnsureSuccessStatusCode();

            // Verify
            var getUpdatedResponse = await _fixture.Client.GetAsync($"/api/admin/empleados/{created.EmpleadoId}");
            var updatedSingle = await getUpdatedResponse.Content.ReadFromJsonAsync<EmpleadoDTO>();
            updatedSingle!.Nombre.Should().Be("Updated");

            // Delete
            var deleteResponse = await _fixture.Client.DeleteAsync($"/api/admin/empleados/{created.EmpleadoId}");
            deleteResponse.EnsureSuccessStatusCode();

            _fixture.Client.DefaultRequestHeaders.Authorization = null;
        }

        [Fact]
        public async Task Create_InvalidModel_ReturnsBadRequest()
        {
            // "required" properties present as empty strings so deserialization succeeds;
            // [Required] then rejects the empty Cargo, actually failing ModelState.
            var token = await _fixture.GetAdminTokenAsync();
            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, "/api/admin/empleados")
            {
                Content = JsonContent.Create(new CrearEmpleadoDTO { Nombre = "X", Apellido = "Y", Cargo = "" })
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Update_NotFound_ReturnsNotFound()
        {
            var token = await _fixture.GetAdminTokenAsync();
            var updateDto = new ActualizarEmpleadoDTO { Nombre = "X", Apellido = "Y", Cargo = "Z" };
            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Put, "/api/admin/empleados/999999")
            {
                Content = JsonContent.Create(updateDto)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_DuplicateEmail_ReturnsBadRequest()
        {
            // EmpleadoService.CreateAsync throws a generic Exception when the email is already
            // registered by another empleado; controller maps it to 400.
            var token = await _fixture.GetAdminTokenAsync();
            var email = $"emp-create-dup-{System.Guid.NewGuid():N}@test.com";

            using var firstRequest = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, "/api/admin/empleados")
            {
                Content = JsonContent.Create(new CrearEmpleadoDTO { Nombre = "First", Apellido = "Test", Email = email, Cargo = "Staff" })
            };
            firstRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var first = await _fixture.Client.SendAsync(firstRequest);
            first.EnsureSuccessStatusCode();

            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, "/api/admin/empleados")
            {
                Content = JsonContent.Create(new CrearEmpleadoDTO { Nombre = "Second", Apellido = "Test", Email = email, Cargo = "Staff" })
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Update_DuplicateEmail_ReturnsBadRequest()
        {
            // EmpleadoService.UpdateAsync throws a generic Exception (not NotFoundException)
            // when the new email belongs to a different empleado; controller maps it to 400.
            var token = await _fixture.GetAdminTokenAsync();
            _fixture.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var uniqueA = System.Guid.NewGuid().ToString("N")[..10];
            var uniqueB = System.Guid.NewGuid().ToString("N")[..10];

            var empA = await _fixture.Client.PostAsJsonAsync("/api/admin/empleados", new CrearEmpleadoDTO
            {
                Nombre = "EmpA", Apellido = "Test", Email = $"empa-{uniqueA}@test.com", Cargo = "Staff"
            });
            empA.EnsureSuccessStatusCode();
            var createdA = await empA.Content.ReadFromJsonAsync<EmpleadoDTO>();

            var empB = await _fixture.Client.PostAsJsonAsync("/api/admin/empleados", new CrearEmpleadoDTO
            {
                Nombre = "EmpB", Apellido = "Test", Email = $"empb-{uniqueB}@test.com", Cargo = "Staff"
            });
            empB.EnsureSuccessStatusCode();
            var createdB = await empB.Content.ReadFromJsonAsync<EmpleadoDTO>();

            var updateResponse = await _fixture.Client.PutAsJsonAsync($"/api/admin/empleados/{createdB!.EmpleadoId}", new ActualizarEmpleadoDTO
            {
                Nombre = "EmpB", Apellido = "Test", Email = createdA!.Email, Cargo = "Staff"
            });

            updateResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

            _fixture.Client.DefaultRequestHeaders.Authorization = null;
        }

        [Fact]
        public async Task Delete_EmpleadoWithUsuarioAsociado_ReturnsBadRequest()
        {
            // Borrar un Empleado que aún tiene un Usuario apuntándole viola la FK
            // Usuario.EmpleadoId; el repositorio deja que la SqlException burbujee como
            // Exception genérica, y el controller la mapea a 400 (no 404/500).
            var registerDto = new RegisterClienteDTO
            {
                Email = $"emp-con-usuario-{System.Guid.NewGuid():N}@test.com",
                Password = "Password123!",
                Nombre = "Test",
                Apellido = "Empleado",
                Telefono = System.Guid.NewGuid().ToString("N")[..10],
                Documento = System.Guid.NewGuid().ToString("N")[..10]
            };
            var registerResponse = await _fixture.Client.PostAsJsonAsync("/api/account/register-empleado", registerDto);
            registerResponse.EnsureSuccessStatusCode();
            var usuario = await registerResponse.Content.ReadFromJsonAsync<UsuarioDTO>();

            var token = await _fixture.GetAdminTokenAsync();
            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Delete, $"/api/admin/empleados/{usuario!.EmpleadoId}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Delete_NotFound_ReturnsNotFound()
        {
            var token = await _fixture.GetAdminTokenAsync();
            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Delete, "/api/admin/empleados/999999");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _fixture.Client.SendAsync(request);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }
    }
}
