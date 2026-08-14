using ComplejoDeportivo.Application.DTOs.Dashboard;
using ComplejoDeportivo.Application.Services.Implementations;
using ComplejoDeportivo.Infrastructure.Repositories.Dashboard;
using FluentAssertions;
using Moq;
using Xunit;

namespace ComplejoDeportivo.Tests.Application.Services
{
    public class DashboardServiceTests
    {
        private readonly Mock<IDashboardRepository> _dashboardRepoMock;
        private readonly DashboardService _dashboardService;

        public DashboardServiceTests()
        {
            _dashboardRepoMock = new Mock<IDashboardRepository>();
            _dashboardService = new DashboardService(_dashboardRepoMock.Object);
        }

        private void SetupRepositoryMocks()
        {
            _dashboardRepoMock.Setup(repo => repo.GetResumenAsync(It.IsAny<FiltrosDashboardDto>()))
                .ReturnsAsync(new DashboardResumenDto());
            
            _dashboardRepoMock.Setup(repo => repo.GetEstadosReservasAsync(It.IsAny<FiltrosDashboardDto>()))
                .ReturnsAsync(new List<ReservaEstadoDto>());
            
            _dashboardRepoMock.Setup(repo => repo.GetReservasRecientesAsync(It.IsAny<FiltrosDashboardDto>()))
                .ReturnsAsync(new List<ReservaRecienteDto>());
                
            _dashboardRepoMock.Setup(repo => repo.GetClientesFrecuentesAsync(It.IsAny<FiltrosDashboardDto>()))
                .ReturnsAsync(new List<ClienteFrecuenteDto>());
                
            _dashboardRepoMock.Setup(repo => repo.GetCanchasPopularesAsync(It.IsAny<FiltrosDashboardDto>()))
                .ReturnsAsync(new List<CanchaPopularDto>());
                
            _dashboardRepoMock.Setup(repo => repo.GetOcupacionPorHorarioAsync(It.IsAny<FiltrosDashboardDto>()))
                .ReturnsAsync(new List<OcupacionHorarioDto>());
                
            _dashboardRepoMock.Setup(repo => repo.GetAlertasStockAsync(It.IsAny<FiltrosDashboardDto>()))
                .ReturnsAsync(new List<AlertaStockDto>());
        }

        [Fact]
        public async Task GetDashboardCompletoAsync_RangoMayorA31Dias_ShouldUseAgrupacionMensual()
        {
            // Arrange
            SetupRepositoryMocks();
            var ingresos = new List<IngresoPeriodoDto>
            {
                new IngresoPeriodoDto { Fecha = new DateTime(2023, 1, 15) }
            };
            
            _dashboardRepoMock.Setup(repo => repo.GetIngresosPorPeriodoAsync(It.IsAny<FiltrosDashboardDto>(), "mensual"))
                .ReturnsAsync(ingresos);

            var filtros = new FiltrosDashboardDto
            {
                Desde = new DateTime(2023, 1, 1),
                Hasta = new DateTime(2023, 3, 1) // > 31 días
            };

            // Act
            var result = await _dashboardService.GetDashboardCompletoAsync(filtros);

            // Assert
            result.Should().NotBeNull();
            _dashboardRepoMock.Verify(repo => repo.GetIngresosPorPeriodoAsync(filtros, "mensual"), Times.Once);
            result.IngresosPorPeriodo!.First().Periodo.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetDashboardCompletoAsync_RangoMenorOIgualA31Dias_ShouldUseAgrupacionDiario()
        {
            // Arrange
            SetupRepositoryMocks();
            var ingresos = new List<IngresoPeriodoDto>
            {
                new IngresoPeriodoDto { Fecha = new DateTime(2023, 1, 15) }
            };
            
            _dashboardRepoMock.Setup(repo => repo.GetIngresosPorPeriodoAsync(It.IsAny<FiltrosDashboardDto>(), "diario"))
                .ReturnsAsync(ingresos);

            var filtros = new FiltrosDashboardDto
            {
                Desde = new DateTime(2023, 1, 1),
                Hasta = new DateTime(2023, 1, 10) // <= 31 días
            };

            // Act
            var result = await _dashboardService.GetDashboardCompletoAsync(filtros);

            // Assert
            result.Should().NotBeNull();
            _dashboardRepoMock.Verify(repo => repo.GetIngresosPorPeriodoAsync(filtros, "diario"), Times.Once);
            result.IngresosPorPeriodo!.First().Periodo.Should().Be("15/01");
        }
    }
}
