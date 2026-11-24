using Application.DTOs.Catalogo;
using Application.Interfaces;
using Business.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

namespace Application.Services
{
    public class CatalogoService : ICatalogoService
    {
        private readonly IApplicationDbContext _context;

        public CatalogoService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProveedorDto>> ListarProveedoresAsync()
        {
            return await _context.Proveedores.AsNoTracking()
                .OrderBy(p => p.Nombre)
                .Select(p => new ProveedorDto(
                    p.Id,
                    p.Nombre,
                    p.Cedula,
                    p.Telefono,
                    p.Email,
                    p.Productos.Count))
                .ToListAsync();
        }

        public async Task<ProveedorDto> CrearProveedorAsync(CrearProveedorDto dto)
        {
            // RD-07: entrada inválida se rechaza con un mensaje claro antes de tocar la BD.
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new InvalidOperationException("El nombre del proveedor es obligatorio.");

            var nombre = dto.Nombre.Trim();
            if (nombre.Length > 100)
                throw new InvalidOperationException(
                    "El nombre del proveedor no puede superar los 100 caracteres.");

            var email = dto.Email?.Trim() ?? string.Empty;
            if (email.Length > 0 && !MailAddress.TryCreate(email, out _))
                throw new InvalidOperationException("El correo del proveedor no es válido.");

            var proveedor = new Proveedor
            {
                Nombre = nombre,
                Cedula = dto.Cedula?.Trim() ?? string.Empty,
                Telefono = dto.Telefono?.Trim() ?? string.Empty,
                Email = email
            };

            _context.Proveedores.Add(proveedor);
            await _context.SaveChangesAsync();

            return new ProveedorDto(
                proveedor.Id,
                proveedor.Nombre,
                proveedor.Cedula,
                proveedor.Telefono,
                proveedor.Email,
                0);
        }

        public async Task<List<ProductoDto>> ListarProductosAsync()
        {
            var productos = await _context.Productos.AsNoTracking()
                .Include(p => p.Proveedor)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            return productos
                .Select(p => new ProductoDto(
                    p.Id,
                    p.Nombre,
                    p.Codigo,
                    p.Precio,
                    p.StockActual,
                    p.StockMinimo,
                    p.ProveedorId,
                    p.Proveedor?.Nombre ?? string.Empty))
                .ToList();
        }

        public async Task<ProductoDto> CrearProductoAsync(CrearProductoDto dto)
        {
            // RD-07: validaciones de entrada con mensajes claros.
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new InvalidOperationException("El nombre del producto es obligatorio.");

            var nombre = dto.Nombre.Trim();
            if (nombre.Length > 100)
                throw new InvalidOperationException(
                    "El nombre del producto no puede superar los 100 caracteres.");

            if (dto.Precio < 0)
                throw new InvalidOperationException("El precio no puede ser negativo.");

            if (dto.StockActual < 0 || dto.StockMinimo < 0)
                throw new InvalidOperationException("El stock no puede ser negativo.");

            var proveedor = await _context.Proveedores.FindAsync(dto.ProveedorId);
            if (proveedor is null)
                throw new InvalidOperationException(
                    $"El proveedor {dto.ProveedorId} no existe.");

            var codigo = string.IsNullOrWhiteSpace(dto.Codigo)
                ? GenerarCodigo()
                : dto.Codigo.Trim();

            if (await _context.Productos.AnyAsync(p => p.Codigo == codigo))
                throw new InvalidOperationException($"El código '{codigo}' ya está en uso.");

            var producto = new Producto
            {
                Nombre = nombre,
                Codigo = codigo,
                Precio = dto.Precio,
                StockActual = dto.StockActual,
                StockMinimo = dto.StockMinimo,
                ProveedorId = proveedor.Id
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            return new ProductoDto(
                producto.Id,
                producto.Nombre,
                producto.Codigo,
                producto.Precio,
                producto.StockActual,
                producto.StockMinimo,
                producto.ProveedorId,
                proveedor.Nombre);
        }

        // Código legible cuando el usuario no indica uno.
        private static string GenerarCodigo() =>
            "PROD-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    }
}
