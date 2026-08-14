using System;
using System.Collections.Generic;
using System.Linq;
using ComplejoDeportivo.Application.DTOs.Dashboard;
using FluentAssertions;
using Xunit;

namespace ComplejoDeportivo.Tests.Application.DTOs
{
    public class AlertaStockDtoTests
    {
        [Theory]
        [InlineData(0, 10, "agotado")]
        [InlineData(2, 10, "crítico")]
        [InlineData(8, 10, "bajo")]
        [InlineData(95, 100, "exceso")]
        [InlineData(50, 100, "normal")]
        public void NivelAlerta_ReturnsExpectedLevel(int stockActual, int stockMaximo, string esperado)
        {
            var dto = new AlertaStockDto { StockActual = stockActual, StockMinimo = 10, StockMaximo = stockMaximo };
            dto.NivelAlerta.Should().Be(esperado);
        }

        [Theory]
        [InlineData(0, "#DC3545")]
        [InlineData(2, "#FF2D00")]
        [InlineData(8, "#FFA500")]
        [InlineData(50, "#2E8B57")]
        [InlineData(95, "#17A2B8")]
        public void ColorAlerta_MatchesNivel(int stockActual, string colorEsperado)
        {
            var dto = new AlertaStockDto { StockActual = stockActual, StockMinimo = 10, StockMaximo = 100 };
            dto.ColorAlerta.Should().Be(colorEsperado);
        }

        [Fact]
        public void IconoAlerta_And_TextoAlerta_CoverEveryLevel()
        {
            foreach (var stockActual in new[] { 0, 2, 8, 95, 50 })
            {
                var dto = new AlertaStockDto { StockActual = stockActual, StockMinimo = 10, StockMaximo = 100 };
                dto.IconoAlerta.Should().NotBeNullOrEmpty();
                dto.TextoAlerta.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public void DiferenciaStock_ComputesCorrectly()
        {
            var dto = new AlertaStockDto { StockActual = 15, StockMinimo = 10 };
            dto.DiferenciaStock.Should().Be(5);
        }

        [Fact]
        public void PorcentajeStock_WhenMaximoIsZero_ReturnsZero()
        {
            var dto = new AlertaStockDto { StockActual = 10, StockMaximo = 0 };
            dto.PorcentajeStock.Should().Be(0);
        }

        [Fact]
        public void PorcentajeStock_WhenMaximoPositive_ComputesPercentage()
        {
            var dto = new AlertaStockDto { StockActual = 50, StockMaximo = 100 };
            dto.PorcentajeStock.Should().Be(50);
        }

        [Fact]
        public void DiasSinMovimiento_WhenNoUltimoMovimiento_ReturnsMinusOne()
        {
            var dto = new AlertaStockDto();
            dto.DiasSinMovimiento.Should().Be(-1);
        }

        [Fact]
        public void DiasSinMovimiento_WhenSet_ComputesDays()
        {
            var dto = new AlertaStockDto { UltimoMovimiento = DateTime.Now.AddDays(-3) };
            dto.DiasSinMovimiento.Should().BeInRange(2, 3);
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(2, true)]
        [InlineData(8, false)]
        public void NecesitaReposicionUrgente_ReturnsExpected(int stockActual, bool esperado)
        {
            var dto = new AlertaStockDto { StockActual = stockActual, StockMinimo = 10, StockMaximo = 100 };
            dto.NecesitaReposicionUrgente.Should().Be(esperado);
        }

        [Fact]
        public void NecesitaAtencion_WhenBajo_ReturnsTrue()
        {
            var dto = new AlertaStockDto { StockActual = 8, StockMinimo = 10, StockMaximo = 100 };
            dto.NecesitaAtencion.Should().BeTrue();
        }

        [Fact]
        public void AnchoBarraStock_CapsAt100()
        {
            var dto = new AlertaStockDto { StockActual = 500, StockMaximo = 100 };
            dto.AnchoBarraStock.Should().Be(100);
        }

        [Theory]
        [InlineData(10, "#DC3545")]
        [InlineData(30, "#FFA500")]
        [InlineData(60, "#17A2B8")]
        [InlineData(90, "#2E8B57")]
        public void ColorBarraStock_MatchesPercentageRange(int stockActual, string colorEsperado)
        {
            var dto = new AlertaStockDto { StockActual = stockActual, StockMaximo = 100 };
            dto.ColorBarraStock.Should().Be(colorEsperado);
        }

        private static List<AlertaStockDto> BuildSample() => new()
        {
            new AlertaStockDto { ProductoId = 1, Categoria = "Bebidas", StockActual = 0, StockMinimo = 10, StockMaximo = 100 },
            new AlertaStockDto { ProductoId = 2, Categoria = "Snacks", StockActual = 8, StockMinimo = 10, StockMaximo = 100 },
            new AlertaStockDto { ProductoId = 3, Categoria = "Bebidas", StockActual = 50, StockMinimo = 10, StockMaximo = 100 }
        };

        [Fact]
        public void FiltrarPorNivel_ReturnsMatchingItems()
        {
            var result = AlertaStockDto.FiltrarPorNivel(BuildSample(), "bajo");
            result.Should().ContainSingle(a => a.ProductoId == 2);
        }

        [Fact]
        public void ObtenerAlertasUrgentes_ReturnsOnlyUrgentOnes()
        {
            var result = AlertaStockDto.ObtenerAlertasUrgentes(BuildSample());
            result.Should().ContainSingle(a => a.ProductoId == 1);
        }

        [Fact]
        public void AgruparPorCategoria_GroupsCorrectly()
        {
            var result = AlertaStockDto.AgruparPorCategoria(BuildSample());
            result.Should().ContainKey("Bebidas").WhoseValue.Should().HaveCount(2);
            result.Should().ContainKey("Snacks").WhoseValue.Should().HaveCount(1);
        }

        [Fact]
        public void OrdenarPorUrgencia_OrdersMostUrgentFirst()
        {
            var result = AlertaStockDto.OrdenarPorUrgencia(BuildSample());
            result.First().ProductoId.Should().Be(1);
        }
    }
}
