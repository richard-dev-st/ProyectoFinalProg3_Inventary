using Application.DTOs.Catalogo;

namespace Application.Interfaces
{
    // Datos maestros del módulo de negocio: proveedores y productos.
    // Sin ellos no se puede crear una orden de compra (RF-NEG-02).
    public interface ICatalogoService
    {
        Task<List<ProveedorDto>> ListarProveedoresAsync();
        Task<ProveedorDto> CrearProveedorAsync(CrearProveedorDto dto);

        Task<List<ProductoDto>> ListarProductosAsync();
        Task<ProductoDto> CrearProductoAsync(CrearProductoDto dto);
    }
}
