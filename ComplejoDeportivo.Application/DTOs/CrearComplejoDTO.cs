using System.ComponentModel.DataAnnotations;

namespace ComplejoDeportivo.Application.DTOs
{
    public class CrearComplejoDTO
    {
        [Required]
        public required string Nombre { get; set; }
        
        [Required]
        public required string Calle { get; set; }
        
        [Required]
        public required string Numero { get; set; }
        
        [Required]
        public required string Ciudad { get; set; }
        
        [Required]
        public required string Provincia { get; set; }
        
        [Required]
        public required string CodigoPostal { get; set; }
    }
}