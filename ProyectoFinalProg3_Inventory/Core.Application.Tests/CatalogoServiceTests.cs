using Application.DTOs.Catalogo;
using Application.Services;
using Business.Inventory.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Tests;

public class CatalogoServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CrearProveedor_Valido_GuardaYDevuelveId()
    {
        // Preparar
        using var context = CreateContext();
        var service = new CatalogoService(context);

        // Actuar
        var creado = await service.CrearProveedorAsync(new CrearProveedorDto(
            "Distribuidora Central", "001-0000000-1", "809-555-0000", "ventas@central.com"));

        // Verificar
        Assert.True(creado.Id > 0);
        Assert.Equal("Distribuidora Central", creado.Nombre);
        Assert.Equal("ventas@central.com", creado.Email);
        Assert.Equal(1, await context.Proveedores.CountAsync());
    }

    [Fact]
    public async Task CrearProveedor_SinNombre_LanzaInvalidOperation()
    {
        // Preparar
        using var context = CreateContext();
        var service = new CatalogoService(context);

        // Actuar y verificar: RD-07 devuelve un mensaje claro.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CrearProveedorAsync(new CrearProveedorDto("  ", null, null, null)));

        Assert.Contains("nombre", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CrearProveedor_EmailInvalido_LanzaInvalidOperation()
    {
        // Preparar
        using var context = CreateContext();
        var service = new CatalogoService(context);

        // Actuar y verificar
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CrearProveedorAsync(new CrearProveedorDto(
                "Proveedor", null, null, "no-es-un-correo")));

        Assert.Contains("correo", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CrearProducto_ProveedorInexistente_LanzaMensajeClaro()
    {
        // Preparar: catálogo vacío.
        using var context = CreateContext();
        var service = new CatalogoService(context);

        var dto = new CrearProductoDto("Monitor", "MON-01", 100m, 5, 1, 99);

        // Actuar y verificar
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CrearProductoAsync(dto));

        Assert.Contains("99", ex.Message);
        Assert.Contains("no existe", ex.Message);
    }

    [Fact]
    public async Task CrearProducto_Valido_GeneraCodigoSiFalta()
    {
        // Preparar: un proveedor existente.
        using var context = CreateContext();
        context.Proveedores.Add(new Proveedor { Id = 1, Nombre = "Proveedor" });
        await context.SaveChangesAsync();
        var service = new CatalogoService(context);

        // Actuar: sin código.
        var creado = await service.CrearProductoAsync(
            new CrearProductoDto("Monitor", null, 100m, 5, 1, 1));

        // Verificar: se autogenera un código y se guarda.
        Assert.True(creado.Id > 0);
        Assert.StartsWith("PROD-", creado.Codigo);
        Assert.Equal("Proveedor", creado.ProveedorNombre);
    }

    [Fact]
    public async Task CrearProducto_PrecioNegativo_LanzaInvalidOperation()
    {
        // Preparar: un proveedor existente.
        using var context = CreateContext();
        context.Proveedores.Add(new Proveedor { Id = 1, Nombre = "Proveedor" });
        await context.SaveChangesAsync();
        var service = new CatalogoService(context);

        // Actuar y verificar
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CrearProductoAsync(
                new CrearProductoDto("Monitor", "MON-01", -1m, 5, 1, 1)));

        Assert.Contains("precio", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CrearProducto_CodigoDuplicado_LanzaInvalidOperation()
    {
        // Preparar: un producto ya usa el código.
        using var context = CreateContext();
        context.Proveedores.Add(new Proveedor { Id = 1, Nombre = "Proveedor" });
        context.Productos.Add(new Producto
        {
            Id = 1,
            Nombre = "Monitor",
            Codigo = "MON-01",
            ProveedorId = 1
        });
        await context.SaveChangesAsync();
        var service = new CatalogoService(context);

        // Actuar y verificar
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CrearProductoAsync(
                new CrearProductoDto("Monitor 2", "MON-01", 100m, 5, 1, 1)));

        Assert.Contains("MON-01", ex.Message);
    }

    [Fact]
    public async Task ListarProductos_IncluyeNombreDelProveedor()
    {
        // Preparar: un producto con su proveedor.
        using var context = CreateContext();
        context.Proveedores.Add(new Proveedor { Id = 1, Nombre = "Distribuidora" });
        context.Productos.Add(new Producto
        {
            Id = 1,
            Nombre = "Teclado",
            Codigo = "TEC-01",
            Precio = 25m,
            ProveedorId = 1
        });
        await context.SaveChangesAsync();
        var service = new CatalogoService(context);

        // Actuar
        var productos = await service.ListarProductosAsync();

        // Verificar
        var producto = Assert.Single(productos);
        Assert.Equal("Teclado", producto.Nombre);
        Assert.Equal("Distribuidora", producto.ProveedorNombre);
    }
}
