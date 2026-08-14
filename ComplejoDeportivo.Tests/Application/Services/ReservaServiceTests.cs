using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ComplejoDeportivo.Application.DTOs;
using ComplejoDeportivo.Application.Repositories;
using ComplejoDeportivo.Application.Services;
using ComplejoDeportivo.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace ComplejoDeportivo.Tests.Application.Services
{
    public class ReservaServiceTests
    {
        private readonly Mock<IReservaRepository> _repoMock;
        private readonly ReservaService _service;

        public ReservaServiceTests()
        {
            _repoMock = new Mock<IReservaRepository>();
            _service = new ReservaService(_repoMock.Object);
        }

        [Fact]
        public async Task ObtenerTurnosDisponibles_ShouldReturnList()
        {
            var fecha = new DateOnly(2023, 10, 10);
            var expected = new List<DisponibilidadCanchaDTO>();
            _repoMock.Setup(r => r.ObtenerTurnosDisponiblesAsync(1, fecha, new TimeOnly(8,0), new TimeOnly(23,0)))
                .ReturnsAsync(expected);

            var result = await _service.ObtenerTurnosDisponibles(1, fecha);

            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task CrearReserva_NullCanchas_ThrowsException()
        {
            var dto = new CrearReservaDTO { CanchaIds = null };

            var act = async () => await _service.CrearReserva(dto);

            await act.Should().ThrowAsync<Exception>().WithMessage("Debe seleccionar al menos una cancha.");
        }

        [Fact]
        public async Task CrearReserva_EmptyCanchas_ThrowsException()
        {
            var dto = new CrearReservaDTO { CanchaIds = new List<int>() };

            var act = async () => await _service.CrearReserva(dto);

            await act.Should().ThrowAsync<Exception>().WithMessage("Debe seleccionar al menos una cancha.");
        }

        [Theory]
        [InlineData("10:00:00", "10:30:00")]
        [InlineData("10:00:00", "11:30:00")]
        [InlineData("10:00:00", "10:00:00")]
        public async Task CrearReserva_InvalidHours_ThrowsException(string start, string end)
        {
            var dto = new CrearReservaDTO 
            { 
                CanchaIds = new List<int> { 1 },
                HoraInicio = TimeOnly.Parse(start),
                HoraFin = TimeOnly.Parse(end)
            };

            var act = async () => await _service.CrearReserva(dto);

            await act.Should().ThrowAsync<Exception>().WithMessage("Las reservas deben ser en bloques de 1 hora.");
        }

        [Fact]
        public async Task CrearReserva_ScheduleOccupied_ThrowsException()
        {
            var dto = new CrearReservaDTO 
            { 
                CanchaIds = new List<int> { 1 },
                Fecha = new DateOnly(2023, 10, 10),
                HoraInicio = new TimeOnly(10, 0),
                HoraFin = new TimeOnly(12, 0)
            };

            var ocupados = new List<HorarioOcupadoDTO> 
            {
                new HorarioOcupadoDTO { CanchaId = 1, HoraInicio = new TimeOnly(10, 30), HoraFin = new TimeOnly(11, 30) }
            };

            _repoMock.Setup(r => r.ObtenerHorariosOcupadosAsync(It.IsAny<List<int>>(), dto.Fecha)).ReturnsAsync(ocupados);
            _repoMock.Setup(r => r.ObtenerTarifasPorFechaAsync(It.IsAny<List<int>>(), dto.Fecha)).ReturnsAsync(new List<Tarifa>());

            var act = async () => await _service.CrearReserva(dto);

            await act.Should().ThrowAsync<Exception>().WithMessage("Horario ocupado en cancha ID 1: 10:00 - 11:00");
        }

        [Fact]
        public async Task CrearReserva_NonOverlappingOccupiedSlot_Succeeds()
        {
            // Non-empty "ocupados" list whose slots do NOT overlap the requested time, so the
            // `o.HoraInicio < fin && o.HoraFin > h` predicate actually runs. One slot is entirely
            // before the window (first condition false, short-circuits) and one entirely after
            // (first condition true, second false) — as opposed to an empty list, where .Any()
            // never invokes the predicate at all.
            var dto = new CrearReservaDTO
            {
                CanchaIds = new List<int> { 1 },
                Fecha = new DateOnly(2023, 10, 10),
                HoraInicio = new TimeOnly(10, 0),
                HoraFin = new TimeOnly(11, 0),
                ClienteId = 5,
                Ambito = "App"
            };

            var ocupados = new List<HorarioOcupadoDTO>
            {
                new HorarioOcupadoDTO { CanchaId = 1, HoraInicio = new TimeOnly(6, 0), HoraFin = new TimeOnly(7, 0) },
                new HorarioOcupadoDTO { CanchaId = 1, HoraInicio = new TimeOnly(15, 0), HoraFin = new TimeOnly(16, 0) }
            };
            var tarifas = new List<Tarifa> { new Tarifa { TarifaId = 10, CanchaId = 1, Precio = 100 } };

            _repoMock.Setup(r => r.ObtenerHorariosOcupadosAsync(It.IsAny<List<int>>(), dto.Fecha)).ReturnsAsync(ocupados);
            _repoMock.Setup(r => r.ObtenerTarifasPorFechaAsync(It.IsAny<List<int>>(), dto.Fecha)).ReturnsAsync(tarifas);
            _repoMock.Setup(r => r.ObtenerTarifaVigenteEnMemoria(It.IsAny<IEnumerable<Tarifa>>(), 1, dto.Fecha, It.IsAny<TimeOnly>()))
                .Returns(new Tarifa { TarifaId = 10, Precio = 100 });
            _repoMock.Setup(r => r.CrearReservaConDetallesAsync(It.IsAny<Reserva>(), It.IsAny<List<DetalleReserva>>()))
                .ReturnsAsync((Reserva r, List<DetalleReserva> d) => { r.ReservaId = 100; return r; });
            _repoMock.Setup(r => r.ObtenerNombreCanchaAsync(1)).ReturnsAsync("Cancha Uno");

            var result = await _service.CrearReserva(dto);

            result.ReservaId.Should().Be(100);
        }

        [Fact]
        public async Task CrearReserva_Success_ReturnsReservaDTO()
        {
            var dto = new CrearReservaDTO 
            { 
                CanchaIds = new List<int> { 1, 1 }, // Duplicate to test Distinct
                Fecha = new DateOnly(2023, 10, 10),
                HoraInicio = new TimeOnly(10, 0),
                HoraFin = new TimeOnly(12, 0),
                ClienteId = 5,
                Ambito = "App"
            };

            var tarifas = new List<Tarifa> { new Tarifa { TarifaId = 10, CanchaId = 1, Precio = 100 } };

            _repoMock.Setup(r => r.ObtenerHorariosOcupadosAsync(It.IsAny<List<int>>(), dto.Fecha)).ReturnsAsync(new List<HorarioOcupadoDTO>());
            _repoMock.Setup(r => r.ObtenerTarifasPorFechaAsync(It.IsAny<List<int>>(), dto.Fecha)).ReturnsAsync(tarifas);
            
            _repoMock.Setup(r => r.ObtenerTarifaVigenteEnMemoria(It.IsAny<IEnumerable<Tarifa>>(), 1, dto.Fecha, It.IsAny<TimeOnly>()))
                .Returns(new Tarifa { TarifaId = 10, Precio = 100 });

            _repoMock.Setup(r => r.CrearReservaConDetallesAsync(It.IsAny<Reserva>(), It.IsAny<List<DetalleReserva>>()))
                .ReturnsAsync((Reserva r, List<DetalleReserva> d) => {
                    r.ReservaId = 99;
                    return r;
                });

            _repoMock.Setup(r => r.ObtenerNombreCanchaAsync(1)).ReturnsAsync("Cancha Uno");

            var result = await _service.CrearReserva(dto);

            result.Should().NotBeNull();
            result.ReservaId.Should().Be(99);
            result.ClienteId.Should().Be(5);
            result.Total.Should().Be(200); // 2 horas * 100
            result.Detalles.Should().HaveCount(1);
            result.Detalles.First().NombreCancha.Should().Be("Cancha Uno");
            result.Detalles.First().Subtotal.Should().Be(200);
            result.Detalles.First().CantidadHoras.Should().Be(2);
        }

        [Fact]
        public async Task ListarReservasCliente_ShouldReturnList()
        {
            var reservas = new List<Reserva>
            {
                new Reserva 
                { 
                    ReservaId = 1, 
                    ClienteId = 5, 
                    EstadoReserva = new EstadoReserva { Nombre = "Confirmada" },
                    DetalleReservas = new List<DetalleReserva> 
                    {
                        new DetalleReserva { DetalleReservaId = 1, Cancha = new Cancha { Nombre = "Cancha 1" } },
                        new DetalleReserva { DetalleReservaId = 2, Cancha = null }
                    }
                },
                new Reserva
                {
                    ReservaId = 2,
                    ClienteId = 5,
                    EstadoReserva = null,
                    DetalleReservas = new List<DetalleReserva>()
                }
            };

            _repoMock.Setup(r => r.ObtenerReservasPorCliente(5)).ReturnsAsync(reservas);

            var result = await _service.ListarReservasCliente(5);

            result.Should().HaveCount(2);
            result[0].Estado.Should().Be("Confirmada");
            result[0].Detalles.Should().HaveCount(2);
            result[0].Detalles[0].NombreCancha.Should().Be("Cancha 1");
            result[0].Detalles[1].NombreCancha.Should().Be("N/A");
            
            result[1].Estado.Should().Be("Desconocido");
        }

        [Fact]
        public async Task CancelarReserva_ReservaNull_ReturnsFalse()
        {
            var dto = new CancelarReservaDTO { ReservaId = 1, ClienteId = 5 };
            _repoMock.Setup(r => r.ObtenerReservaPorIdAsync(1)).ReturnsAsync((Reserva?)null);

            var result = await _service.CancelarReserva(dto);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task CancelarReserva_DifferentClient_ReturnsFalse()
        {
            var dto = new CancelarReservaDTO { ReservaId = 1, ClienteId = 5 };
            _repoMock.Setup(r => r.ObtenerReservaPorIdAsync(1)).ReturnsAsync(new Reserva { ClienteId = 10 });

            var result = await _service.CancelarReserva(dto);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task CancelarReserva_NotConfirmed_ThrowsException()
        {
            var dto = new CancelarReservaDTO { ReservaId = 1, ClienteId = 5 };
            _repoMock.Setup(r => r.ObtenerReservaPorIdAsync(1)).ReturnsAsync(new Reserva { ClienteId = 5, EstadoReservaId = 2 });

            var act = async () => await _service.CancelarReserva(dto);

            await act.Should().ThrowAsync<Exception>().WithMessage("La reserva no se puede cancelar porque no está 'Confirmada'.");
        }

        [Fact]
        public async Task CancelarReserva_LessThan24Hours_ThrowsException()
        {
            var dto = new CancelarReservaDTO { ReservaId = 1, ClienteId = 5 };
            var reserva = new Reserva 
            { 
                ClienteId = 5, 
                EstadoReservaId = 1,
                Fecha = DateOnly.FromDateTime(DateTime.Now),
                HoraInicio = TimeOnly.FromDateTime(DateTime.Now.AddHours(12)) // less than 24 hours
            };
            _repoMock.Setup(r => r.ObtenerReservaPorIdAsync(1)).ReturnsAsync(reserva);

            var act = async () => await _service.CancelarReserva(dto);

            await act.Should().ThrowAsync<Exception>().WithMessage("La reserva solo se puede cancelar con 24 horas de anticipación.");
        }

        [Fact]
        public async Task CancelarReserva_Valid_UpdatesStateAndReturnsTrue()
        {
            var dto = new CancelarReservaDTO { ReservaId = 1, ClienteId = 5 };
            var reserva = new Reserva 
            { 
                ClienteId = 5, 
                EstadoReservaId = 1,
                Fecha = DateOnly.FromDateTime(DateTime.Now.AddDays(2)), // more than 24 hours
                HoraInicio = TimeOnly.FromDateTime(DateTime.Now)
            };
            _repoMock.Setup(r => r.ObtenerReservaPorIdAsync(1)).ReturnsAsync(reserva);
            _repoMock.Setup(r => r.GuardarAsync()).Returns(Task.CompletedTask);

            var result = await _service.CancelarReserva(dto);

            result.Should().BeTrue();
            reserva.EstadoReservaId.Should().Be(2);
            _repoMock.Verify(r => r.GuardarAsync(), Times.Once);
        }

        [Fact]
        public async Task ListarComplejos_ShouldReturnList()
        {
            var list = new List<ComplejoDTO>();
            _repoMock.Setup(r => r.ObtenerComplejosAsync()).ReturnsAsync(list);

            var result = await _service.ListarComplejos();

            result.Should().BeSameAs(list);
        }

        [Fact]
        public async Task ListarCanchasPorComplejo_ShouldReturnList()
        {
            var list = new List<CanchaDTO>();
            _repoMock.Setup(r => r.ObtenerCanchasPorComplejoAsync(1)).ReturnsAsync(list);

            var result = await _service.ListarCanchasPorComplejo(1);

            result.Should().BeSameAs(list);
        }

        [Fact]
        public async Task ObtenerHorariosDisponiblesCancha_ShouldReturnList()
        {
            var list = new List<HorarioLibreDTO>();
            var fecha = new DateOnly(2023, 10, 10);
            _repoMock.Setup(r => r.ObtenerHorariosDisponiblesCanchaAsync(1, fecha, new TimeOnly(8,0), new TimeOnly(23,0))).ReturnsAsync(list);

            var result = await _service.ObtenerHorariosDisponiblesCancha(1, fecha);

            result.Should().BeSameAs(list);
        }
    }
}
