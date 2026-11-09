using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Services
{
    public class AlmacenDocumentosLocal : IAlmacenDocumentos
    {
        private readonly string _rutaBase;

        public AlmacenDocumentosLocal(IConfiguration config)
        {
            _rutaBase = Path.GetFullPath(config["Documentos:RutaBase"] ?? "uploads");
            Directory.CreateDirectory(_rutaBase);
        }

        private string Ruta(string nombre)
        {
            var ruta = Path.GetFullPath(Path.Combine(_rutaBase, nombre));
            if(!ruta.StartsWith(_rutaBase, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Ruta de archivo no valida.");
            }

            return ruta;
        }

        public Task<Stream> AbrirAsync(string nombreAlmacenado, CancellationToken ct = default)
        {
            return Task.FromResult<Stream>(File.OpenRead(Ruta(nombreAlmacenado)));
        }

        public Task EliminarAsync(string nombreAlmacenado, CancellationToken ct = default)
        {
            var ruta = Ruta(nombreAlmacenado);
            if(File.Exists(ruta))
            {
                File.Delete(ruta);
            }
            return Task.CompletedTask;
        }

        public bool Existe(string nombreAlmacenado)
        {
            return File.Exists(Ruta(nombreAlmacenado));
        }

        public async Task GuardarAsync(Stream contenido, string nombreAlmacenado, CancellationToken ct = default)
        {
            await using var destino = File.Create(Ruta(nombreAlmacenado));
            await contenido.CopyToAsync(destino, ct);
        }
    }
}
