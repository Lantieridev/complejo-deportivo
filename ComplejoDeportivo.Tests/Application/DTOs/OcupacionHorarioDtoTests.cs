using System.Collections.Generic;
using System.Linq;
using ComplejoDeportivo.Application.DTOs.Dashboard;
using FluentAssertions;
using Xunit;

namespace ComplejoDeportivo.Tests.Application.DTOs
{
    public class OcupacionHorarioDtoTests
    {
        [Theory]
        [InlineData(8, 10, 80, "Alta")]
        [InlineData(6, 10, 60, "Media")]
        [InlineData(3, 10, 30, "Baja")]
        [InlineData(1, 10, 10, "Muy Baja")]
        public void NivelOcupacionCanchas_And_Asadores_ReturnExpectedLevel(int ocupadas, int totales, decimal _, string esperado)
        {
            var dto = new OcupacionHorarioDto { CanchasOcupadas = ocupadas, CanchasTotales = totales, AsadoresOcupados = ocupadas, AsadoresTotales = totales };
            dto.NivelOcupacionCanchas.Should().Be(esperado);
            dto.NivelOcupacionAsadores.Should().Be(esperado);
            dto.ColorOcupacionCanchas.Should().NotBeNullOrEmpty();
            dto.ColorOcupacionAsadores.Should().NotBeNullOrEmpty();
            dto.IconoOcupacionCanchas.Should().NotBeNullOrEmpty();
            dto.IconoOcupacionAsadores.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void PorcentajeOcupacion_WhenTotalesZero_ReturnsZero()
        {
            var dto = new OcupacionHorarioDto();
            dto.PorcentajeOcupacionCanchas.Should().Be(0);
            dto.PorcentajeOcupacionAsadores.Should().Be(0);
        }

        [Fact]
        public void OcupacionTotalPromedio_AveragesBoth()
        {
            var dto = new OcupacionHorarioDto { CanchasOcupadas = 5, CanchasTotales = 10, AsadoresOcupados = 3, AsadoresTotales = 3 };
            dto.OcupacionTotalPromedio.Should().Be((50 + 100) / 2m);
        }

        [Fact]
        public void AnchoBarra_CapsAt100()
        {
            var dto = new OcupacionHorarioDto { CanchasOcupadas = 20, CanchasTotales = 10, AsadoresOcupados = 20, AsadoresTotales = 10 };
            dto.AnchoBarraCanchas.Should().Be(100);
            dto.AnchoBarraAsadores.Should().Be(100);
        }

        [Fact]
        public void EsHorarioPico_And_Valle_ReflectThresholds()
        {
            var pico = new OcupacionHorarioDto { CanchasOcupadas = 8, CanchasTotales = 10 };
            pico.EsHorarioPico.Should().BeTrue();
            pico.EsHorarioValle.Should().BeFalse();

            var valle = new OcupacionHorarioDto { CanchasOcupadas = 2, CanchasTotales = 10 };
            valle.EsHorarioValle.Should().BeTrue();
            valle.EsHorarioPico.Should().BeFalse();
        }

        [Fact]
        public void CrearFranjasHorarias_ReturnsSevenTwoHourSlots()
        {
            var result = OcupacionHorarioDto.CrearFranjasHorarias();
            result.Should().HaveCount(7);
            result[0].CanchasTotales.Should().Be(5);
            result[0].AsadoresTotales.Should().Be(3);
        }

        private static List<OcupacionHorarioDto> BuildSample() => new()
        {
            new() { Horario = "A", CanchasOcupadas = 2, CanchasTotales = 10 },
            new() { Horario = "B", CanchasOcupadas = 9, CanchasTotales = 10 }
        };

        [Fact]
        public void ObtenerFranjaMasOcupada_ReturnsHighest()
        {
            var result = OcupacionHorarioDto.ObtenerFranjaMasOcupada(BuildSample());
            result!.Horario.Should().Be("B");
        }

        [Fact]
        public void ObtenerFranjaMenosOcupada_ReturnsLowest()
        {
            var result = OcupacionHorarioDto.ObtenerFranjaMenosOcupada(BuildSample());
            result!.Horario.Should().Be("A");
        }

        [Fact]
        public void ObtenerFranjaMasOcupada_EmptyList_ReturnsNull()
        {
            var result = OcupacionHorarioDto.ObtenerFranjaMasOcupada(new List<OcupacionHorarioDto>());
            result.Should().BeNull();
        }

        [Fact]
        public void FiltrarHorariosPico_ReturnsOnlyPicoOnes()
        {
            var result = OcupacionHorarioDto.FiltrarHorariosPico(BuildSample());
            result.Should().ContainSingle(f => f.Horario == "B");
        }

        [Fact]
        public void FiltrarHorariosValle_ReturnsOnlyValleOnes()
        {
            var result = OcupacionHorarioDto.FiltrarHorariosValle(BuildSample());
            result.Should().ContainSingle(f => f.Horario == "A");
        }
    }
}
