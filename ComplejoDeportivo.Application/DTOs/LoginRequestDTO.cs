using System.ComponentModel.DataAnnotations;

namespace ComplejoDeportivo.Application.DTOs
{
    public class LoginRequestDTO
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string Password { get; set; } // Usamos 'Password', no 'Contraseña'
    }
}