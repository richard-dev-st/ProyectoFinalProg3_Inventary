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
            context);

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
            context);

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
            context);

        var ex = await Assert.ThrowsAsync<ServicioExternoException>(
            () => service.ObtenerTotalEnDopAsync(7));

        Assert.Equal(ServicioExternoException.Causa.NoConfigurado, ex.Motivo);
    }
}
