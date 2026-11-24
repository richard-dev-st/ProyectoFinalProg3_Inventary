using Application.DTOs.Divisa;
using Application.Exceptions;
using Application.Interfaces;
using Application.Services;
using Business.Inventory.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Tests;

public class OrdenCompraServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private sealed class RepoFalso : IOrdenCompraRepository
    {
        private readonly OrdenCompra? _orden;

        public RepoFalso(OrdenCompra? orden) => _orden = orden;

        public Task<OrdenCompra> GetByIdAsync(int id) => Task.FromResult(_orden!);
        public Task AddAsync(OrdenCompra orden) => Task.CompletedTask;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class TasaFalsa : ITasaCambioService
    {
        private readonly decimal _tasa;
        private readonly Exception? _error;

        public TasaFalsa(decimal tasa, Exception? error = null)
        {
            _tasa = tasa;
            _error = error;
        }

        public Task<TasaCambioDto> ObtenerTasaAsync(
            string origen,
            string destino,
            CancellationToken ct = default)
            => _error is null
                ? Task.FromResult(new TasaCambioDto(_tasa, origen, destino, DateTime.UtcNow))
                : Task.FromException<TasaCambioDto>(_error);
    }

    [Fact]
    public async Task OrdenInexistente_LanzaInvalidOperation()
    {
        using var context = CreateContext();

        var service = new OrdenCompraService(
            new RepoFalso(null),
            new TasaFalsa(59m),
            context,
            new NotificacionService(context));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ObtenerTotalEnDopAsync(1));
    }

    [Fact]
    public async Task OrdenExistente_ConvierteYRedondea()
    {
        using var context = CreateContext();

        var orden = new OrdenCompra { Id = 7, Total = 10.01m };

        var service = new OrdenCompraService(
            new RepoFalso(orden),
            new TasaFalsa(59.427m),
            context,
            new NotificacionService(context));

        var resultado = await service.ObtenerTotalEnDopAsync(7);

        Assert.Equal(7, resultado.OrdenId);
        Assert.Equal(10.01m, resultado.TotalUsd);
        Assert.Equal(59.427m, resultado.Tasa);
        Assert.Equal(594.86m, resultado.TotalDop); // 10.01 * 59.427 = 594.86427
    }

    [Fact]
    public async Task ErrorExterno_SePropaga()
    {
        using var context = CreateContext();

        var orden = new OrdenCompra { Id = 7, Total = 100m };
        var error = new ServicioExternoException(
            "La clave de la API externa no está configurada.",
            ServicioExternoException.Causa.NoConfigurado);

        var service = new OrdenCompraService(
            new RepoFalso(orden),
            new TasaFalsa(0m, error),
            context,
            new NotificacionService(context));

        var ex = await Assert.ThrowsAsync<ServicioExternoException>(
            () => service.ObtenerTotalEnDopAsync(7));

        Assert.Equal(ServicioExternoException.Causa.NoConfigurado, ex.Motivo);
    }

    [Fact]
    public async Task ListarAsync_Estandar_SoloDevuelveSusPropiasOrdenes()
    {
        // Preparar: dos órdenes de usuarios distintos.
        using var context = CreateContext();
        var usuario = Guid.NewGuid();
        var otroUsuario = Guid.NewGuid();
        context.OrdenesCompra.AddRange(
            new OrdenCompra { Id = 1, CreadoPorUsuarioId = usuario, Total = 10m },
            new OrdenCompra { Id = 2, CreadoPorUsuarioId = otroUsuario, Total = 20m });
        await context.SaveChangesAsync();

        var service = new OrdenCompraService(
            new RepoFalso(null),
            new TasaFalsa(59m),
            context,
            new NotificacionService(context));

        // Actuar: listar como Estándar.
        var ordenes = await service.ListarAsync(usuario, esAdministrador: false);

        // Verificar: solo aparece la orden propia.
        Assert.Single(ordenes);
        Assert.Equal(1, ordenes[0].Id);
        Assert.Equal("Borrador", ordenes[0].Estado);
    }

    [Fact]
    public async Task ListarAsync_Administrador_DevuelveTodasLasOrdenes()
    {
        // Preparar: dos órdenes de usuarios distintos.
        using var context = CreateContext();
        context.OrdenesCompra.AddRange(
            new OrdenCompra { Id = 1, CreadoPorUsuarioId = Guid.NewGuid(), Total = 10m },
            new OrdenCompra { Id = 2, CreadoPorUsuarioId = Guid.NewGuid(), Total = 20m });
        await context.SaveChangesAsync();

        var service = new OrdenCompraService(
            new RepoFalso(null),
            new TasaFalsa(59m),
            context,
            new NotificacionService(context));

        // Actuar: listar como Administrador.
        var ordenes = await service.ListarAsync(Guid.NewGuid(), esAdministrador: true);

        // Verificar: aparecen las dos, la más reciente primero.
        Assert.Equal(2, ordenes.Count);
        Assert.Equal(2, ordenes[0].Id);
    }
}
