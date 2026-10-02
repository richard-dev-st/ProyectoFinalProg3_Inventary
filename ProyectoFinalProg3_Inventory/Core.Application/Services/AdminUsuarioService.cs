using Application.DTOs.Admin;
using Core.Application.DTOs.Admin;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Services
{
    public class AdminUsuarioService : IAdminUsuarioService
    {
        private readonly IApplicationDbContext _context;

        public AdminUsuarioService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CambiarEstadoUsuarioAsync(Guid usuarioId, CambiarEstadoDto dto, Guid adminIdActual)
        {
            // Buscamos el usuario en la base de datos
            var usuario = await _context.Usuarios.FindAsync(usuarioId);

            // Si el usuario no existe, lanzamos una excepción
            if (usuario == null)
            {
                throw new KeyNotFoundException("El usuario especificado no existe.");
            }

            // Cambiamos el estado del usuario utilizando los métodos de la entidad Usuario
            if (dto.Activo)
            {
                usuario.Reactivar();
            }
            else
            {
                // Desactivamos el usuario utilizando el método de la entidad Usuario
                usuario.Desactivar(adminIdActual);
            }

            // Guardamos los cambios en la base de datos
            await _context.SaveChangesAsync();
        }

        public async Task CambiarRolUsuarioAsync(Guid usuarioId, CambiarRolDto dto, Guid adminIdActual)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);

            if (usuario == null)
            {
                throw new Exception("El usuario especificado no existe.");
            }

            // Cambiamos el rol del usuario utilizando el método de la entidad Usuario
            //Regla de negocio en el dominio (RF-CA-08)
            usuario.CambiarRol(dto.NuevoRol, adminIdActual);

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<UsuarioAdminDto>> ObtenerUsuariosAsync()
        {
            return await _context.Usuarios.AsNoTracking()
                .Select(u => new UsuarioAdminDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    Rol = u.Rol,
                    Activo = u.Activo
                })
                .ToListAsync();
        }

    }
}