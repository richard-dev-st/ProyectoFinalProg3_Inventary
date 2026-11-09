using Application.DTOs.Documentos;
using Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class DocumentoService : IDocumentoService
    {

        private const long TamanioMaximoBytes = 10 * 1024 * 1024; //10 MB

        private static readonly string[] ExtensionesPermitidas = { ".pdf", ".png", ".jpg", ".jpeg", ".docx", ".xlsx", ".txt" };

        private readonly IApplicationDbContext _context;
        private readonly IAlmacenDocumentos _almacen;

        public DocumentoService(IApplicationDbContext context, IAlmacenDocumentos almacen)
        {
            _context = context;
            _almacen = almacen;
        }

        public async Task<DocumentoDescargaDto> DescargarDocumentoAsync(Guid id, Guid usuarioId)
        {
            var documento = await _context.Documentos.FirstOrDefaultAsync(d => d.Id == id && !d.EsEliminado) ?? throw new InvalidOperationException("Documento no encontrado.");

            if (documento.SubidoPorUsuarioId != usuarioId && !await EsAdministradorAsync(usuarioId)) throw new UnauthorizedAccessException("No tienes acceso a este documento.");

            if (!_almacen.Existe(documento.NombreAlmacenado)) throw new InvalidOperationException("El archivo ya no está disponible en el almacenamiento.");

            var contenido = await _almacen.AbrirAsync(documento.NombreAlmacenado);

            return new DocumentoDescargaDto(documento.NombreOriginal, documento.TipoContenido, contenido);
        }

        public async Task EliminarDocumentoAsync(Guid id, Guid usuarioId)
        {
            var documento = await _context.Documentos.FirstOrDefaultAsync(d => d.Id == id && !d.EsEliminado) ?? throw new InvalidOperationException("Documento no encontrado.");

            if (documento.SubidoPorUsuarioId != usuarioId && !await EsAdministradorAsync(usuarioId)) throw new UnauthorizedAccessException("No puedes eliminar un documento que no te pertenece.");

            documento.Eliminar();
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<DocumentoDto>> ListarDocumentosAsync(Guid usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId) ?? throw new UnauthorizedAccessException("Usuario no válido.");

            var query = _context.Documentos.Where(d => !d.EsEliminado);

            if (usuario.Rol != RolUsuario.Administrador) query = query.Where(d => d.SubidoPorUsuarioId == usuarioId);

            var documentos = await query
                 .Include(d => d.SubidoPorUsuario)
                 .OrderByDescending(d => d.FechaSubida)
                 .ToListAsync();

            return documentos.Select(d => Mapear(d, d.SubidoPorUsuario?.Email ?? string.Empty));
        }

        public async Task<DocumentoDto> SubirDocumentoAsync(ArchivoEntrada archivo, Guid usuarioId)
        {
            //Validar entrada
            if(archivo is null)
            {
                throw new InvalidOperationException("No se recibió ningún archivo.");
            }
            if (string.IsNullOrWhiteSpace(archivo.NombreOriginal))
            {
                throw new InvalidOperationException("El nombre del archivo es obligatorio.");
            }
            if(archivo.Longitud <= 0)
            {
                throw new InvalidOperationException("El archivo está vacío.");
            }
            if(archivo.Longitud > TamanioMaximoBytes)
            {
                throw new InvalidOperationException("El archivo supera el límite de 10 MB.");
            }

            var extension = Path.GetExtension(archivo.NombreOriginal).ToLowerInvariant();
            if (!ExtensionesPermitidas.Contains(extension))
            {
                throw new InvalidOperationException($"Extensión no permitida. Permitidas: {string.Join(", ", ExtensionesPermitidas)}");
            }

            var usuario = await _context.Usuarios.FindAsync(usuarioId) ?? throw new UnauthorizedAccessException("Usuario no válido.");

            //2. Nombre fisico unico: evita colisiones y que el cliente controle la ruta
            var nombreAlmacenado = $"{Guid.NewGuid():N}{extension}";
            await _almacen.GuardarAsync(archivo.Contenido, nombreAlmacenado);

            var tipoContenido = string.IsNullOrWhiteSpace(archivo.TipoContenido)
                ? "application/octet-stream"
                : archivo.TipoContenido;

            var documento = new Documento(
                Path.GetFileName(archivo.NombreOriginal),
                nombreAlmacenado,
                tipoContenido,
                archivo.Longitud,
                usuarioId,
                DateTime.UtcNow);

            //3. Persistir; si falla la BD, borrar el archivo (compensacion)
            try
            {
                _context.Documentos.Add(documento);
                await _context.SaveChangesAsync();
            }
            catch
            {
                await _almacen.EliminarAsync(nombreAlmacenado);
                throw;
            }
            return Mapear(documento, usuario.Email);
        }

        private async Task<bool> EsAdministradorAsync(Guid usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            return usuario?.Rol == RolUsuario.Administrador;
        }

        private static DocumentoDto Mapear(Documento d, string subidoPorEmail) => new(
            d.Id, d.NombreOriginal, d.TipoContenido, d.TamanioBytes, d.FechaSubida, d.SubidoPorUsuarioId, subidoPorEmail, d.EsEliminado);
    }
}
