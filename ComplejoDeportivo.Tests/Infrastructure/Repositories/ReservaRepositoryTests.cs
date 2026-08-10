using ComplejoDeportivo.Application.DTOs;
using ComplejoDeportivo.Domain;
using ComplejoDeportivo.Infrastructure.Persistence;
using ComplejoDeportivo.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ComplejoDeportivo.Tests.Infrastructure.Repositories
{
    public class ReservaRepositoryTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
    {
        private readonly DatabaseFixture _fixture;
        private ComplejoDeportivoContext _context = null!;
        private ReservaRepository _repository = null!;

        public ReservaRepositoryTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public async Task InitializeAsync()
        {
            _context = new ComplejoDeportivoContext(_fixture.Options);
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM DetalleReserva; DELETE FROM Reserva; DELETE FROM BloqueoCancha; DELETE FROM Tarifa; DELETE FROM Cancha; DELETE FROM Complejo; DELETE FROM Direccion; DELETE FROM Cliente; DELETE FROM EstadoReserva; DELETE FROM TipoCancha; DELETE FROM TipoSuperficie;");
            _repository = new ReservaRepository(_context);
        }

        public async Task DisposeAsync()
        {
            if (_context != null)
            {
                await _context.DisposeAsync();
            }
        }

        // --- Helpers: build the real FK chain every persisted Cancha/Reserva needs ---

        private async Task<Cancha> CrearCanchaAsync(string nombre = "Cancha Test")
        {
            var direccion = new Direccion { Calle = "Calle", Numero = "1", Ciudad = "Ciudad", Provincia = "Provincia", CodigoPostal = "1000" };
            await _context.Direccions.AddAsync(direccion);
            await _context.SaveChangesAsync();

            var complejo = new Complejo { Nombre = "Complejo Test", DireccionId = direccion.DireccionId };
            var tipoCancha = new TipoCancha { Nombre = "Futbol" };
            var tipoSuperficie = new TipoSuperficie { Nombre = "Sintetico" };
            await _context.Complejos.AddAsync(complejo);
            await _context.TipoCanchas.AddAsync(tipoCancha);
            await _context.TipoSuperficies.AddAsync(tipoSuperficie);
            await _context.SaveChangesAsync();

            var cancha = new Cancha { Nombre = nombre, ComplejoId = complejo.ComplejoId, TipoCanchaId = tipoCancha.TipoCanchaId, TipoSuperficieId = tipoSuperficie.TipoSuperficieId, Activa = true };
            await _context.Canchas.AddAsync(cancha);
            await _context.SaveChangesAsync();
            return cancha;
        }

        private async Task<(Cancha cancha, Tarifa tarifa)> CrearCanchaConTarifaAsync(decimal precio = 100)
        {
            var cancha = await CrearCanchaAsync();
            var tarifa = new Tarifa { CanchaId = cancha.CanchaId, FechaVigencia = new DateOnly(2020, 1, 1), Precio = precio, ContratoLuz = false, EsActual = true };
            await _context.Tarifas.AddAsync(tarifa);
            await _context.SaveChangesAsync();
            return (cancha, tarifa);
        }

        private async Task<(int clienteId, int estadoReservaId)> CrearClienteYEstadoAsync()
        {
            var cliente = new Cliente { Nombre = "Test", Apellido = "Cliente" };
            var estado = new EstadoReserva { Nombre = "Confirmada" };
            await _context.Clientes.AddAsync(cliente);
            await _context.EstadoReservas.AddAsync(estado);
            await _context.SaveChangesAsync();
            return (cliente.ClienteId, estado.EstadoReservaId);
        }

        [Fact]
        public async Task ExisteReservaSuperpuestaAsync_ShouldReturnTrue_WhenSuperpuesta()
        {
            var (cancha, tarifa) = await CrearCanchaConTarifaAsync();
            var (clienteId, estadoId) = await CrearClienteYEstadoAsync();
            var fecha = new DateOnly(2025, 1, 1);
            var reserva = new Reserva { Fecha = fecha, HoraInicio = new TimeOnly(18, 0), HoraFin = new TimeOnly(19, 0), ClienteId = clienteId, EstadoReservaId = estadoId, Ambito = "Web", FechaCreacion = DateTime.Now };
            reserva.DetalleReservas = new List<DetalleReserva> { new DetalleReserva { CanchaId = cancha.CanchaId, TarifaHoraId = tarifa.TarifaId, CantidadHoras = 1, Subtotal = tarifa.Precio } };
            await _context.Reservas.AddAsync(reserva);
            await _context.SaveChangesAsync();

            var result = await _repository.ExisteReservaSuperpuestaAsync(cancha.CanchaId, fecha, new TimeOnly(18, 0), new TimeOnly(19, 0));
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExisteReservaSuperpuestaAsync_ShouldReturnFalse_WhenNotSuperpuesta()
        {
            var (cancha, tarifa) = await CrearCanchaConTarifaAsync();
            var (clienteId, estadoId) = await CrearClienteYEstadoAsync();
            var fecha = new DateOnly(2025, 1, 1);
            var reserva = new Reserva { Fecha = fecha, HoraInicio = new TimeOnly(18, 0), HoraFin = new TimeOnly(19, 0), ClienteId = clienteId, EstadoReservaId = estadoId, Ambito = "Web", FechaCreacion = DateTime.Now };
            reserva.DetalleReservas = new List<DetalleReserva> { new DetalleReserva { CanchaId = cancha.CanchaId, TarifaHoraId = tarifa.TarifaId, CantidadHoras = 1, Subtotal = tarifa.Precio } };
            await _context.Reservas.AddAsync(reserva);
            await _context.SaveChangesAsync();

            var result = await _repository.ExisteReservaSuperpuestaAsync(cancha.CanchaId, fecha, new TimeOnly(19, 0), new TimeOnly(20, 0));
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExisteBloqueoAsync_ShouldReturnTrue_WhenBloqueoExists()
        {
            var cancha = await CrearCanchaAsync();
            var fecha = new DateOnly(2025, 1, 1);
            var bloqueo = new BloqueoCancha { CanchaId = cancha.CanchaId, Fecha = fecha, HoraInicio = new TimeOnly(10, 0), HoraFin = new TimeOnly(11, 0), Motivo = "Mantenimiento" };
            await _context.BloqueoCanchas.AddAsync(bloqueo);
            await _context.SaveChangesAsync();

            var result = await _repository.ExisteBloqueoAsync(cancha.CanchaId, fecha, new TimeOnly(10, 0), new TimeOnly(11, 0));
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExisteBloqueoAsync_ShouldReturnFalse_WhenNoBloqueo()
        {
            var result = await _repository.ExisteBloqueoAsync(2, new DateOnly(2025, 1, 1), new TimeOnly(10, 0), new TimeOnly(11, 0));
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ObtenerTurnosDisponiblesAsync_ShouldReturnTurnosLibres()
        {
            var cancha = await CrearCanchaAsync();
            var fecha = new DateOnly(2025, 1, 1);
            // Bloqueo de 11 a 12
            var bloqueo = new BloqueoCancha { CanchaId = cancha.CanchaId, Fecha = fecha, HoraInicio = new TimeOnly(11, 0), HoraFin = new TimeOnly(12, 0), Motivo = "Mantenimiento" };
            await _context.BloqueoCanchas.AddAsync(bloqueo);
            await _context.SaveChangesAsync();

            // Apertura 10:00 a 13:00 -> turnos esperados: 10 a 11, y 12 a 13.
            var result = await _repository.ObtenerTurnosDisponiblesAsync(cancha.CanchaId, fecha, new TimeOnly(10, 0), new TimeOnly(13, 0));

            result.Should().HaveCount(2);
            result.Any(t => t.HoraInicio == new TimeOnly(10, 0) && t.HoraFin == new TimeOnly(11, 0)).Should().BeTrue();
            result.Any(t => t.HoraInicio == new TimeOnly(12, 0) && t.HoraFin == new TimeOnly(13, 0)).Should().BeTrue();
        }

        [Fact]
        public async Task ObtenerHorariosOcupadosAsync_ShouldCombineReservasAndBloqueos()
        {
            var (canchaReserva, tarifa) = await CrearCanchaConTarifaAsync();
            var canchaBloqueo = await CrearCanchaAsync("Cancha Bloqueo");
            var (clienteId, estadoId) = await CrearClienteYEstadoAsync();
            var fecha = new DateOnly(2025, 1, 1);
            var reserva = new Reserva { Fecha = fecha, HoraInicio = new TimeOnly(14, 0), HoraFin = new TimeOnly(15, 0), ClienteId = clienteId, EstadoReservaId = estadoId, Ambito = "Web", FechaCreacion = DateTime.Now };
            reserva.DetalleReservas = new List<DetalleReserva> { new DetalleReserva { CanchaId = canchaReserva.CanchaId, TarifaHoraId = tarifa.TarifaId, CantidadHoras = 1, Subtotal = tarifa.Precio } };
            var bloqueo = new BloqueoCancha { CanchaId = canchaBloqueo.CanchaId, Fecha = fecha, HoraInicio = new TimeOnly(15, 0), HoraFin = new TimeOnly(16, 0), Motivo = "Mantenimiento" };

            await _context.Reservas.AddAsync(reserva);
            await _context.BloqueoCanchas.AddAsync(bloqueo);
            await _context.SaveChangesAsync();

            var result = await _repository.ObtenerHorariosOcupadosAsync(new List<int> { canchaReserva.CanchaId, canchaBloqueo.CanchaId }, fecha);

            result.Should().HaveCount(2);
            result.Should().Contain(x => x.CanchaId == canchaReserva.CanchaId && x.HoraInicio == new TimeOnly(14, 0));
            result.Should().Contain(x => x.CanchaId == canchaBloqueo.CanchaId && x.HoraInicio == new TimeOnly(15, 0));
        }

        [Fact]
        public async Task ObtenerTarifasPorFechaAsync_ShouldReturnTarifas()
        {
            var cancha = await CrearCanchaAsync();
            var fecha = new DateOnly(2025, 1, 1);
            var tarifa = new Tarifa { CanchaId = cancha.CanchaId, FechaVigencia = fecha, Precio = 100, ContratoLuz = false, EsActual = true };
            await _context.Tarifas.AddAsync(tarifa);
            await _context.SaveChangesAsync();

            var result = await _repository.ObtenerTarifasPorFechaAsync(new List<int> { cancha.CanchaId }, fecha.AddDays(1));

            result.Should().HaveCount(1);
            result.First().Precio.Should().Be(100);
        }

        [Fact]
        public async Task ObtenerReservaPorIdAsync_ShouldReturnReserva()
        {
            var (clienteId, estadoId) = await CrearClienteYEstadoAsync();
            var reserva = new Reserva { Fecha = new DateOnly(2025, 1, 1), ClienteId = clienteId, EstadoReservaId = estadoId, Ambito = "Web", FechaCreacion = DateTime.Now };
            await _context.Reservas.AddAsync(reserva);
            await _context.SaveChangesAsync();

            var result = await _repository.ObtenerReservaPorIdAsync(reserva.ReservaId);

            result.Should().NotBeNull();
            result.ReservaId.Should().Be(reserva.ReservaId);
        }

        [Fact]
        public async Task ObtenerReservaPorIdAsync_ShouldReturnNull_WhenNotExists()
        {
            var result = await _repository.ObtenerReservaPorIdAsync(999);
            result.Should().BeNull();
        }

        [Fact]
        public async Task ObtenerReservasPorCliente_ShouldReturnReservas()
        {
            var (clienteId, estadoId) = await CrearClienteYEstadoAsync();
            var reserva = new Reserva { ClienteId = clienteId, EstadoReservaId = estadoId, Ambito = "Web", Fecha = new DateOnly(2025, 1, 1), FechaCreacion = DateTime.Now };
            await _context.Reservas.AddAsync(reserva);
            await _context.SaveChangesAsync();

            var result = await _repository.ObtenerReservasPorCliente(clienteId);

            result.Should().HaveCount(1);
            result.First().ClienteId.Should().Be(clienteId);
        }

        [Fact]
        public async Task CrearReservaConDetallesAsync_ShouldSaveInTransaction()
        {
            var (cancha, tarifa) = await CrearCanchaConTarifaAsync();
            var (clienteId, estadoId) = await CrearClienteYEstadoAsync();

            var reserva = new Reserva { Fecha = new DateOnly(2025, 2, 2), ClienteId = clienteId, EstadoReservaId = estadoId, Ambito = "Web", FechaCreacion = DateTime.Now };
            var detalles = new List<DetalleReserva> { new DetalleReserva { CanchaId = cancha.CanchaId, TarifaHoraId = tarifa.TarifaId, CantidadHoras = 1, Subtotal = 200 } };

            var result = await _repository.CrearReservaConDetallesAsync(reserva, detalles);

            result.ReservaId.Should().BeGreaterThan(0);
            var inDb = await _context.Reservas.Include(r => r.DetalleReservas).FirstOrDefaultAsync(r => r.ReservaId == result.ReservaId);
            inDb!.DetalleReservas.Should().HaveCount(1);
        }

        [Fact]
        public async Task ObtenerNombreCanchaAsync_ShouldReturnNombre_WhenExists()
        {
            var cancha = await CrearCanchaAsync("Cancha 1");

            var result = await _repository.ObtenerNombreCanchaAsync(cancha.CanchaId);
            result.Should().Be("Cancha 1");
        }

        [Fact]
        public async Task ObtenerNombreCanchaAsync_ShouldReturnNA_WhenNotExists()
        {
            var result = await _repository.ObtenerNombreCanchaAsync(999);
            result.Should().Be("N/A");
        }

        [Fact]
        public async Task ObtenerComplejosAsync_ShouldReturnDTOs()
        {
            var direccion = new Direccion { Calle = "Calle", Numero = "1", Ciudad = "Ciudad", Provincia = "Provincia", CodigoPostal = "1000" };
            await _context.Direccions.AddAsync(direccion);
            await _context.SaveChangesAsync();

            var complejo = new Complejo { Nombre = "Comp 1", DireccionId = direccion.DireccionId };
            await _context.Complejos.AddAsync(complejo);
            await _context.SaveChangesAsync();

            var result = await _repository.ObtenerComplejosAsync();
            result.Should().Contain(c => c.Nombre == "Comp 1");
        }

        [Fact]
        public async Task ObtenerCanchasPorComplejoAsync_ShouldReturnCanchaDTOs()
        {
            var cancha = await CrearCanchaAsync("C 1");

            var result = await _repository.ObtenerCanchasPorComplejoAsync(cancha.ComplejoId);
            result.Should().Contain(c => c.Nombre == "C 1");
        }

        [Fact]
        public async Task AgregarReserva_ShouldAddToTracker()
        {
            var reserva = new Reserva { Fecha = new DateOnly(2025, 1, 1) };
            _repository.AgregarReserva(reserva);

            _context.ChangeTracker.HasChanges().Should().BeTrue();
            _context.ChangeTracker.Entries<Reserva>().Should().HaveCount(1);
        }

        [Fact]
        public async Task AgregarDetalle_ShouldAddToTracker()
        {
            var detalle = new DetalleReserva { CanchaId = 1 };
            _repository.AgregarDetalle(detalle);

            _context.ChangeTracker.HasChanges().Should().BeTrue();
            _context.ChangeTracker.Entries<DetalleReserva>().Should().HaveCount(1);
        }

        [Fact]
        public async Task GuardarAsync_ShouldSaveChanges()
        {
            var (clienteId, estadoId) = await CrearClienteYEstadoAsync();
            var reserva = new Reserva { Fecha = new DateOnly(2025, 1, 1), ClienteId = clienteId, EstadoReservaId = estadoId, Ambito = "Web", FechaCreacion = DateTime.Now };
            _repository.AgregarReserva(reserva);
            await _repository.GuardarAsync();

            reserva.ReservaId.Should().BeGreaterThan(0);
        }
        [Fact]
        public async Task CrearReservaConDetallesAsync_ShouldThrowAndRollback_OnError()
        {
            var res = new Reserva { ClienteId = 1, FechaCreacion = DateTime.Now, Ambito = "A", Total = 1 };
            // Pass an invalid DetalleReserva to fail db update
            var detalles = new System.Collections.Generic.List<DetalleReserva> { new DetalleReserva { CanchaId = 1, TarifaHoraId = 1 } };
            // Because CanchaId 1 / TarifaHoraId 1 don't exist, FK error triggers rollback
            await Assert.ThrowsAnyAsync<Exception>(() => _repository.CrearReservaConDetallesAsync(res, detalles));
        }

        [Fact]
        public async Task ObtenerTarifaVigenteAsync_Fallback_WithoutLuz()
        {
            var tc = new TipoCancha { Nombre = "TC" }; _context.TipoCanchas.Add(tc);
            var ts = new TipoSuperficie { Nombre = "TS" }; _context.TipoSuperficies.Add(ts);
            await _context.SaveChangesAsync();
            var dir = new Direccion { CodigoPostal = "1", Provincia = "1", Calle="1", Ciudad="1", Numero="1" }; _context.Direccions.Add(dir); await _context.SaveChangesAsync();
            var com = new Complejo { Nombre = "C", DireccionId = dir.DireccionId }; _context.Complejos.Add(com); await _context.SaveChangesAsync();
            var can = new Cancha { ComplejoId = com.ComplejoId, TipoCanchaId = tc.TipoCanchaId, TipoSuperficieId = ts.TipoSuperficieId, Activa = true, Nombre = "C" }; _context.Canchas.Add(can);
            await _context.SaveChangesAsync();

            // Insert Tarifa with ContratoLuz = false but esActual = true
            _context.Tarifas.Add(new Tarifa { CanchaId = can.CanchaId, EsActual = true, ContratoLuz = false, FechaVigencia = new DateOnly(2020, 1, 1), Precio = 1 });
            await _context.SaveChangesAsync();

            var tarifa = await _repository.ObtenerTarifaVigenteAsync(can.CanchaId, new DateOnly(2025,1,1), new TimeOnly(20,0)); // night (needs luz)
            Assert.NotNull(tarifa);

            // Now test completely missing
            await Assert.ThrowsAnyAsync<Exception>(() => _repository.ObtenerTarifaVigenteAsync(can.CanchaId, new DateOnly(2019, 1, 1), new TimeOnly(12,0)));
        }

        [Fact]
        public async Task ObtenerTarifaVigenteAsync_FinalFallback_WhenOnlyInactiveTarifaExists()
        {
            // No tarifa matches CanchaId+EsActual+ContratoLuz+Fecha directly, but one exists at
            // all (EsActual=false) — exercises the fallback != null (true) side of the final if.
            var cancha = await CrearCanchaAsync();
            _context.Tarifas.Add(new Tarifa { CanchaId = cancha.CanchaId, EsActual = false, ContratoLuz = false, FechaVigencia = new DateOnly(2020, 1, 1), Precio = 55 });
            await _context.SaveChangesAsync();

            var tarifa = await _repository.ObtenerTarifaVigenteAsync(cancha.CanchaId, new DateOnly(2025, 1, 1), new TimeOnly(12, 0));

            tarifa.Should().NotBeNull();
            tarifa.Precio.Should().Be(55);
        }

        [Fact]
        public async Task ObtenerTarifaVigenteAsync_DirectMatch_Diurno()
        {
            var cancha = await CrearCanchaAsync();
            _context.Tarifas.Add(new Tarifa { CanchaId = cancha.CanchaId, EsActual = true, ContratoLuz = false, FechaVigencia = new DateOnly(2020, 1, 1), Precio = 100 });
            await _context.SaveChangesAsync();

            var tarifa = await _repository.ObtenerTarifaVigenteAsync(cancha.CanchaId, new DateOnly(2025, 1, 1), new TimeOnly(12, 0));

            tarifa.Should().NotBeNull();
            tarifa.Precio.Should().Be(100);
        }

        [Fact]
        public async Task ObtenerTarifaVigenteAsync_DirectMatch_Nocturno()
        {
            var cancha = await CrearCanchaAsync();
            _context.Tarifas.Add(new Tarifa { CanchaId = cancha.CanchaId, EsActual = true, ContratoLuz = true, FechaVigencia = new DateOnly(2020, 1, 1), Precio = 200 });
            await _context.SaveChangesAsync();

            var tarifa = await _repository.ObtenerTarifaVigenteAsync(cancha.CanchaId, new DateOnly(2025, 1, 1), new TimeOnly(20, 0));

            tarifa.Should().NotBeNull();
            tarifa.Precio.Should().Be(200);
        }

        [Fact]
        public void ObtenerTarifaVigenteEnMemoria_DirectMatch_Diurno_ReturnsExpected()
        {
            var tarifas = new List<Tarifa>
            {
                new() { CanchaId = 1, EsActual = true, ContratoLuz = false, FechaVigencia = new DateOnly(2020, 1, 1), Precio = 100 }
            };

            var result = _repository.ObtenerTarifaVigenteEnMemoria(tarifas, 1, new DateOnly(2025, 1, 1), new TimeOnly(12, 0));

            result.Precio.Should().Be(100);
        }

        [Fact]
        public void ObtenerTarifaVigenteEnMemoria_DirectMatch_Nocturno_ReturnsExpected()
        {
            var tarifas = new List<Tarifa>
            {
                new() { CanchaId = 1, EsActual = true, ContratoLuz = true, FechaVigencia = new DateOnly(2020, 1, 1), Precio = 200 }
            };

            var result = _repository.ObtenerTarifaVigenteEnMemoria(tarifas, 1, new DateOnly(2025, 1, 1), new TimeOnly(20, 0));

            result.Precio.Should().Be(200);
        }

        [Fact]
        public void ObtenerTarifaVigenteEnMemoria_NocturnoFallsBackToDiurno_WhenNoLuzTarifa()
        {
            var tarifas = new List<Tarifa>
            {
                new() { CanchaId = 1, EsActual = true, ContratoLuz = false, FechaVigencia = new DateOnly(2020, 1, 1), Precio = 100 }
            };

            var result = _repository.ObtenerTarifaVigenteEnMemoria(tarifas, 1, new DateOnly(2025, 1, 1), new TimeOnly(20, 0));

            result.Precio.Should().Be(100);
        }

        [Fact]
        public void ObtenerTarifaVigenteEnMemoria_FallsBackToAnyTarifa_WhenNoActiveMatch()
        {
            var tarifas = new List<Tarifa>
            {
                new() { CanchaId = 1, EsActual = false, ContratoLuz = false, FechaVigencia = new DateOnly(2020, 1, 1), Precio = 50 }
            };

            var result = _repository.ObtenerTarifaVigenteEnMemoria(tarifas, 1, new DateOnly(2025, 1, 1), new TimeOnly(12, 0));

            result.Precio.Should().Be(50);
        }

        [Fact]
        public void ObtenerTarifaVigenteEnMemoria_NoTarifaAtAll_Throws()
        {
            var act = () => _repository.ObtenerTarifaVigenteEnMemoria(new List<Tarifa>(), 1, new DateOnly(2025, 1, 1), new TimeOnly(12, 0));

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ObtenerTarifaVigenteEnMemoria_NocturnoFallback_IgnoresDecoysOnEveryCondition()
        {
            // Decoy rows that each fail exactly one of the ContratoLuz==false-fallback query's
            // conditions (CanchaId, EsActual, ContratoLuz, FechaVigencia), so every clause in that
            // compound Where genuinely evaluates both true and false across the data set.
            var tarifas = new List<Tarifa>
            {
                new() { CanchaId = 99, EsActual = true, ContratoLuz = false, FechaVigencia = new DateOnly(2020, 1, 1), Precio = 1 },   // wrong cancha
                new() { CanchaId = 1, EsActual = false, ContratoLuz = false, FechaVigencia = new DateOnly(2020, 1, 1), Precio = 2 },    // not activa
                new() { CanchaId = 1, EsActual = true, ContratoLuz = false, FechaVigencia = new DateOnly(2030, 1, 1), Precio = 4 },     // not vigente todavía
                new() { CanchaId = 1, EsActual = true, ContratoLuz = false, FechaVigencia = new DateOnly(2020, 1, 1), Precio = 100 }    // el match real
            };

            var result = _repository.ObtenerTarifaVigenteEnMemoria(tarifas, 1, new DateOnly(2025, 1, 1), new TimeOnly(20, 0));

            result.Precio.Should().Be(100);
        }

        [Fact]
        public void ObtenerTarifaVigenteEnMemoria_FinalFallback_IgnoresDecoysOnEveryCondition()
        {
            // Same idea for the last-resort fallback query (line ~127), whose predicate only
            // checks CanchaId and FechaVigencia — decoys exercise both sides of each.
            var tarifas = new List<Tarifa>
            {
                new() { CanchaId = 99, EsActual = false, ContratoLuz = true, FechaVigencia = new DateOnly(2020, 1, 1), Precio = 1 },  // wrong cancha
                new() { CanchaId = 1, EsActual = false, ContratoLuz = true, FechaVigencia = new DateOnly(2030, 1, 1), Precio = 2 },   // not vigente todavía
                new() { CanchaId = 1, EsActual = false, ContratoLuz = true, FechaVigencia = new DateOnly(2020, 1, 1), Precio = 77 }   // el match real (fallback puro)
            };

            var result = _repository.ObtenerTarifaVigenteEnMemoria(tarifas, 1, new DateOnly(2025, 1, 1), new TimeOnly(12, 0));

            result.Precio.Should().Be(77);
        }
    }
}
