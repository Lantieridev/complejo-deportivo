using complejoDeportivo.DTOs;
using complejoDeportivo.Models;
using Microsoft.EntityFrameworkCore; 
using System.Threading.Tasks; 
using System.Collections.Generic; // Agregado
using System.Linq; // Agregado
using System; // Agregado

namespace complejoDeportivo.Repositories
{
    public class ReservaRepository : IReservaRepository
    {
        private readonly ComplejoDeportivoContext _contexto;
        private static readonly TimeOnly _horaLuz = new TimeOnly(19, 0, 0); // Hora a la que entra la tarifa nocturna

        public ReservaRepository(ComplejoDeportivoContext contexto)
        {
            _contexto = contexto;
        }

        public async Task<List<DisponibilidadCanchaDTO>> ObtenerTurnosDisponiblesAsync(int canchaId, DateOnly fecha, TimeOnly apertura, TimeOnly cierre)
        {
            var turnos = new List<DisponibilidadCanchaDTO>();
            var hora = apertura;

            while (hora.AddHours(1) <= cierre)
            {
                var horaFin = hora.AddHours(1);

                if (!await ExisteReservaSuperpuestaAsync(canchaId, fecha, hora, horaFin)
                    && !await ExisteBloqueoAsync(canchaId, fecha, hora, horaFin))
                {
                    turnos.Add(new DisponibilidadCanchaDTO
                    {
                        CanchaId = canchaId,
                        Fecha = fecha,
                        HoraInicio = hora,
                        HoraFin = horaFin
                    });
                }

                hora = hora.AddHours(1);
            }

            return turnos;
        }

        public async Task<bool> ExisteReservaSuperpuestaAsync(int canchaId, DateOnly fecha, TimeOnly inicio, TimeOnly fin)
        {
            return await (from r in _contexto.Reservas
                    join d in _contexto.DetalleReservas on r.ReservaId equals d.ReservaId
                    where d.CanchaId == canchaId
                    && r.Fecha == fecha
                    && r.HoraInicio < fin
                    && r.HoraFin > inicio
                    select r).AnyAsync();
        }

        public async Task<bool> ExisteBloqueoAsync(int canchaId, DateOnly fecha, TimeOnly inicio, TimeOnly fin)
        {
            return await _contexto.BloqueoCanchas
                .AnyAsync(b => b.CanchaId == canchaId
                    && b.Fecha == fecha
                    && b.HoraInicio < fin
                    && b.HoraFin > inicio);
        }

        public async Task<List<HorarioOcupadoDTO>> ObtenerHorariosOcupadosAsync(List<int> canchaIds, DateOnly fecha)
        {
            var reservas = from r in _contexto.Reservas
                           from d in r.DetalleReservas
                           where r.Fecha == fecha && canchaIds.Contains(d.CanchaId)
                           select new HorarioOcupadoDTO
                           {
                               CanchaId = d.CanchaId,
                               HoraInicio = r.HoraInicio,
                               HoraFin = r.HoraFin
                           };

            var bloqueos = from b in _contexto.BloqueoCanchas
                           where b.Fecha == fecha && canchaIds.Contains(b.CanchaId)
                           select new HorarioOcupadoDTO
                           {
                               CanchaId = b.CanchaId,
                               HoraInicio = b.HoraInicio,
                               HoraFin = b.HoraFin
                           };

            return await reservas.Concat(bloqueos).ToListAsync();
        }

        public async Task<List<Tarifa>> ObtenerTarifasPorFechaAsync(List<int> canchaIds, DateOnly fecha)
        {
            return await _contexto.Tarifas
                .Where(t => canchaIds.Contains(t.CanchaId) && t.FechaVigencia <= fecha)
                .ToListAsync();
        }

        public static Tarifa ObtenerTarifaVigenteEnMemoria(IEnumerable<Tarifa> tarifas, int canchaId, DateOnly fecha, TimeOnly hora)
        {
            bool requiereLuz = hora >= _horaLuz;

            var tarifa = tarifas
                .Where(t => t.CanchaId == canchaId 
                            && t.EsActual 
                            && t.ContratoLuz == requiereLuz 
                            && t.FechaVigencia <= fecha)
                .OrderByDescending(t => t.FechaVigencia)
                .FirstOrDefault();

            if (tarifa == null && requiereLuz)
            {
                tarifa = tarifas
                    .Where(t => t.CanchaId == canchaId 
                                && t.EsActual 
                                && t.ContratoLuz == false 
                                && t.FechaVigencia <= fecha)
                    .OrderByDescending(t => t.FechaVigencia)
                    .FirstOrDefault();
            }

            if (tarifa == null)
            {
                var fallback = tarifas
                    .Where(t => t.CanchaId == canchaId && t.FechaVigencia <= fecha)
                    .OrderByDescending(t => t.FechaVigencia)
                    .FirstOrDefault();
                if (fallback != null) return fallback;
            }

            if (tarifa != null) return tarifa;

            throw new InvalidOperationException($"No se encontró tarifa vigente para la cancha {canchaId} en la fecha {fecha} a las {hora}.");
        }

        public async Task<Tarifa> ObtenerTarifaVigenteAsync(int canchaId, DateOnly fecha, TimeOnly hora)
        {
            bool requiereLuz = hora >= _horaLuz;

            var tarifa = await _contexto.Tarifas
                .Where(t => t.CanchaId == canchaId
                            && t.EsActual
                            && t.ContratoLuz == requiereLuz
                            && t.FechaVigencia <= fecha)
                .OrderByDescending(t => t.FechaVigencia)
                .FirstOrDefaultAsync();

            if (tarifa == null && requiereLuz)
            {
                tarifa = await _contexto.Tarifas
                    .Where(t => t.CanchaId == canchaId
                                && t.EsActual
                                && t.ContratoLuz == false
                                && t.FechaVigencia <= fecha)
                    .OrderByDescending(t => t.FechaVigencia)
                    .FirstOrDefaultAsync();
            }

            if (tarifa == null)
            {
			    var fallback = await _contexto.Tarifas
				    .Where(t => t.CanchaId == canchaId && t.FechaVigencia <= fecha)
				    .OrderByDescending(t => t.FechaVigencia)
				    .FirstOrDefaultAsync();
                if (fallback != null) return fallback;
            }

			if (tarifa != null) return tarifa;

			throw new InvalidOperationException($"No se encontró tarifa vigente para la cancha {canchaId} en la fecha {fecha} a las {hora}.");
        }

        public async Task<Reserva> ObtenerReservaPorIdAsync(int reservaId)
        {
			var reserva = await _contexto.Reservas.FirstOrDefaultAsync(r => r.ReservaId == reservaId);
			if (reserva == null)
				throw new InvalidOperationException($"No se encontró reserva con id {reservaId}.");
			return reserva;
        }

        public async Task<List<Reserva>> ObtenerReservasPorCliente(int clienteId)
        {
            return await _contexto.Reservas
                .Where(r => r.ClienteId == clienteId)
                .Include(r => r.EstadoReserva) 
                .Include(r => r.DetalleReservas) 
                    .ThenInclude(d => d.Cancha) 
                .OrderByDescending(r => r.Fecha)
                .ToListAsync(); 
        }

        public void AgregarReserva(Reserva reserva)
        {
            _contexto.Reservas.Add(reserva);
        }

        public void AgregarDetalle(DetalleReserva detalle)
        {
            _contexto.DetalleReservas.Add(detalle);
        }

        public async Task<Reserva> CrearReservaConDetallesAsync(Reserva reserva, List<DetalleReserva> detalles)
        {
            using var transaction = await _contexto.Database.BeginTransactionAsync();
            try
            {
                _contexto.Reservas.Add(reserva);
                await _contexto.SaveChangesAsync();

                foreach (var detalle in detalles)
                {
                    detalle.ReservaId = reserva.ReservaId;
                    _contexto.DetalleReservas.Add(detalle);
                }

                await _contexto.SaveChangesAsync();
                await transaction.CommitAsync();
                return reserva;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<string> ObtenerNombreCanchaAsync(int canchaId)
        {
            var cancha = await _contexto.Canchas.FindAsync(canchaId);
            return cancha?.Nombre ?? "N/A";
        }
        public async Task<List<ComplejoDTO>> ObtenerComplejosAsync()
        {
            return await _contexto.Complejos
                .Select(c => new ComplejoDTO
                {
                    ComplejoId = c.ComplejoId,
                    Nombre = c.Nombre
                }).ToListAsync();
        }

        public async Task<List<CanchaDTO>> ObtenerCanchasPorComplejoAsync(int complejoId)
        {
            return await _contexto.Canchas
                .Where(c => c.ComplejoId == complejoId)
                .Select(c => new CanchaDTO(c)
                {
                    CanchaId = c.CanchaId,
                    Nombre = c.Nombre
                }).ToListAsync();
        }
        public async Task<List<HorarioLibreDTO>> ObtenerHorariosDisponiblesCanchaAsync(int canchaId, DateOnly fecha, TimeOnly apertura, TimeOnly cierre)
        {
            List<HorarioLibreDTO> resultado = new List<HorarioLibreDTO>();

            for (TimeOnly hora = apertura; hora < cierre; hora = hora.AddHours(1))
            {
                TimeOnly siguiente = hora.Add(TimeSpan.FromHours(1));

                bool ocupadoPorReserva = await _contexto.Reservas
                    .AnyAsync(r => r.DetalleReservas.Any(d => d.CanchaId == canchaId) && r.Fecha == fecha &&
                         hora < r.HoraFin && siguiente > r.HoraInicio);

                if (!ocupadoPorReserva)
                {
                    resultado.Add(new HorarioLibreDTO
                    {
                        HoraInicio = hora,
                        HoraFin = siguiente
                    });
                }
            }

            return resultado;
        }

        public async Task GuardarAsync()
        {
            await _contexto.SaveChangesAsync();
        }
    }
}