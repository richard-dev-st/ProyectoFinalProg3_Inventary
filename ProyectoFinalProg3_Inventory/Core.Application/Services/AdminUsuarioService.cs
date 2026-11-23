using Application.DTOs.Admin;
using Core.Application.DTOs.Admin;
using Application.Interfaces;
using Core.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Core.Application.Services
{
    public class AdminUsuarioService : IAdminUsuarioService
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuditoriaService _auditoria;

        public AdminUsuarioService(IApplicationDbContext context, IAuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
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

            // RF-AUD-04: el actor es el administrador que ejecuta el cambio.
            var admin = await _context.Usuarios.FindAsync(adminIdActual)
                ?? throw new UnauthorizedAccessException("Administrador no válido.");

            // RF-AUD-04: valor anterior antes de cambiar el estado.
            var valorAnterior = JsonSerializer.Serialize(new { usuario.Activo });

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

            var valorNuevo = JsonSerializer.Serialize(new { usuario.Activo });

            // RF-AUD-04: desactivación y reactivación quedan auditadas.
            _context.Auditoria.Add(_auditoria.Preparar(
                admin.Id,
                admin.Email,
                dto.Activo ? AccionesAuditoria.Reactivar : AccionesAuditoria.Desactivar,
                EntidadesAuditoria.Usuario,
                usuario.Id.ToString(),
                valorAnterior,
                valorNuevo));

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

            // RF-AUD-04: el actor es el administrador que ejecuta el cambio.
            var admin = await _context.Usuarios.FindAsync(adminIdActual)
                ?? throw new UnauthorizedAccessException("Administrador no válido.");

            // RF-AUD-04: valor anterior antes de cambiar el rol.
            var valorAnterior = JsonSerializer.Serialize(new { Rol = usuario.Rol.ToString() });

            // Cambiamos el rol del usuario utilizando el método de la entidad Usuario
            //Regla de negocio en el dominio (RF-CA-08)
            usuario.CambiarRol(dto.NuevoRol, adminIdActual);

            var valorNuevo = JsonSerializer.Serialize(new { Rol = usuario.Rol.ToString() });

            // RF-AUD-04: todo cambio de rol queda auditado con el valor anterior y el nuevo.
            _context.Auditoria.Add(_auditoria.Preparar(
                admin.Id,
                admin.Email,
                AccionesAuditoria.CambiarRol,
                EntidadesAuditoria.Usuario,
                usuario.Id.ToString(),
                valorAnterior,
                valorNuevo));

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
