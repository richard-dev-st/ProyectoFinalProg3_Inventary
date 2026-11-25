using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Inventario.Web.Models;

namespace Inventario.Web.Services
{
    // Práctica 4 (RF-DOC-01..04): cliente del manejador de documentos.
    // El servidor decide qué ve cada usuario: el Estándar solo lo suyo y el
    // Administrador todo; aquí no se replica esa regla, solo se muestra el resultado.
    public class DocumentosServices
    {
        private readonly HttpClient _http;
        private readonly SesionActual _sesion;

        public DocumentosServices(HttpClient http, SesionActual sesion)
        {
            _http = http;
            _sesion = sesion;
        }

        // RF-DOC-02: listado permitido (sin eliminados; filtrado por rol en el servidor).
        public async Task<(bool Ok, List<DocumentoDto> Documentos, string Mensaje)> ListarAsync()
        {
            using var request = CrearRequest(HttpMethod.Get, "api/documentos");
            var response = await _http.SendAsync(request);

            var (ok, mensaje) = await InterpretarAsync(response, "No se pudieron cargar los documentos.");
            if (!ok) return (false, new(), mensaje);

            var documentos = await response.Content.ReadFromJsonAsync<List<DocumentoDto>>();
            return (true, documentos ?? new(), string.Empty);
        }

        // RF-DOC-01: sube el archivo como multipart/form-data en el campo "archivo".
        public async Task<(bool Ok, DocumentoDto? Documento, string Mensaje)> SubirAsync(
            string nombreArchivo, string tipoContenido, Stream contenido)
        {
            using var form = new MultipartFormDataContent();
            using var archivo = new StreamContent(contenido);
            archivo.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(tipoContenido) ? "application/octet-stream" : tipoContenido);
            form.Add(archivo, "archivo", nombreArchivo);

            using var request = CrearRequest(HttpMethod.Post, "api/documentos/subir");
            request.Content = form;

            var response = await _http.SendAsync(request);
            var (ok, mensaje) = await InterpretarAsync(response, "No se pudo subir el documento.");
            if (!ok) return (false, null, mensaje);

            var creado = await response.Content.ReadFromJsonAsync<DocumentoDto>();
            return (true, creado, string.Empty);
        }

        // RF-DOC-03: descarga en memoria; el dueño o un Administrador reciben los bytes.
        public async Task<(bool Ok, byte[]? Contenido, string Nombre, string TipoContenido, string Mensaje)>
            DescargarAsync(Guid id)
        {
            using var request = CrearRequest(HttpMethod.Get, $"api/documentos/{id}/descargar");
            var response = await _http.SendAsync(request);

            var (ok, mensaje) = await InterpretarAsync(
                response, "No se pudo descargar el documento.");
            if (!ok) return (false, null, string.Empty, string.Empty, mensaje);

            var nombre = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName
                ?? "documento";
            nombre = nombre.Trim('"');

            var tipo = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var bytes = await response.Content.ReadAsByteArrayAsync();

            return (true, bytes, nombre, tipo, string.Empty);
        }

        // RF-DOC-04: eliminación lógica (el servidor oculta el documento del listado).
        public async Task<(bool Ok, string Mensaje)> EliminarAsync(Guid id)
        {
            using var request = CrearRequest(HttpMethod.Delete, $"api/documentos/{id}");
            var response = await _http.SendAsync(request);

            return await InterpretarAsync(response, "No se pudo eliminar el documento.");
        }

        private HttpRequestMessage CrearRequest(HttpMethod metodo, string url)
        {
            var request = new HttpRequestMessage(metodo, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _sesion.Token);
            return request;
        }

        private async Task<(bool Ok, string Mensaje)> InterpretarAsync(
            HttpResponseMessage response, string mensajePorDefecto)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _sesion.CerrarAsync();
                return (false, "Tu sesión no es válida. Inicia sesión de nuevo.");
            }

            if (response.IsSuccessStatusCode)
            {
                return (true, string.Empty);
            }

            // 403/404/400 traen un { mensaje } claro del servidor (RD-06/RD-07).
            return (false, await LeerMensajeAsync(response, mensajePorDefecto));
        }

        private static async Task<string> LeerMensajeAsync(HttpResponseMessage response, string defecto)
        {
            try
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var raiz = doc.RootElement;

                foreach (var clave in new[] { "mensaje", "message", "detail", "title", "error" })
                {
                    if (raiz.TryGetProperty(clave, out var valor) && valor.ValueKind == JsonValueKind.String)
                    {
                        return valor.GetString()!;
                    }
                }
            }
            catch (JsonException)
            {
                // Cuerpo vacío o no JSON: se usa el mensaje por defecto.
            }

            return defecto;
        }
    }
}
