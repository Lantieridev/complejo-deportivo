using System.Collections.Generic;
using ComplejoDeportivo.Application.DTOs.Dashboard;
using FluentAssertions;
using Xunit;

namespace ComplejoDeportivo.Tests.Application.DTOs
{
    public class ReservaEstadoDtoTests
    {
        [Theory]
        [InlineData("pendiente")]
        [InlineData("pending")]
        [InlineData("confirmada")]
        [InlineData("confirmed")]
        [InlineData("cancelada")]
        [InlineData("cancelled")]
        [InlineData("completada")]
        [InlineData("completed")]
        [InlineData("en curso")]
        [InlineData("in progress")]
        [InlineData("no show")]
        [InlineData("desconocido")]
        public void ObtenerColorPorEstado_And_Icono_ReturnNonEmptyForEveryState(string estado)
        {
            ReservaEstadoDto.ObtenerColorPorEstado(estado).Should().NotBeNullOrEmpty();
            ReservaEstadoDto.ObtenerIconoPorEstado(estado).Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void CalcularPorcentajes_NullList_DoesNothing()
        {
            var act = () => ReservaEstadoDto.CalcularPorcentajes(null!);
            act.Should().NotThrow();
        }

        [Fact]
        public void CalcularPorcentajes_EmptyList_DoesNothing()
        {
            var list = new List<ReservaEstadoDto>();
            ReservaEstadoDto.CalcularPorcentajes(list);
            list.Should().BeEmpty();
        }

        [Fact]
        public void CalcularPorcentajes_WhenTotalZero_SetsPorcentajeZero()
        {
            var list = new List<ReservaEstadoDto> { new() { Estado = "Pendiente", Cantidad = 0 } };
            ReservaEstadoDto.CalcularPorcentajes(list);
            list[0].Porcentaje.Should().Be(0);
        }

        [Fact]
        public void CalcularPorcentajes_ComputesPercentagesAndAssignsColorIcon()
        {
            var list = new List<ReservaEstadoDto>
            {
                new() { Estado = "Pendiente", Cantidad = 25 },
                new() { Estado = "Confirmada", Cantidad = 75 }
            };

            ReservaEstadoDto.CalcularPorcentajes(list);

            list[0].Porcentaje.Should().Be(25);
            list[1].Porcentaje.Should().Be(75);
            list[0].Color.Should().NotBeNullOrEmpty();
            list[0].Icono.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void CrearDatosEjemplo_ReturnsFiveStatesWithPercentages()
        {
            var result = ReservaEstadoDto.CrearDatosEjemplo();
            result.Should().HaveCount(5);
            result.Should().OnlyContain(r => r.Porcentaje >= 0);
        }
    }
}
