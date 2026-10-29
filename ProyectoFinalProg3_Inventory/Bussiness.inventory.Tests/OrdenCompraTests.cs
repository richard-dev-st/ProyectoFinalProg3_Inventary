using Application.Services;
using Business.Inventory.Entities;
using Core.Domain.Entities.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bussiness.inventory.Tests;

public class OrdenCompraTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    //TRANSICIONES PERMITIDAS
    [Fact]
    public void Borrador_EnviarAAprobacion_PasaAPendienteAprobacion()
    {
        //Arrange
        var orden = new OrdenCompra
        {
            ProveedorId = 5,
            CreadoPorUsuarioId = Guid.NewGuid(),
            Detalles = new List<DetalleOrden>
            {
                new DetalleOrden
                {
                    ProductoId = 2,
                    Cantidad = 10,
                    Precio = 100
                }
            }
        };

        //Act
        orden.EnviarAAprobacion();

        //Assert
        Assert.Equal(EstadoOrden.PendienteAprobacion, orden.Estado);
    }

    [Fact]
    public void PendienteAprobacion_Aprobar_PasaAAprobada()
    {
        //Arrange
        var orden = new OrdenCompra
        {
            ProveedorId = 6,
            CreadoPorUsuarioId = Guid.NewGuid(),
            Detalles = new List<DetalleOrden>
            {
                new() { ProductoId = 3, Cantidad = 4, Precio = 100000}
            }
        };
        orden.EnviarAAprobacion();

        //Act
        orden.Aprobar();

        //Assert
        Assert.Equal(EstadoOrden.Aprobada, orden.Estado);
    }

    [Fact]
    public void Aprobada_Recibir_PasaARecibida()
    {
        //Arrange
        var orden = new OrdenCompra
        {
            ProveedorId = 7,
            CreadoPorUsuarioId = Guid.NewGuid(),
            Detalles = new List<DetalleOrden>
            {
                new() { ProductoId = 4, Cantidad = 1, Precio = 100}
            }
        };
        orden.EnviarAAprobacion();
        orden.Aprobar();

        //Act
        orden.Recibir();

        //Assert
        Assert.Equal(EstadoOrden.Recibida, orden.Estado);
    }

    //TRANSICIONES PROHIBIDAS
    [Fact]
    public void Borrador_Aprobar_LanzaInvalidOperationException_YEstaNoCambia()
    {
        //Arrange
        var orden = new OrdenCompra
        {
            ProveedorId = 8,
            CreadoPorUsuarioId = Guid.NewGuid(),
            Detalles = new List<DetalleOrden>
            {
                new() { ProductoId = 5, Cantidad = 1,Precio = 100}
            }
        };

        //Act
        var ex = Assert.Throws<InvalidOperationException>(() => orden.Aprobar());

        //Assert
        Assert.Equal(EstadoOrden.Borrador, orden.Estado);
        Assert.Contains("Transición inválida", ex.Message);
    }

    //ESTADO TERMINAL
    [Fact]
    public void Recibida_Recibir_LanzaInvalidOperationException_YEstaNoCambia()
    {
        //Arrange
        var orden = new OrdenCompra
        {
            ProveedorId = 9,
            CreadoPorUsuarioId = Guid.NewGuid(),
            Detalles = new List<DetalleOrden>
            {
                new() { ProductoId = 6, Cantidad = 1,Precio = 100}
            }
        };
        orden.EnviarAAprobacion();
        orden.Aprobar();
        orden.Recibir();

        //Act
        var ex = Assert.Throws<InvalidOperationException>(() => orden.Recibir());

        //Assert
        Assert.Equal(EstadoOrden.Recibida, orden.Estado);
        Assert.Contains("Transición inválida", ex.Message);
    }

    [Fact]
    public void Borrador_Cancelar_PasaACancelada()
    {
        //Arrange
        var orden = new OrdenCompra
        {
            ProveedorId = 10,
            CreadoPorUsuarioId = Guid.NewGuid(),
            Detalles = new List<DetalleOrden>
            {
                new() { ProductoId = 7, Cantidad = 1, Precio = 50}
            }
        };

        //Act
        orden.Cancelar();

        //Assert
        Assert.Equal(EstadoOrden.Cancelada, orden.Estado);
    }

    [Fact]
    public void PendienteAprobacion_Cancelar_PasaACancelada()
    {
        //Arrange
        var orden = new OrdenCompra
        {
            ProveedorId = 11,
            CreadoPorUsuarioId = Guid.NewGuid(),
            Detalles = new List<DetalleOrden>
            {
                new() { ProductoId = 8, Cantidad = 1, Precio = 75}
            }
        };
        orden.EnviarAAprobacion();

        //Act
        orden.Cancelar();

        //Assert
        Assert.Equal(EstadoOrden.Cancelada, orden.Estado);
    }

    [Fact]
    public void Aprobada_Cancelar_PasaACancelada()
    {
        //Arrange
        var orden = new OrdenCompra
        {
            ProveedorId = 12,
            CreadoPorUsuarioId = Guid.NewGuid(),
            Detalles = new List<DetalleOrden>
            {
                new() { ProductoId = 9, Cantidad = 4, Precio = 40}
            }
        };
        orden.EnviarAAprobacion();
        orden.Aprobar();

        //Act
        orden.Cancelar();

        //Assert
        Assert.Equal(EstadoOrden.Cancelada, orden.Estado);
    }

    [Fact]
    public void Cancelada_EnviarAAprobacion_LanzaInvalidOperationException_YElEstadoNoCambia()
    {
        //Arrange
        var orden = new OrdenCompra
        {
            ProveedorId = 13,
            CreadoPorUsuarioId = Guid.NewGuid(),
            Detalles = new List<DetalleOrden>
            {
                new() { ProductoId = 10, Cantidad = 1, Precio = 120}
            }
        };
        orden.Cancelar();

        //Act
        var ex = Assert.Throws<InvalidOperationException>(() => orden.EnviarAAprobacion());

        //Assert
        Assert.Equal(EstadoOrden.Cancelada, orden.Estado);
        Assert.Contains("Transición inválida", ex.Message);

    }

    [Fact]
    public void Borrador_Recibir_LanzaInvalidOperationException_YElEstadoNoCambia()
    {
        //Arrange
        var orden = new OrdenCompra
        {
            ProveedorId = 14,
            CreadoPorUsuarioId = Guid.NewGuid(),
            Detalles = new List<DetalleOrden>
            {
                new() { ProductoId = 11, Cantidad = 6, Precio = 230}
            }
        };

        //Act
        var ex = Assert.Throws<InvalidOperationException>(() => orden.Recibir());

        //Assert
        Assert.Equal(EstadoOrden.Borrador, orden.Estado);
        Assert.Contains("Transición inválida", ex.Message);
    }

    [Fact]
    public void PendienteAprobacion_Recibir_LanzaInvalidOperationException_YElEstadoNoCambia()
    {
        //Arrange
        var orden = new OrdenCompra
        {
            ProveedorId = 15,
            CreadoPorUsuarioId = Guid.NewGuid(),
            Detalles = new List<DetalleOrden>
            {
                new() { ProductoId = 12, Cantidad = 6, Precio = 230}
            }
        };
        orden.EnviarAAprobacion();

        //Act
        var ex = Assert.Throws<InvalidOperationException>(() => orden.Recibir());

        //Assert
        Assert.Equal(EstadoOrden.PendienteAprobacion, orden.Estado);
        Assert.Contains("Transición inválida", ex.Message);
    }

    [Fact]
    public void Aprobada_EnviarAAprobacion_LanzaInvalidOperationException_YElEstadoNoCambia()
    {
        //Arrange
        var orden = new OrdenCompra
        {
            ProveedorId = 16,
            CreadoPorUsuarioId = Guid.NewGuid(),
            Detalles = new List<DetalleOrden>
            {
                new() { ProductoId = 13, Cantidad = 7, Precio = 236}
            }
        };
        orden.EnviarAAprobacion();
        orden.Aprobar();

        //Act
        var ex = Assert.Throws<InvalidOperationException>(() => orden.EnviarAAprobacion());

        //Assert
        Assert.Equal(EstadoOrden.Aprobada, orden.Estado);
        Assert.Contains("Transición inválida", ex.Message);
    }

    [Fact]
    public void Aprobada_Aprobar_LanzaInvalidOperationException_YElEstadoNoCambia()
    {
        //Arrange
        var orden = new OrdenCompra
        {
            ProveedorId = 17,
            CreadoPorUsuarioId = Guid.NewGuid(),
            Detalles = new List<DetalleOrden>
            {
                new() { ProductoId = 14, Cantidad = 8, Precio = 240}
            }
        };
        orden.EnviarAAprobacion();
        orden.Aprobar();

        //Act
        var ex = Assert.Throws<InvalidOperationException>(() => orden.Aprobar());
        
        //Assert
        Assert.Equal(EstadoOrden.Aprobada, orden.Estado);
        Assert.Contains("Transición inválida", ex.Message);
    }

    [Fact]
    public void Recibida_Aprobar_LanzaInvalidOperationException_YElEstadoNoCambia()
    {
        //Arrange
        var orden = new OrdenCompra
        {
            ProveedorId = 18,
            CreadoPorUsuarioId = Guid.NewGuid(),
            Detalles = new List<DetalleOrden>
            {
                new() { ProductoId = 15, Cantidad = 9, Precio = 250}
            }
        };
        orden.EnviarAAprobacion();
        orden.Aprobar();
        orden.Recibir();

        //Act
        var ex = Assert.Throws<InvalidOperationException>(() => orden.Aprobar());
        
        //Assert
        Assert.Equal(EstadoOrden.Recibida, orden.Estado);
        Assert.Contains("Transición inválida", ex.Message);
    }

    [Fact]
    public void Cancelada_Aprobar_LanzaInvalidOperationException_YElEstadoNoCambia()
    {
        //Arrange
        var orden = new OrdenCompra
        {
            ProveedorId = 19,
            CreadoPorUsuarioId = Guid.NewGuid(),
            Detalles = new List<DetalleOrden>
            {
                new() { ProductoId = 16, Cantidad = 10, Precio = 260}
            }
        };
        orden.Cancelar();
        
        //Act
        var ex = Assert.Throws<InvalidOperationException>(() => orden.Aprobar());

        //Assert
        Assert.Equal(EstadoOrden.Cancelada, orden.Estado);
        Assert.Contains("Transición inválida", ex.Message);
    }

    [Fact]
    public void Borrador_EnviarAAprobacion_SinDetalles_LanzaInvalidOperationException_YElEstadoNoCambia()
    {
        //Arrange
        var orden = new OrdenCompra
        {
            ProveedorId = 20,
            CreadoPorUsuarioId = Guid.NewGuid(),
            Detalles = new List<DetalleOrden>() // Sin detalles
        };
        
        //Act
        var ex = Assert.Throws<InvalidOperationException>(() => orden.EnviarAAprobacion());
        
        //Assert
        Assert.Equal(EstadoOrden.Borrador, orden.Estado);
        Assert.Contains("detalle", ex.Message.ToLower());
    }

    [Fact]
    public void PendienteAprobacion_EnviarAAprobacion_LanzaInvalidOperationException_YElEstadoNoCambia()
    {
        //Arrange
        var orden = new OrdenCompra
        {
            ProveedorId = 21,
            CreadoPorUsuarioId = Guid.NewGuid(),
            Detalles = new List<DetalleOrden>
            {
                new() { ProductoId = 17, Cantidad = 1, Precio = 10 }
            }
        };
        orden.EnviarAAprobacion();

        //Act
        var ex = Assert.Throws<InvalidOperationException>(() => orden.EnviarAAprobacion());

        //Assert
        Assert.Equal(EstadoOrden.PendienteAprobacion, orden.Estado);
        Assert.Contains("Transición inválida", ex.Message);
    }
}
