using System.ComponentModel.DataAnnotations;

namespace ComplejoDeportivo.Application.DTOs
{
    public class CancelarReservaDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "ReservaId debe ser mayor a 0.")]
        public int ReservaId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "ClienteId debe ser mayor a 0.")]
        public int ClienteId { get; set; }
        public string Motivo { get; set; } = "";
    }
}
