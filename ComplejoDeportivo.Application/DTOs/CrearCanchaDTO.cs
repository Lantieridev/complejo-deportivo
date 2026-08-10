using System.ComponentModel.DataAnnotations;

namespace ComplejoDeportivo.Application.DTOs
{
    public class CrearCanchaDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "ComplejoId debe ser mayor a 0.")]
        public int ComplejoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "TipoCanchaId debe ser mayor a 0.")]
        public int TipoCanchaId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "TipoSuperficieId debe ser mayor a 0.")]
        public int TipoSuperficieId { get; set; }

        [Required(AllowEmptyStrings = false)]
        [StringLength(100)]
        public required string Nombre { get; set; }

    }
}