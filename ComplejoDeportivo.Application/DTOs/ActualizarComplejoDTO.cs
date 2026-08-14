using System.ComponentModel.DataAnnotations;

namespace ComplejoDeportivo.Application.DTOs
{
    public class ActualizarComplejoDTO
    {
        [Required]
        [StringLength(100)]
        public required string Nombre { get; set; }

        [Required]
        [StringLength(150)]
        public required string Calle { get; set; }

        [Required]
        [StringLength(10)]
        public required string Numero { get; set; }

        [Required]
        [StringLength(50)]
        public required string Ciudad { get; set; }

        [Required]
        [StringLength(50)]
        public required string Provincia { get; set; }

        [Required]
        [StringLength(10)]
        public required string CodigoPostal { get; set; }
    }
}