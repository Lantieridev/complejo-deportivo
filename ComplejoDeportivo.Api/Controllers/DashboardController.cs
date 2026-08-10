using ComplejoDeportivo.Application.DTOs.Dashboard;
using ComplejoDeportivo.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplejoDeportivo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Empleado")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<ActionResult<DashboardCompletoDto>> GetDashboardCompleto([FromQuery] FiltrosDashboardDto filtros)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var dashboardData = await _dashboardService.GetDashboardCompletoAsync(filtros);
            return Ok(dashboardData);
        }
    }
}