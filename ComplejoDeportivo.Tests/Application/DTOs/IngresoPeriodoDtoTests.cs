using System;
using ComplejoDeportivo.Application.DTOs.Dashboard;
using FluentAssertions;
using Xunit;

namespace ComplejoDeportivo.Tests.Application.DTOs
{
    public class IngresoPeriodoDtoTests
    {
        [Fact]
        public void PromedioPorReserva_WhenNoReservas_ReturnsZero()
        {
            var dto = new IngresoPeriodoDto { Total = 1000, CantidadReservas = 0 };
            dto.PromedioPorReserva.Should().Be(0);
        }

        [Fact]
        public void PromedioPorReserva_ComputesAverage()
        {
            var dto = new IngresoPeriodoDto { Total = 1000, CantidadReservas = 10 };
            dto.PromedioPorReserva.Should().Be(100);
        }

        [Theory]
        [InlineData("diario")]
        [InlineData("daily")]
        [InlineData("semanal")]
        [InlineData("weekly")]
        [InlineData("mensual")]
        [InlineData("monthly")]
        [InlineData("anual")]
        [InlineData("yearly")]
        [InlineData("desconocido")]
        [InlineData(null)]
        public void FormatearPeriodo_EveryTipoAgrupacion_ReturnsNonEmptyString(string? tipo)
        {
            var result = IngresoPeriodoDto.FormatearPeriodo(new DateTime(2025, 3, 15), tipo!);
            result.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void CrearDatosEjemploMensual_ReturnsTwelveMonths()
        {
            var result = IngresoPeriodoDto.CrearDatosEjemploMensual();
            result.Should().HaveCount(12);
        }

        [Fact]
        public void CrearDatosEjemploDiario_ReturnsRequestedDayCount()
        {
            var result = IngresoPeriodoDto.CrearDatosEjemploDiario(15);
            result.Should().HaveCount(15);
        }

        [Fact]
        public void CrearDatosEjemploDiario_DefaultsToThirtyDays()
        {
            var result = IngresoPeriodoDto.CrearDatosEjemploDiario();
            result.Should().HaveCount(30);
        }
    }
}
