using ComplejoDeportivo.Application.DTOs;
using ComplejoDeportivo.Domain;
using System.Threading.Tasks; 
using System.Collections.Generic; // Agregado

namespace ComplejoDeportivo.Application.Repositories
{
    public interface IReservaRepository
    {
        Task<Reserva?> ObtenerReservaPorIdAsync(int reservaId);
        Task<List<Reserva>> ObtenerReservasPorCliente(int clienteId);
        void AgregarReserva(Reserva reserva);
        void AgregarDetalle(DetalleReserva detalle);
        Task<Reserva> CrearReservaConDetallesAsync(Reserva reserva, List<DetalleReserva> detalles);
        Task<string> ObtenerNombreCanchaAsync(int canchaId);
        Task<bool> ExisteReservaSuperpuestaAsync(int canchaId, DateOnly fecha, TimeOnly inicio, TimeOnly fin);
        Task<bool> ExisteBloqueoAsync(int canchaId, DateOnly fecha, TimeOnly inicio, TimeOnly fin);
        Task<Tarifa> ObtenerTarifaVigenteAsync(int canchaId, DateOnly fecha, TimeOnly hora);
        Task<List<HorarioOcupadoDTO>> ObtenerHorariosOcupadosAsync(List<int> canchaIds, DateOnly fecha);
        Task<List<Tarifa>> ObtenerTarifasPorFechaAsync(List<int> canchaIds, DateOnly fecha);
        Task<List<ComplejoDTO>> ObtenerComplejosAsync();
        Task<List<CanchaDTO>> ObtenerCanchasPorComplejoAsync(int complejoId);
        Task<List<HorarioLibreDTO>> ObtenerHorariosDisponiblesCanchaAsync(int canchaId, DateOnly fecha, TimeOnly apertura, TimeOnly cierre);
        Task GuardarAsync();
        Tarifa ObtenerTarifaVigenteEnMemoria(IEnumerable<Tarifa> tarifas, int canchaId, DateOnly fecha, TimeOnly hora);
        Task<List<DisponibilidadCanchaDTO>> ObtenerTurnosDisponiblesAsync(int canchaId, DateOnly date, TimeOnly apertura, TimeOnly cierre);
    }
}