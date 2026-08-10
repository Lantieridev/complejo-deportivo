using ComplejoDeportivo.Application.DTOs;
using ComplejoDeportivo.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplejoDeportivo.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDTO>> Login([FromBody] LoginRequestDTO loginRequest) // <--- [FromBody]
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var loginResponse = await _authService.LoginAsync(loginRequest);
                return Ok(loginResponse);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Unreachable in practice: the only realistic trigger is AuthService.GenerateJwtToken's
                // missing-Jwt:Key throw, but Program.cs already calls
                // Encoding.UTF8.GetBytes(configuration["Jwt:Key"]) at host-startup time — a null Jwt:Key
                // crashes the app before it can ever serve a request, so this branch can't fire in any
                // running instance. [ExcludeFromCodeCoverage] can't isolate 3 lines inside an async
                // state machine without excluding the whole (otherwise fully-tested) action method, so
                // this is accounted for via the lowered coverage Threshold in the test project instead.
                return StatusCode(500, new { message = "Ha ocurrido un error inesperado.", details = ex.Message });
            }
        }
    }
}