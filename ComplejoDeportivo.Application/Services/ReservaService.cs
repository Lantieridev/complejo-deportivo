using ComplejoDeportivo.Application.DTOs;
using ComplejoDeportivo.Domain;
using ComplejoDeportivo.Application.Repositories;
using System.Threading.Tasks; 
using System.Linq; 
using System.Collections.Generic; 
using System; 

namespace ComplejoDeportivo.Application.Services
{
    public class ReservaService : IReservaService
    {
        private readonly IReservaRepository _repo;
        private readonly TimeOnly _apertura = new TimeOnly(8, 0, 0);
        private readonly TimeOnly _cierre = new TimeOnly(23, 0, 0);

        public ReservaService(IReservaRepository repo) 
        {
            _repo = repo;
        }

        public async Task<List<DisponibilidadCanchaDTO>> ObtenerTurnosDisponibles(int canchaId, DateOnly fecha)
        {
            return await _repo.ObtenerTurnosDisponiblesAsync(canchaId, fecha, _apertura, _cierre);
        }

        public async Task<ReservaDTO> CrearReserva(CrearReservaDTO dto)
        {
            if (dto.CanchaIds == null || !dto.CanchaIds.Any())
                throw new Exception("Debe seleccionar al menos una cancha.");

            var horas = dto.HoraFin - dto.HoraInicio;
            if (horas.TotalHours < 1 || horas.TotalHours % 1 != 0)
                throw new Exception("Las reservas deben ser en bloques de 1 hora.");

            var canchaIdsUnicas = dto.CanchaIds.Distinct().ToList();

            var horariosOcupados = await _repo.ObtenerHorariosOcupadosAsync(canchaIdsUnicas, dto.Fecha);
            var tarifas = await _repo.ObtenerTarifasPorFechaAsync(canchaIdsUnicas, dto.Fecha);

            decimal total = 0;
            var detallesParaCrear = new List<(int CanchaId, Tarifa Tarifa, int CantidadHoras, decimal Subtotal)>();

            foreach (var canchaId in canchaIdsUnicas)
            {
                decimal subtotalCancha = 0;
                var h = dto.HoraInicio;

                var ocupadosCancha = horariosOcupados.Where(o => o.CanchaId == canchaId).ToList();
                var tarifasCancha = tarifas.Where(t => t.CanchaId == canchaId).ToList();

                while (h < dto.HoraFin)
                {
                    var fin = h.AddHours(1);
                    if (ocupadosCancha.Any(o => o.HoraInicio < fin && o.HoraFin > h))
                        throw new Exception($"Horario ocupado en cancha ID {canchaId}: {h} - {fin}");

                    var tarifa = _repo.ObtenerTarifaVigenteEnMemoria(tarifasCancha, canchaId, dto.Fecha, h);
                    subtotalCancha += tarifa.Precio;
                    h = fin;
                }

                var tarifaReferencia = _repo.ObtenerTarifaVigenteEnMemoria(tarifasCancha, canchaId, dto.Fecha, dto.HoraInicio);
                detallesParaCrear.Add((canchaId, tarifaReferencia, (int)horas.TotalHours, subtotalCancha));
                total += subtotalCancha;
            }

            var reserva = new Reserva
            {
                ClienteId = dto.ClienteId,
                Fecha = dto.Fecha,
                HoraInicio = dto.HoraInicio,
                HoraFin = dto.HoraFin,
                EstadoReservaId = 1, // 1 = Confirmada
                Ambito = dto.Ambito,
                FechaCreacion = DateTime.Now,
                Total = total
            };

            var detalles = detallesParaCrear.Select(detalleInfo => new DetalleReserva
            {
                CanchaId = detalleInfo.CanchaId,
                TarifaHoraId = detalleInfo.Tarifa.TarifaId,
                CantidadHoras = detalleInfo.CantidadHoras,
                Descuento = 0,
                Recargo = 0,
                Subtotal = detalleInfo.Subtotal
            }).ToList();

            await _repo.CrearReservaConDetallesAsync(reserva, detalles);

            var detallesDto = new List<DetalleReservaDTO>();
            foreach (var d in detallesParaCrear)
            {
                detallesDto.Add(new DetalleReservaDTO
                {
                    CanchaId = d.CanchaId,
                    NombreCancha = await _repo.ObtenerNombreCanchaAsync(d.CanchaId),
                    Subtotal = d.Subtotal,
                    CantidadHoras = d.CantidadHoras
                });
            }

            return new ReservaDTO
            {
                ReservaId = reserva.ReservaId,
                ClienteId = reserva.ClienteId,
                Fecha = reserva.Fecha,
                HoraInicio = reserva.HoraInicio,
                HoraFin = reserva.HoraFin,
                Total = reserva.Total,
                Estado = "Confirmada", 
                FechaCreacion = reserva.FechaCreacion,
                Detalles = detallesDto
            };
        }

        public async Task<List<ReservaDTO>> ListarReservasCliente(int clienteId)
        {
            var reservas = await _repo.ObtenerReservasPorCliente(clienteId); 
            
            return reservas.Select(r => new ReservaDTO
            {
                ReservaId = r.ReservaId,
                ClienteId = r.ClienteId,
                Fecha = r.Fecha,
                HoraInicio = r.HoraInicio,
                HoraFin = r.HoraFin,
                Total = r.Total,
                Estado = r.EstadoReserva?.Nombre ?? "Desconocido", 
                FechaCreacion = r.FechaCreacion,
                Detalles = r.DetalleReservas.Select(d => new DetalleReservaDTO 
                {
                    DetalleReservaId = d.DetalleReservaId,
                    CanchaId = d.CanchaId,
                    NombreCancha = d.Cancha?.Nombre ?? "N/A",
                    Subtotal = d.Subtotal,
                    CantidadHoras = d.CantidadHoras
                }).ToList()
            }).ToList();
        }

        public async Task<bool> CancelarReserva(CancelarReservaDTO dto)
        {
            var reserva = await _repo.ObtenerReservaPorIdAsync(dto.ReservaId);
            if (reserva == null || reserva.ClienteId != dto.ClienteId) return false;

            if (reserva.EstadoReservaId != 1) 
            {
                throw new Exception("La reserva no se puede cancelar porque no está 'Confirmada'.");
            }

            var tiempoRestante = reserva.Fecha.ToDateTime(reserva.HoraInicio) - DateTime.Now;
            if (tiempoRestante.TotalHours < 24)
                throw new Exception("La reserva solo se puede cancelar con 24 horas de anticipación.");

            reserva.EstadoReservaId = 2; // 2 = Cancelada
            await _repo.GuardarAsync(); 
            return true;
        }

        public async Task<List<ComplejoDTO>> ListarComplejos()
        {
            return await _repo.ObtenerComplejosAsync();
        }

        public async Task<List<CanchaDTO>> ListarCanchasPorComplejo(int complejoId)
        {
            return await _repo.ObtenerCanchasPorComplejoAsync(complejoId);
        }

        public async Task<List<HorarioLibreDTO>> ObtenerHorariosDisponiblesCancha(int canchaId, DateOnly fecha)
        {
            return await _repo.ObtenerHorariosDisponiblesCanchaAsync(canchaId, fecha, _apertura, _cierre);
        }
    }
}