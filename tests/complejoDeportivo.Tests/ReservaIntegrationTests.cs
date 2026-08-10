using complejoDeportivo.DTOs;
using complejoDeportivo.Models;
using complejoDeportivo.Repositories;
using complejoDeportivo.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace complejoDeportivo.Tests;

public class ReservaIntegrationTests
{
    [Fact]
    public async Task CrearReserva_MultiHora_AplicaTarifasPorFranjaYGuardaDetalle()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();

        var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(10));
        await SeedReservaBaseAsync(context, fecha);

        var service = new ReservaServicie(new ReservaRepository(context), context);

        var reserva = await service.CrearReserva(new CrearReservaDTO
        {
            ClienteId = 1,
            CanchaIds = new List<int> { 1 },
            Fecha = fecha,
            HoraInicio = new TimeOnly(18, 0),
            HoraFin = new TimeOnly(21, 0),
            Ambito = "Test"
        });

        Assert.Equal(400m, reserva.Total);
        var detalle = Assert.Single(reserva.Detalles);
        Assert.Equal(3, detalle.CantidadHoras);
        Assert.Equal(400m, detalle.Subtotal);

        Assert.Equal(1, await context.Reservas.CountAsync());
        Assert.Equal(1, await context.DetalleReservas.CountAsync());
    }

    [Fact]
    public async Task CrearReserva_EnFranjaNocturna_CobraTarifaConLuz()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();

        var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(10));
        await SeedReservaBaseAsync(context, fecha);

        var service = new ReservaServicie(new ReservaRepository(context), context);

        var reserva = await service.CrearReserva(new CrearReservaDTO
        {
            ClienteId = 1,
            CanchaIds = new List<int> { 1 },
            Fecha = fecha,
            HoraInicio = new TimeOnly(19, 0),
            HoraFin = new TimeOnly(21, 0),
            Ambito = "Test"
        });

        Assert.Equal(300m, reserva.Total);
        var detalle = Assert.Single(reserva.Detalles);
        Assert.Equal(2, detalle.CantidadHoras);
        Assert.Equal(300m, detalle.Subtotal);
    }

    private static ComplejoDeportivoContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ComplejoDeportivoContext>()
            .UseSqlite(connection)
            .Options;

        return new ComplejoDeportivoContext(options);
    }

    private static async Task SeedReservaBaseAsync(ComplejoDeportivoContext context, DateOnly fechaTarifa)
    {
        context.Direccions.Add(new Direccion
        {
            DireccionId = 1,
            Calle = "Siempre Viva",
            Numero = "742",
            Ciudad = "Springfield",
            Provincia = "Springfield",
            CodigoPostal = "1000"
        });

        context.Complejos.Add(new Complejo
        {
            ComplejoId = 1,
            Nombre = "Complejo Central",
            DireccionId = 1
        });

        context.TipoCanchas.Add(new TipoCancha { TipoCanchaId = 1, Nombre = "Futbol 5" });
        context.TipoSuperficies.Add(new TipoSuperficie { TipoSuperficieId = 1, Nombre = "Sintetico" });

        context.Canchas.Add(new Cancha
        {
            CanchaId = 1,
            ComplejoId = 1,
            TipoCanchaId = 1,
            TipoSuperficieId = 1,
            Nombre = "Cancha 1",
            Activa = true
        });

        context.Clientes.Add(new Cliente
        {
            ClienteId = 1,
            Nombre = "Ana",
            Apellido = "Gomez",
            Email = "ana@example.com",
            Telefono = "123456",
            Documento = "30111222",
            FechaRegistro = DateTime.UtcNow
        });

        context.EstadoReservas.Add(new EstadoReserva
        {
            EstadoReservaId = 1,
            Nombre = "Confirmada"
        });

        context.Tarifas.AddRange(
            new Tarifa
            {
                TarifaId = 1,
                CanchaId = 1,
                Precio = 100m,
                ContratoLuz = false,
                FechaVigencia = fechaTarifa,
                EsActual = true
            },
            new Tarifa
            {
                TarifaId = 2,
                CanchaId = 1,
                Precio = 150m,
                ContratoLuz = true,
                FechaVigencia = fechaTarifa,
                EsActual = true
            }
        );

        await context.SaveChangesAsync();
    }
}
