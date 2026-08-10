using System;
using System.Collections.Generic;
using System.Linq;
using ComplejoDeportivo.Application.DTOs.Dashboard;
using FluentAssertions;
using Xunit;

namespace ComplejoDeportivo.Tests.Application.DTOs
{
    public class ClienteFrecuenteDtoTests
    {
        [Fact]
        public void DisplayProperties_FormatCorrectly()
        {
            var dto = new ClienteFrecuenteDto
            {
                PosicionRanking = 2,
                TotalGastado = 50000,
                UltimaReserva = new DateTime(2025, 1, 15),
                FechaRegistro = DateTime.Now.AddMonths(-6),
                TotalReservas = 5
            };

            dto.PosicionDisplay.Should().Be("#2");
            dto.TotalGastadoDisplay.Should().Be($"${50000:N0}");
            dto.UltimaReservaDisplay.Should().Be("15/01/2025");
            dto.AntiguedadDisplay.Should().Contain("meses");
            dto.TicketPromedio.Should().Be(10000);
            dto.TicketPromedioDisplay.Should().Be($"${10000:N0}");
        }

        [Fact]
        public void TicketPromedio_WhenNoReservas_ReturnsZero()
        {
            var dto = new ClienteFrecuenteDto { TotalReservas = 0, TotalGastado = 0 };
            dto.TicketPromedio.Should().Be(0);
        }

        [Theory]
        [InlineData(250000, "VIP")]
        [InlineData(150000, "Frecuente")]
        [InlineData(75000, "Regular")]
        [InlineData(10000, "Ocasional")]
        public void CategoriaCliente_ReturnsExpectedCategory(decimal gastado, string esperado)
        {
            var dto = new ClienteFrecuenteDto { TotalGastado = gastado };
            dto.CategoriaCliente.Should().Be(esperado);
        }

        [Fact]
        public void ColorCategoria_CoversEveryCategory()
        {
            foreach (var gastado in new decimal[] { 250000, 150000, 75000, 10000 })
            {
                var dto = new ClienteFrecuenteDto { TotalGastado = gastado };
                dto.ColorCategoria.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public void EsClienteActivo_WhenRecentReserva_ReturnsTrue()
        {
            var dto = new ClienteFrecuenteDto { UltimaReserva = DateTime.Now.AddDays(-5) };
            dto.EsClienteActivo.Should().BeTrue();
            dto.IconoActividad.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void EsClienteActivo_WhenOldReserva_ReturnsFalse()
        {
            var dto = new ClienteFrecuenteDto { UltimaReserva = DateTime.Now.AddDays(-90) };
            dto.EsClienteActivo.Should().BeFalse();
        }

        private static List<ClienteFrecuenteDto> BuildSample() => new()
        {
            new() { ClienteId = 1, TotalReservas = 5, TotalGastado = 250000, UltimaReserva = DateTime.Now },
            new() { ClienteId = 2, TotalReservas = 10, TotalGastado = 10000, UltimaReserva = DateTime.Now.AddDays(-90) }
        };

        [Fact]
        public void AplicarRanking_OrdersByReservasThenGastado()
        {
            var result = ClienteFrecuenteDto.AplicarRanking(BuildSample());
            result[0].ClienteId.Should().Be(2);
            result[0].PosicionRanking.Should().Be(1);
        }

        [Fact]
        public void FiltrarActivos_ReturnsOnlyActiveClients()
        {
            var result = ClienteFrecuenteDto.FiltrarActivos(BuildSample());
            result.Should().ContainSingle(c => c.ClienteId == 1);
        }

        [Fact]
        public void AgruparPorCategoria_GroupsAndOrdersByPriority()
        {
            var result = ClienteFrecuenteDto.AgruparPorCategoria(BuildSample());
            result.Keys.First().Should().Be("VIP");
        }

        [Fact]
        public void AgruparPorCategoria_OrdersAllFourCategoriesByPriority()
        {
            var clientes = new List<ClienteFrecuenteDto>
            {
                new() { ClienteId = 1, TotalGastado = 10000 },   // Ocasional
                new() { ClienteId = 2, TotalGastado = 75000 },   // Regular
                new() { ClienteId = 3, TotalGastado = 150000 },  // Frecuente
                new() { ClienteId = 4, TotalGastado = 250000 }   // VIP
            };

            var result = ClienteFrecuenteDto.AgruparPorCategoria(clientes);

            result.Keys.Should().ContainInOrder("VIP", "Frecuente", "Regular", "Ocasional");
        }
    }
}
