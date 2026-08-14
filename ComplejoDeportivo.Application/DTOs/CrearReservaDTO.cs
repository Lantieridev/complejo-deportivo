using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ComplejoDeportivo.Application.DTOs
{
    public class CrearReservaDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "ClienteId debe ser mayor a 0.")]
        public int ClienteId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Debe seleccionar al menos una cancha.")]
        public List<int>? CanchaIds { get; set; } = new List<int>();
        public DateOnly Fecha { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
        public string Ambito { get; set; } = "Web";
    }
}