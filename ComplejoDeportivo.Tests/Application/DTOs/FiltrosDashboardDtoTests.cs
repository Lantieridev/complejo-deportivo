using System;
using System.ComponentModel.DataAnnotations;
using ComplejoDeportivo.Application.DTOs.Dashboard;
using FluentAssertions;
using Xunit;

namespace ComplejoDeportivo.Tests.Application.DTOs
{
    public class FiltrosDashboardDtoTests
    {
        [Fact]
        public void ValidarRangoFechas_HastaMenorQueDesde_ReturnsError()
        {
            var dto = new FiltrosDashboardDto { ComplejoId = 1, Desde = new DateTime(2025, 1, 10), Hasta = new DateTime(2025, 1, 1) };
            var context = new ValidationContext(dto) { MemberName = nameof(dto.Hasta) };

            var result = FiltrosDashboardDto.ValidarRangoFechas(dto.Hasta, context);

            result.Should().NotBe(ValidationResult.Success);
        }

        [Fact]
        public void ValidarRangoFechas_RangoMayorAUnAnio_ReturnsError()
        {
            var dto = new FiltrosDashboardDto { ComplejoId = 1, Desde = new DateTime(2024, 1, 1), Hasta = new DateTime(2025, 6, 1) };
            var context = new ValidationContext(dto) { MemberName = nameof(dto.Hasta) };

            var result = FiltrosDashboardDto.ValidarRangoFechas(dto.Hasta, context);

            result.Should().NotBe(ValidationResult.Success);
        }

        [Fact]
        public void ValidarRangoFechas_RangoValido_ReturnsSuccess()
        {
            var dto = new FiltrosDashboardDto { ComplejoId = 1, Desde = new DateTime(2025, 1, 1), Hasta = new DateTime(2025, 1, 31) };
            var context = new ValidationContext(dto) { MemberName = nameof(dto.Hasta) };

            var result = FiltrosDashboardDto.ValidarRangoFechas(dto.Hasta, context);

            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void ObtenerRangoFechas_ExplicitDates_ReturnsThem()
        {
            var dto = new FiltrosDashboardDto { ComplejoId = 1, Desde = new DateTime(2025, 1, 1), Hasta = new DateTime(2025, 1, 31) };

            var (inicio, fin) = dto.ObtenerRangoFechas();

            inicio.Should().Be(new DateTime(2025, 1, 1));
            fin.Should().Be(new DateTime(2025, 1, 31));
        }

        [Theory]
        [InlineData("hoy")]
        [InlineData("ayer")]
        [InlineData("esta_semana")]
        [InlineData("semana_pasada")]
        [InlineData("este_mes")]
        [InlineData("mes_pasado")]
        [InlineData("este_anio")]
        [InlineData("anio_pasado")]
        [InlineData("ultimos_7_dias")]
        [InlineData("ultimos_30_dias")]
        [InlineData("ultimos_90_dias")]
        [InlineData("periodo_desconocido")]
        [InlineData(null)]
        public void ObtenerRangoFechas_EveryPeriodoPredefinido_ReturnsARange(string? periodo)
        {
            var dto = new FiltrosDashboardDto { ComplejoId = 1, PeriodoPredefinido = periodo };

            var (inicio, fin) = dto.ObtenerRangoFechas();

            inicio.Should().BeOnOrBefore(fin);
        }

        [Fact]
        public void TieneFiltrosActivos_NoFiltros_ReturnsFalse()
        {
            var dto = new FiltrosDashboardDto { ComplejoId = 1 };
            dto.TieneFiltrosActivos().Should().BeFalse();
        }

        [Fact]
        public void TieneFiltrosActivos_ConFiltroDeFecha_ReturnsTrue()
        {
            var dto = new FiltrosDashboardDto { ComplejoId = 1, Desde = DateTime.Today };
            dto.TieneFiltrosActivos().Should().BeTrue();
        }

        [Fact]
        public void TieneFiltrosActivos_SinFechaPeroConFiltroEspecifico_ReturnsTrue()
        {
            var dto = new FiltrosDashboardDto { ComplejoId = 1, CanchaId = 5 };
            dto.TieneFiltrosActivos().Should().BeTrue();
        }

        [Fact]
        public void TieneFiltrosDeFecha_ConDesde_ReturnsTrue()
        {
            var dto = new FiltrosDashboardDto { ComplejoId = 1, Desde = DateTime.Today };
            dto.TieneFiltrosDeFecha().Should().BeTrue();
        }

        [Fact]
        public void TieneFiltrosEspecificos_ConCanchaId_ReturnsTrue()
        {
            var dto = new FiltrosDashboardDto { ComplejoId = 1, CanchaId = 5 };
            dto.TieneFiltrosEspecificos().Should().BeTrue();
        }

        [Fact]
        public void TieneFiltrosEspecificos_ConTodosLosFiltros_ReturnsTrue()
        {
            var dto = new FiltrosDashboardDto
            {
                ComplejoId = 1,
                CanchaId = 1,
                TipoCanchaId = 1,
                EstadoReservaId = 1,
                ClienteId = 1,
                AsadorId = 1,
                EstadoPagoId = 1
            };
            dto.TieneFiltrosEspecificos().Should().BeTrue();
        }

        [Fact]
        public void ObtenerDescripcionFiltros_SinFiltros_ReturnsMensajeSinFiltros()
        {
            var dto = new FiltrosDashboardDto { ComplejoId = 1 };

            var descripcion = dto.ObtenerDescripcionFiltros("Complejo Test");

            descripcion.Should().Contain("Sin filtros adicionales");
        }

        [Fact]
        public void ObtenerDescripcionFiltros_ConTodosLosFiltros_IncludesEachOne()
        {
            var dto = new FiltrosDashboardDto
            {
                ComplejoId = 1,
                Desde = new DateTime(2025, 1, 1),
                Hasta = new DateTime(2025, 1, 31),
                CanchaId = 1,
                TipoCanchaId = 1,
                EstadoReservaId = 1,
                ClienteId = 1,
                AsadorId = 1,
                EstadoPagoId = 1
            };

            var descripcion = dto.ObtenerDescripcionFiltros("Complejo Test");

            descripcion.Should().Contain("Complejo: Complejo Test");
            descripcion.Should().Contain("Filtrado por cancha");
            descripcion.Should().Contain("Filtrado por tipo de cancha");
            descripcion.Should().Contain("Filtrado por estado de reserva");
            descripcion.Should().Contain("Filtrado por cliente");
            descripcion.Should().Contain("Filtrado por asador");
            descripcion.Should().Contain("Filtrado por estado de pago");
        }

        [Fact]
        public void LimpiarFiltros_ResetsEverythingExceptComplejoIdAndPagination()
        {
            var dto = new FiltrosDashboardDto
            {
                ComplejoId = 1,
                Desde = DateTime.Today,
                Hasta = DateTime.Today,
                CanchaId = 1,
                TipoCanchaId = 1,
                EstadoReservaId = 1,
                ClienteId = 1,
                AsadorId = 1,
                EstadoPagoId = 1,
                PeriodoPredefinido = "hoy",
                Pagina = 3
            };

            dto.LimpiarFiltros();

            dto.ComplejoId.Should().Be(1);
            dto.Desde.Should().BeNull();
            dto.Hasta.Should().BeNull();
            dto.CanchaId.Should().BeNull();
            dto.TipoCanchaId.Should().BeNull();
            dto.EstadoReservaId.Should().BeNull();
            dto.ClienteId.Should().BeNull();
            dto.AsadorId.Should().BeNull();
            dto.EstadoPagoId.Should().BeNull();
            dto.PeriodoPredefinido.Should().BeNull();
            dto.Pagina.Should().Be(1);
        }

        [Fact]
        public void PorDefecto_CreatesExpectedDefaults()
        {
            var dto = FiltrosDashboardDto.PorDefecto(7);

            dto.ComplejoId.Should().Be(7);
            dto.PeriodoPredefinido.Should().Be("ultimos_30_dias");
            dto.Pagina.Should().Be(1);
            dto.TamanoPagina.Should().Be(10);
            dto.OrdenarPor.Should().Be("fecha");
            dto.OrdenDescendente.Should().BeTrue();
        }

        [Fact]
        public void CopiarConNuevoComplejo_KeepsSharedFiltersButResetsCanchaAndAsador()
        {
            var original = new FiltrosDashboardDto
            {
                ComplejoId = 1,
                Desde = new DateTime(2025, 1, 1),
                Hasta = new DateTime(2025, 1, 31),
                CanchaId = 5,
                TipoCanchaId = 2,
                EstadoReservaId = 3,
                ClienteId = 4,
                AsadorId = 6,
                EstadoPagoId = 1,
                PeriodoPredefinido = "hoy",
                Pagina = 2,
                TamanoPagina = 20,
                OrdenarPor = "nombre",
                OrdenDescendente = false
            };

            var copia = original.CopiarConNuevoComplejo(99);

            copia.ComplejoId.Should().Be(99);
            copia.CanchaId.Should().BeNull();
            copia.AsadorId.Should().BeNull();
            copia.TipoCanchaId.Should().Be(2);
            copia.EstadoReservaId.Should().Be(3);
            copia.ClienteId.Should().Be(4);
            copia.EstadoPagoId.Should().Be(1);
            copia.PeriodoPredefinido.Should().Be("hoy");
            copia.Pagina.Should().Be(2);
            copia.TamanoPagina.Should().Be(20);
            copia.OrdenarPor.Should().Be("nombre");
            copia.OrdenDescendente.Should().BeFalse();
        }
    }
}
