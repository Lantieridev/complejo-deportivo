using ComplejoDeportivo.Application.DTOs;

namespace ComplejoDeportivo.Application.Services.Interfaces
{
    public interface IComplejoService
    {
        Task<IEnumerable<ComplejoDetalleDTO>> GetAllAsync(string? searchTerm = null);
        Task<ComplejoDetalleDTO> GetByIdAsync(int id);
        Task<ComplejoDetalleDTO> CreateAsync(CrearComplejoDTO createDto);
        Task UpdateAsync(int id, ActualizarComplejoDTO updateDto);
        Task DeleteAsync(int id);
    }
}