using System.ComponentModel.DataAnnotations;

namespace ComplejoDeportivo.Application.DTOs
{
    // DTO para que un invitado se registre.
    // Solo puede registrarse como Cliente.
    public class RegisterClienteDTO
    {

        [EmailAddress]
        [StringLength(150)]
        public required string Email { get; set; }

        [MinLength(6)]
        public required string Password { get; set; }

        [StringLength(100)]
        public required string Nombre { get; set; }

        [StringLength(100)]
        public required string Apellido { get; set; }

        [StringLength(20)]
        public required string Telefono { get; set; }

        [StringLength(20)]
        public required string Documento { get; set; }
    }
}