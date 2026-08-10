using System;

namespace ComplejoDeportivo.Application.DTOs
{
    public class HorarioOcupadoDTO
    {
        public int CanchaId { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
    }
}
