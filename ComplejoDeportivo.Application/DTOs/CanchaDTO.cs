using ComplejoDeportivo.Domain;

namespace ComplejoDeportivo.Application.DTOs
{
    public class CanchaDTO
	{
        public CanchaDTO() { }

        public CanchaDTO(Cancha cancha)
        {
            CanchaId = cancha.CanchaId;
            ComplejoId = cancha.ComplejoId;
            TipoCanchaId = cancha.TipoCanchaId;
            TipoSuperficieId = cancha.TipoSuperficieId;
            Nombre = cancha.Nombre;
            Activa = cancha.Activa;
        }

		public int CanchaId { get; set; }

		public int ComplejoId { get; set; }

		public int TipoCanchaId { get; set; }

		public int TipoSuperficieId { get; set; }

		public string Nombre { get; set; } = string.Empty;

		public bool Activa { get; set; }
	}
}
