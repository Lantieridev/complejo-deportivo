using System.ComponentModel.DataAnnotations;

namespace ComplejoDeportivo.Application.DTOs
{
    public class CreateUsuarioDTO
    {
        [EmailAddress]
        public required string Email { get; set; }

        [MinLength(6)]
        public required string Password { get; set; }
        public required string TipoUsuario { get; set; }
        public int? ClienteId { get; set; }
        public int? EmpleadoId { get; set; }
    }
}