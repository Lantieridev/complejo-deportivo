using System;
using System.Collections.Generic;
using ComplejoDeportivo.Application.DTOs.Dashboard;
using FluentAssertions;
using Xunit;

namespace ComplejoDeportivo.Tests.Application.DTOs
{
    public class ReservaRecienteDtoTests
    {
        [Fact]
        public void DisplayProperties_FormatCorrectly()
        {
            var dto = new ReservaRecienteDto
            {
                Fecha = new DateTime(2025, 3, 10),
                HoraInicio = new TimeSpan(18, 0, 0),
                HoraFin = new TimeSpan(19, 30, 0),
                Total = 5000
            };

            dto.FechaDisplay.Should().Be("10/03/2025");
            dto.HorarioDisplay.Should().Be("18:00 - 19:30");
            dto.TotalDisplay.Should().Be($"${5000:N0}");
            dto.DuracionDisplay.Should().Be($"{1.5}h");
        }

        [Theory]
        [InlineData("pendiente")]
        [InlineData("confirmada")]
        [InlineData("en curso")]
        [InlineData("completada")]
        [InlineData("cancelada")]
        [InlineData("desconocido")]
        public void EstadoColor_And_Icono_ReturnNonEmptyForEveryState(string estado)
        {
            var dto = new ReservaRecienteDto { Estado = estado };
            dto.EstadoColor.Should().NotBeNullOrEmpty();
            dto.EstadoIcono.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void EsHoy_EsFutura_EsPasada_ReflectDate()
        {
            new ReservaRecienteDto { Fecha = DateTime.Today }.EsHoy.Should().BeTrue();
            new ReservaRecienteDto { Fecha = DateTime.Today.AddDays(1) }.EsFutura.Should().BeTrue();
            new ReservaRecienteDto { Fecha = DateTime.Today.AddDays(-1) }.EsPasada.Should().BeTrue();
        }

        [Fact]
        public void BadgeFecha_And_ColorBadgeFecha_ReflectTiming()
        {
            var hoy = new ReservaRecienteDto { Fecha = DateTime.Today };
            hoy.BadgeFecha.Should().Be("HOY");
            hoy.ColorBadgeFecha.Should().Be("bg-warning");

            var futura = new ReservaRecienteDto { Fecha = DateTime.Today.AddDays(1) };
            futura.BadgeFecha.Should().Be("PRÓXIMA");
            futura.ColorBadgeFecha.Should().Be("bg-info");

            var pasada = new ReservaRecienteDto { Fecha = DateTime.Today.AddDays(-1) };
            pasada.BadgeFecha.Should().Be("PASADA");
            pasada.ColorBadgeFecha.Should().Be("bg-secondary");
        }

        [Fact]
        public void EstaEnCurso_WhenNotToday_ReturnsFalse()
        {
            var dto = new ReservaRecienteDto { Fecha = DateTime.Today.AddDays(-1), Estado = "confirmada" };
            dto.EstaEnCurso.Should().BeFalse();
            dto.IconoEnCurso.Should().BeEmpty();
        }

        [Fact]
        public void EstaEnCurso_WhenTodayInRangeAndConfirmada_ReturnsTrue()
        {
            var ahora = DateTime.Now.TimeOfDay;
            var dto = new ReservaRecienteDto
            {
                Fecha = DateTime.Today,
                HoraInicio = ahora.Subtract(TimeSpan.FromMinutes(30)),
                HoraFin = ahora.Add(TimeSpan.FromMinutes(30)),
                Estado = "confirmada"
            };
            dto.EstaEnCurso.Should().BeTrue();
            dto.IconoEnCurso.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void EstaEnCurso_WhenTodayButNotConfirmada_ReturnsFalse()
        {
            var ahora = DateTime.Now.TimeOfDay;
            var dto = new ReservaRecienteDto
            {
                Fecha = DateTime.Today,
                HoraInicio = ahora.Subtract(TimeSpan.FromMinutes(30)),
                HoraFin = ahora.Add(TimeSpan.FromMinutes(30)),
                Estado = "pendiente"
            };
            dto.EstaEnCurso.Should().BeFalse();
        }

        private static List<ReservaRecienteDto> BuildSample() => new()
        {
            new() { ReservaId = 1, Estado = "confirmada", Fecha = DateTime.Today, HoraInicio = new TimeSpan(10, 0, 0) },
            new() { ReservaId = 2, Estado = "cancelada", Fecha = DateTime.Today.AddDays(1), HoraInicio = new TimeSpan(11, 0, 0) }
        };

        [Fact]
        public void FiltrarPorEstado_ReturnsMatching()
        {
            var result = ReservaRecienteDto.FiltrarPorEstado(BuildSample(), "CONFIRMADA");
            result.Should().ContainSingle(r => r.ReservaId == 1);
        }

        [Fact]
        public void FiltrarPorFecha_ReturnsMatching()
        {
            var result = ReservaRecienteDto.FiltrarPorFecha(BuildSample(), DateTime.Today);
            result.Should().ContainSingle(r => r.ReservaId == 1);
        }

        [Fact]
        public void ObtenerProximas24Horas_ReturnsUpcomingConfirmed()
        {
            var result = ReservaRecienteDto.ObtenerProximas24Horas(BuildSample());
            result.Should().Contain(r => r.ReservaId == 1);
        }

        [Fact]
        public void ObtenerProximas24Horas_ExcludesOutOfRangeAndPastDates()
        {
            var reservas = new List<ReservaRecienteDto>
            {
                new() { ReservaId = 10, Estado = "confirmada", Fecha = DateTime.Today.AddDays(-1) }, // pasada: excluida
                new() { ReservaId = 11, Estado = "confirmada", Fecha = DateTime.Today.AddDays(10) }  // muy lejana: excluida
            };

            var result = ReservaRecienteDto.ObtenerProximas24Horas(reservas);

            result.Should().BeEmpty();
        }

        [Fact]
        public void OrdenarPorFecha_Descendente_OrdersNewestFirst()
        {
            var result = ReservaRecienteDto.OrdenarPorFecha(BuildSample(), descendente: true);
            result[0].ReservaId.Should().Be(2);
        }

        [Fact]
        public void OrdenarPorFecha_Ascendente_OrdersOldestFirst()
        {
            var result = ReservaRecienteDto.OrdenarPorFecha(BuildSample(), descendente: false);
            result[0].ReservaId.Should().Be(1);
        }
    }
}
