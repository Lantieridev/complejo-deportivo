using complejoDeportivo.DTOs;
using complejoDeportivo.Models;
using System.Threading.Tasks; 
using System.Collections.Generic; // Agregado

namespace complejoDeportivo.Services
{
    public interface IReservaService
    {
        Task<List<DisponibilidadCanchaDTO>> ObtenerTurnosDisponibles(int canchaId, DateOnly fecha);
        Task<ReservaDTO> CrearReserva(CrearReservaDTO dto);
        Task<List<ReservaDTO>> ListarReservasCliente(int clienteId);
        Task<bool> CancelarReserva(CancelarReservaDTO dto);
        Task<List<ComplejoDTO>> ListarComplejos();
        Task<List<CanchaDTO>> ListarCanchasPorComplejo(int complejoId);
        Task<List<HorarioLibreDTO>> ObtenerHorariosDisponiblesCancha(int canchaId, DateOnly fecha);
    }
}