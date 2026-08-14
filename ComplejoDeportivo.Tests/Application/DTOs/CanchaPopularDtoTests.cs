using System.Collections.Generic;
using System.Linq;
using ComplejoDeportivo.Application.DTOs.Dashboard;
using FluentAssertions;
using Xunit;

namespace ComplejoDeportivo.Tests.Application.DTOs
{
    public class CanchaPopularDtoTests
    {
        [Fact]
        public void DisplayProperties_FormatCorrectly()
        {
            var dto = new CanchaPopularDto { PosicionRanking = 1, IngresosTotales = 15000, OcupacionPorcentaje = 75.5m, HorasTotales = 10 };

            dto.PosicionDisplay.Should().Be("#1");
            dto.IngresosDisplay.Should().Be($"${15000:N0}");
            dto.OcupacionDisplay.Should().Be($"{75.5m:F1}%");
            dto.HorasDisplay.Should().Be("10h");
        }

        [Fact]
        public void AnchoBarraOcupacion_CapsAt100()
        {
            var dto = new CanchaPopularDto { OcupacionPorcentaje = 150 };
            dto.AnchoBarraOcupacion.Should().Be(100);
        }

        [Theory]
        [InlineData(1, "#FFD700")]
        [InlineData(2, "#C0C0C0")]
        [InlineData(3, "#CD7F32")]
        [InlineData(4, "#6B7280")]
        public void ColorPosicion_MatchesRanking(int posicion, string colorEsperado)
        {
            var dto = new CanchaPopularDto { PosicionRanking = posicion };
            dto.ColorPosicion.Should().Be(colorEsperado);
        }

        [Theory]
        [InlineData("Futbol 5")]
        [InlineData("Fútbol 7")]
        [InlineData("futbol 11")]
        [InlineData("Padel")]
        public void IconoTipoCancha_ReturnsIconForEveryType(string tipo)
        {
            var dto = new CanchaPopularDto { TipoCancha = tipo };
            dto.IconoTipoCancha.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void AplicarRanking_OrdersByReservasThenIngresos()
        {
            var canchas = new List<CanchaPopularDto>
            {
                new() { CanchaId = 1, ReservasCount = 5, IngresosTotales = 100 },
                new() { CanchaId = 2, ReservasCount = 10, IngresosTotales = 50 },
                new() { CanchaId = 3, ReservasCount = 10, IngresosTotales = 200 }
            };

            var result = CanchaPopularDto.AplicarRanking(canchas);

            result[0].CanchaId.Should().Be(3);
            result[0].PosicionRanking.Should().Be(1);
            result[1].CanchaId.Should().Be(2);
            result[2].CanchaId.Should().Be(1);
        }

        [Fact]
        public void AgruparPorTipo_GroupsCorrectly()
        {
            var canchas = new List<CanchaPopularDto>
            {
                new() { CanchaId = 1, TipoCancha = "Futbol" },
                new() { CanchaId = 2, TipoCancha = "Futbol" },
                new() { CanchaId = 3, TipoCancha = "Padel" }
            };

            var result = CanchaPopularDto.AgruparPorTipo(canchas);

            result["Futbol"].Should().HaveCount(2);
            result["Padel"].Should().HaveCount(1);
        }
    }
}
