using Microsoft.Extensions.Configuration;
using ComplejoDeportivo.Application.DTOs;
using ComplejoDeportivo.Domain;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using ComplejoDeportivo.Application.Repositories;
using ComplejoDeportivo.Application.Services.Interfaces;

namespace ComplejoDeportivo.Application.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IUsuarioRepository usuarioRepository, IConfiguration configuration)
        {
            _usuarioRepository = usuarioRepository;
            _configuration = configuration;
        }

        public async Task<LoginResponseDTO> LoginAsync(LoginRequestDTO loginRequest)
        {
            var usuario = await _usuarioRepository.GetByEmailAsync(loginRequest.Email);

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, usuario.PasswordHash))
            {
                 throw new UnauthorizedAccessException("Credenciales inválidas.");
            }
            
            var token = GenerateJwtToken(usuario); 
            var rol = GetUserRole(usuario);

            return new LoginResponseDTO
            {
                Email = usuario.Email,
                Token = token,
                Rol = rol,
                ClienteId = usuario.ClienteId
            };
        }

        private string GetUserRole(Usuario usuario)
        {
            if (usuario.TipoUsuario == "Empleado" && usuario.Empleado != null)
            {
                return usuario.Empleado.Cargo;
            }
            return usuario.TipoUsuario;
        }

        private string GenerateJwtToken(Usuario usuario)
        {
            var jwtKey = _configuration["Jwt:Key"]// Asegurarse de que la clave no sea nula
				?? throw new InvalidOperationException("La clave JWT no está configurada.");
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var rol = GetUserRole(usuario);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.UsuarioId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(ClaimTypes.Role, rol)
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                // UTC, not server-local time: JWT validation middleware (and any client checking
                // "exp") compares against UTC, so a server running in a non-UTC timezone would have
                // silently issued tokens with the wrong effective lifetime.
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}