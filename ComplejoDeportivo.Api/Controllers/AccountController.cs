using ComplejoDeportivo.Application.DTOs;
using ComplejoDeportivo.Application.Services.Implementations;
using ComplejoDeportivo.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ComplejoDeportivo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("auth")]
    public class AccountController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;

        public AccountController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<UsuarioDTO>> Register([FromBody] RegisterClienteDTO dto) // <--- [FromBody]
        {
            if (!ModelState.IsValid)
            {

                return BadRequest(ModelState);
            }

            try
            {

                var nuevoUsuario = await _usuarioService.RegisterClienteAsync(dto);
                // Devolvemos 200 OK con los datos del usuario creado
                return Ok(nuevoUsuario);
            }
            catch (System.Exception ex)
            {

                // Captura el error "El email ya est� registrado" del servicio
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("register-empleado")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UsuarioDTO>> RegisterEmpleado([FromBody] RegisterClienteDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var nuevoUsuario = await _usuarioService.RegisterEmpleadoAsync(dto);
                return Ok(nuevoUsuario);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
