using ComplejoDeportivo.Application.DTOs.Dashboard;

namespace ComplejoDeportivo.Application.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardCompletoDto> GetDashboardCompletoAsync(FiltrosDashboardDto filtros);
    }
}