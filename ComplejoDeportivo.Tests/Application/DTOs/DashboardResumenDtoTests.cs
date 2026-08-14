using ComplejoDeportivo.Application.DTOs.Dashboard;
using FluentAssertions;
using Xunit;

namespace ComplejoDeportivo.Tests.Application.DTOs
{
    public class DashboardResumenDtoTests
    {
        [Fact]
        public void VariacionIngresos_WhenAnteriorPositive_ComputesPercentage()
        {
            var dto = new DashboardResumenDto { IngresosMesActual = 150, IngresosMesAnterior = 100 };
            dto.VariacionIngresos.Should().Be(50);
        }

        [Fact]
        public void VariacionIngresos_WhenAnteriorZeroAndActualPositive_Returns100()
        {
            var dto = new DashboardResumenDto { IngresosMesActual = 50, IngresosMesAnterior = 0 };
            dto.VariacionIngresos.Should().Be(100);
        }

        [Fact]
        public void VariacionIngresos_WhenBothZero_ReturnsZero()
        {
            var dto = new DashboardResumenDto { IngresosMesActual = 0, IngresosMesAnterior = 0 };
            dto.VariacionIngresos.Should().Be(0);
        }

        [Fact]
        public void VariacionReservas_WhenAnteriorPositive_ComputesPercentage()
        {
            var dto = new DashboardResumenDto { ReservasMesActual = 20, ReservasMesAnterior = 10 };
            dto.VariacionReservas.Should().Be(100);
        }

        [Fact]
        public void VariacionReservas_WhenAnteriorZeroAndActualPositive_Returns100()
        {
            var dto = new DashboardResumenDto { ReservasMesActual = 5, ReservasMesAnterior = 0 };
            dto.VariacionReservas.Should().Be(100);
        }

        [Fact]
        public void VariacionReservas_WhenBothZero_ReturnsZero()
        {
            var dto = new DashboardResumenDto { ReservasMesActual = 0, ReservasMesAnterior = 0 };
            dto.VariacionReservas.Should().Be(0);
        }

        [Fact]
        public void TendenciaPositiva_ReflectsVariacionSign()
        {
            var positivo = new DashboardResumenDto { IngresosMesActual = 150, IngresosMesAnterior = 100, ReservasMesActual = 20, ReservasMesAnterior = 10 };
            positivo.TendenciaIngresosPositiva.Should().BeTrue();
            positivo.TendenciaReservasPositiva.Should().BeTrue();
            positivo.ObtenerIconoTendenciaIngresos().Should().NotBeNullOrEmpty();
            positivo.ObtenerIconoTendenciaReservas().Should().NotBeNullOrEmpty();

            var negativo = new DashboardResumenDto { IngresosMesActual = 50, IngresosMesAnterior = 100, ReservasMesActual = 5, ReservasMesAnterior = 10 };
            negativo.TendenciaIngresosPositiva.Should().BeFalse();
            negativo.TendenciaReservasPositiva.Should().BeFalse();
            negativo.ObtenerIconoTendenciaIngresos().Should().NotBeNullOrEmpty();
            negativo.ObtenerIconoTendenciaReservas().Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void OcupacionCanchasPorcentaje_WhenTotalesZero_ReturnsZero()
        {
            var dto = new DashboardResumenDto { CanchasActivas = 0, CanchasTotales = 0 };
            dto.OcupacionCanchasPorcentaje.Should().Be(0);
        }

        [Fact]
        public void OcupacionCanchasPorcentaje_ComputesPercentage()
        {
            var dto = new DashboardResumenDto { CanchasActivas = 5, CanchasTotales = 10 };
            dto.OcupacionCanchasPorcentaje.Should().Be(50);
        }

        [Fact]
        public void OcupacionAsadoresPorcentaje_WhenTotalesZero_ReturnsZero()
        {
            var dto = new DashboardResumenDto { AsadoresActivos = 0, AsadoresTotales = 0 };
            dto.OcupacionAsadoresPorcentaje.Should().Be(0);
        }

        [Fact]
        public void OcupacionAsadoresPorcentaje_ComputesPercentage()
        {
            var dto = new DashboardResumenDto { AsadoresActivos = 2, AsadoresTotales = 4 };
            dto.OcupacionAsadoresPorcentaje.Should().Be(50);
        }

        [Theory]
        [InlineData(1, 0, "danger")]
        [InlineData(0, 1, "warning")]
        [InlineData(0, 0, "success")]
        public void ObtenerColorAlertaStock_ReturnsExpected(int critico, int bajo, string esperado)
        {
            var dto = new DashboardResumenDto { ProductosStockCritico = critico, ProductosStockBajo = bajo };
            dto.ObtenerColorAlertaStock().Should().Be(esperado);
        }

        [Fact]
        public void ObtenerTextoResumen_SinActividad_ReturnsMensajeSinActividad()
        {
            var dto = new DashboardResumenDto();
            dto.ObtenerTextoResumen().Should().Be("Sin actividad hoy");
        }

        [Fact]
        public void ObtenerTextoResumen_ConActividad_IncludesEachPoint()
        {
            var dto = new DashboardResumenDto
            {
                ReservasHoy = 3,
                IngresosHoy = 5000,
                AlertasStock = 2,
                ReservasPendientes = 1
            };

            var texto = dto.ObtenerTextoResumen();

            texto.Should().Contain("3 reservas para hoy");
            texto.Should().Contain("ingresados hoy");
            texto.Should().Contain("alertas de stock");
            texto.Should().Contain("reservas pendientes");
        }

        [Fact]
        public void CrearVacio_InitializesEverythingToZero()
        {
            var dto = DashboardResumenDto.CrearVacio(1);

            dto.ReservasHoy.Should().Be(0);
            dto.IngresosHoy.Should().Be(0);
            dto.CanchasTotales.Should().Be(0);
            dto.TotalClientesRegistrados.Should().Be(0);
        }
    }
}
