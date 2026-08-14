using System;
using System.Linq;
using System.Threading.Tasks;
using ComplejoDeportivo.Application.DTOs.Dashboard;
using ComplejoDeportivo.Domain;
using ComplejoDeportivo.Infrastructure.Persistence;
using ComplejoDeportivo.Infrastructure.Repositories.Dashboard;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ComplejoDeportivo.Tests.Infrastructure.Repositories
{
    public class DashboardRepositoryTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
    {
        private readonly DatabaseFixture _fixture;
        private ComplejoDeportivoContext _context = null!;
        private DashboardRepository _repository = null!;

        public DashboardRepositoryTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public Task InitializeAsync()
        {
            _context = new ComplejoDeportivoContext(_fixture.Options);
            _repository = new DashboardRepository(_context);
            
            _context.Database.BeginTransaction();
            
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            if (_context.Database.CurrentTransaction != null)
            {
                await _context.Database.CurrentTransaction.RollbackAsync();
            }
            await _context.DisposeAsync();
        }
        
        private async Task<(int ComplejoId, int ClienteId, int CanchaId)> SeedDataAsync()
        {
            var dir = new Direccion { Calle = "C", Ciudad = "C", Numero = "1", CodigoPostal = "1", Provincia = "P" };
            _context.Direccions.Add(dir);
            await _context.SaveChangesAsync();
            
            var complejo = new Complejo { Nombre = "Comp1", DireccionId = dir.DireccionId };
            _context.Complejos.Add(complejo);
            
            var cliente = new Cliente { Nombre = "Cli", Apellido = "1", Email = "cli@a.com", Telefono = "1", Documento = "1", FechaRegistro = DateTime.Now };
            _context.Clientes.Add(cliente);
            
            var tipoCancha = new TipoCancha { Nombre = "F5" };
            var tipoSup = new TipoSuperficie { Nombre = "Cesped" };
            var estado = new EstadoReserva { Nombre = "Pendiente" };
            _context.TipoCanchas.Add(tipoCancha);
            _context.TipoSuperficies.Add(tipoSup);
            _context.EstadoReservas.Add(estado);
            await _context.SaveChangesAsync();
            
            var cancha = new Cancha { Nombre = "C1", ComplejoId = complejo.ComplejoId, TipoCanchaId = tipoCancha.TipoCanchaId, TipoSuperficieId = tipoSup.TipoSuperficieId, Activa = true };
            _context.Canchas.Add(cancha);
            await _context.SaveChangesAsync();
            
            var tarifa = new Tarifa { Precio = 100, Cancha = cancha, ContratoLuz = false, FechaVigencia = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), EsActual = true };
            _context.Tarifas.Add(tarifa);
            await _context.SaveChangesAsync();
            
            var reserva = new Reserva 
            { 
                ClienteId = cliente.ClienteId, 
                EstadoReservaId = estado.EstadoReservaId, 
                Fecha = DateOnly.FromDateTime(DateTime.Today), 
                HoraInicio = new TimeOnly(10, 0), 
                HoraFin = new TimeOnly(11, 0), 
                FechaCreacion = DateTime.Now,
                Total = 100,
                Ambito = "Web"
            };
            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();
            
            var detalle = new DetalleReserva 
            { 
                ReservaId = reserva.ReservaId, 
                CanchaId = cancha.CanchaId, 
                TarifaHoraId = tarifa.TarifaId, 
                Subtotal = 100,
                CantidadHoras = 1
            };
            _context.DetalleReservas.Add(detalle);
            await _context.SaveChangesAsync();
            
            return (complejo.ComplejoId, cliente.ClienteId, cancha.CanchaId);
        }

        [Fact]
        public async Task GetResumenAsync_WhenComplejoHasNoCanchas_DefaultsInfraestructuraToZero()
        {
            // Complejo with zero canchas makes the GroupBy query return no groups, so
            // FirstOrDefaultAsync yields null and the `canchas ?? new { Totales = 0, Activas = 0 }`
            // fallback actually runs (as opposed to every other test, which always seeds a cancha).
            var dir = new Direccion { Calle = "C", Ciudad = "C", Numero = "1", CodigoPostal = "1", Provincia = "P" };
            _context.Direccions.Add(dir);
            await _context.SaveChangesAsync();

            var complejo = new Complejo { Nombre = "Sin Canchas", DireccionId = dir.DireccionId };
            _context.Complejos.Add(complejo);
            await _context.SaveChangesAsync();

            var filtros = new FiltrosDashboardDto { ComplejoId = complejo.ComplejoId };

            var result = await _repository.GetResumenAsync(filtros);

            result.CanchasTotales.Should().Be(0);
            result.CanchasActivas.Should().Be(0);
        }

        [Fact]
        public async Task GetResumenAsync_WithConfirmadaAndCanceladaEstados_MapsEachCount()
        {
            // SeedDataAsync only ever produces a "Pendiente" reserva, so ReservasConfirmadas and
            // ReservasCanceladas always take the "not found -> ?? 0" branch of their
            // `estados.FirstOrDefault(...)?.Cantidad ?? 0` lookup. Seeding one reserva in each of
            // the other two states exercises the "found" side for all three.
            var dir = new Direccion { Calle = "C", Ciudad = "C", Numero = "1", CodigoPostal = "1", Provincia = "P" };
            _context.Direccions.Add(dir);
            var complejo = new Complejo { Nombre = "ComEstados", Direccion = dir };
            _context.Complejos.Add(complejo);
            var cliente = new Cliente { Nombre = "Cli", Apellido = "1", Email = "cli-estados@a.com", Telefono = "2", Documento = "2", FechaRegistro = DateTime.Now };
            _context.Clientes.Add(cliente);
            var tipoCancha = new TipoCancha { Nombre = "F5b" };
            var tipoSup = new TipoSuperficie { Nombre = "Cespedb" };
            var estadoConfirmada = new EstadoReserva { Nombre = "Confirmada" };
            var estadoCancelada = new EstadoReserva { Nombre = "Cancelada" };
            _context.TipoCanchas.Add(tipoCancha);
            _context.TipoSuperficies.Add(tipoSup);
            _context.EstadoReservas.AddRange(estadoConfirmada, estadoCancelada);
            await _context.SaveChangesAsync();

            var cancha = new Cancha { Nombre = "C2", ComplejoId = complejo.ComplejoId, TipoCanchaId = tipoCancha.TipoCanchaId, TipoSuperficieId = tipoSup.TipoSuperficieId, Activa = true };
            _context.Canchas.Add(cancha);
            await _context.SaveChangesAsync();

            var tarifa = new Tarifa { Precio = 100, Cancha = cancha, ContratoLuz = false, FechaVigencia = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), EsActual = true };
            _context.Tarifas.Add(tarifa);
            await _context.SaveChangesAsync();

            var reservaConfirmada = new Reserva { ClienteId = cliente.ClienteId, EstadoReservaId = estadoConfirmada.EstadoReservaId, Fecha = DateOnly.FromDateTime(DateTime.Today), HoraInicio = new TimeOnly(9, 0), HoraFin = new TimeOnly(10, 0), FechaCreacion = DateTime.Now, Total = 50, Ambito = "Web" };
            var reservaCancelada = new Reserva { ClienteId = cliente.ClienteId, EstadoReservaId = estadoCancelada.EstadoReservaId, Fecha = DateOnly.FromDateTime(DateTime.Today), HoraInicio = new TimeOnly(11, 0), HoraFin = new TimeOnly(12, 0), FechaCreacion = DateTime.Now, Total = 50, Ambito = "Web" };
            _context.Reservas.AddRange(reservaConfirmada, reservaCancelada);
            await _context.SaveChangesAsync();

            // AplicarFiltros requires each Reserva to link to a Cancha via DetalleReserva to match ComplejoId.
            _context.DetalleReservas.AddRange(
                new DetalleReserva { ReservaId = reservaConfirmada.ReservaId, CanchaId = cancha.CanchaId, TarifaHoraId = tarifa.TarifaId, Subtotal = 50, CantidadHoras = 1 },
                new DetalleReserva { ReservaId = reservaCancelada.ReservaId, CanchaId = cancha.CanchaId, TarifaHoraId = tarifa.TarifaId, Subtotal = 50, CantidadHoras = 1 }
            );
            await _context.SaveChangesAsync();

            var filtros = new FiltrosDashboardDto { ComplejoId = complejo.ComplejoId };

            var result = await _repository.GetResumenAsync(filtros);

            result.ReservasConfirmadas.Should().Be(1);
            result.ReservasCanceladas.Should().Be(1);
            result.ReservasPendientes.Should().Be(0);
        }

        [Fact]
        public async Task GetResumenAsync_ShouldReturnResumen()
        {
            var ids = await SeedDataAsync();
            var filtros = new FiltrosDashboardDto { ComplejoId = ids.ComplejoId, Desde = DateTime.Today.AddDays(-1), Hasta = DateTime.Today.AddDays(1) };
            
            var result = await _repository.GetResumenAsync(filtros);
            
            result.Should().NotBeNull();
            result.IngresosHoy.Should().Be(100);
            result.ReservasHoy.Should().Be(1);
            result.CanchasTotales.Should().Be(1);
            result.CanchasActivas.Should().Be(1);
            result.ReservasPendientes.Should().Be(1);
        }

        [Fact]
        public async Task GetIngresosPorPeriodoAsync_Diario_ShouldReturnIngresos()
        {
            var ids = await SeedDataAsync();
            var filtros = new FiltrosDashboardDto { ComplejoId = ids.ComplejoId, Desde = DateTime.Today.AddDays(-1), Hasta = DateTime.Today.AddDays(1) };
            
            var result = await _repository.GetIngresosPorPeriodoAsync(filtros, "diario");
            
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Total.Should().Be(100);
            result.First().CantidadReservas.Should().Be(1);
            result.First().CantidadClientes.Should().Be(1);
        }

        [Fact]
        public async Task GetIngresosPorPeriodoAsync_Mensual_ShouldReturnIngresos()
        {
            var ids = await SeedDataAsync();
            var filtros = new FiltrosDashboardDto { ComplejoId = ids.ComplejoId, Desde = DateTime.Today.AddDays(-1), Hasta = DateTime.Today.AddDays(1) };
            
            var result = await _repository.GetIngresosPorPeriodoAsync(filtros, "mensual");
            
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Total.Should().Be(100);
        }
        
        [Fact]
        public async Task GetEstadosReservasAsync_ShouldReturnEstados()
        {
            var ids = await SeedDataAsync();
            var filtros = new FiltrosDashboardDto { ComplejoId = ids.ComplejoId, Desde = DateTime.Today.AddDays(-1), Hasta = DateTime.Today.AddDays(1) };
            
            var result = await _repository.GetEstadosReservasAsync(filtros);
            
            result.Should().NotBeNull();
            result.Should().ContainSingle();
            result.First().Estado.Should().Be("Pendiente");
            result.First().Cantidad.Should().Be(1);
            result.First().Porcentaje.Should().Be(100);
        }

        [Fact]
        public async Task GetCanchasPopularesAsync_ShouldReturnCanchas()
        {
            var ids = await SeedDataAsync();
            var filtros = new FiltrosDashboardDto { ComplejoId = ids.ComplejoId, Desde = DateTime.Today.AddDays(-1), Hasta = DateTime.Today.AddDays(1) };
            
            var result = await _repository.GetCanchasPopularesAsync(filtros);
            
            result.Should().NotBeNull();
            result.Should().ContainSingle();
            result.First().Nombre.Should().Be("C1");
            result.First().ReservasCount.Should().Be(1);
        }
        
        [Fact]
        public async Task GetClientesFrecuentesAsync_ShouldReturnClientes()
        {
            var ids = await SeedDataAsync();
            var filtros = new FiltrosDashboardDto { ComplejoId = ids.ComplejoId, Desde = DateTime.Today.AddDays(-1), Hasta = DateTime.Today.AddDays(1) };
            
            var result = await _repository.GetClientesFrecuentesAsync(filtros);
            
            result.Should().NotBeNull();
            result.Should().ContainSingle();
            result.First().NombreCompleto.Should().Be("Cli 1");
            result.First().TotalReservas.Should().Be(1);
        }

        [Fact]
        public async Task GetReservasRecientesAsync_ShouldReturnReservas()
        {
            var ids = await SeedDataAsync();
            var filtros = new FiltrosDashboardDto { ComplejoId = ids.ComplejoId, Desde = DateTime.Today.AddDays(-1), Hasta = DateTime.Today.AddDays(1) };
            
            var result = await _repository.GetReservasRecientesAsync(filtros);
            
            result.Should().NotBeNull();
            result.Should().ContainSingle();
            result.First().CanchaNombre.Should().Be("C1");
            result.First().Estado.Should().Be("Pendiente");
        }
        
        [Fact]
        public async Task GetOcupacionPorHorarioAsync_ShouldReturnOcupacion()
        {
            var ids = await SeedDataAsync();
            var filtros = new FiltrosDashboardDto { ComplejoId = ids.ComplejoId, Desde = DateTime.Today.AddDays(-1), Hasta = DateTime.Today.AddDays(1) };
            
            var result = await _repository.GetOcupacionPorHorarioAsync(filtros);
            
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
        }
        
        [Fact]
        public async Task GetAlertasStockAsync_ShouldReturnAlertas()
        {
            var ids = await SeedDataAsync();
            var filtros = new FiltrosDashboardDto { ComplejoId = ids.ComplejoId, Desde = DateTime.Today.AddDays(-1), Hasta = DateTime.Today.AddDays(1) };
            
            var result = await _repository.GetAlertasStockAsync(filtros);
            
            result.Should().NotBeNull();
        }
        [Fact]
        public async Task GetResumenAsync_WithAllFilters_ShouldApplyFilters()
        {
            var filters = new FiltrosDashboardDto
            {
                ComplejoId = 1,
                CanchaId = 1,
                TipoCanchaId = 1,
                EstadoReservaId = 1,
                ClienteId = 1,
                Desde = DateTime.Today.AddDays(-10),
                Hasta = DateTime.Today.AddDays(10)
            };
            var resumen = await _repository.GetResumenAsync(filters);
            resumen.Should().NotBeNull();
        }
    }
}
