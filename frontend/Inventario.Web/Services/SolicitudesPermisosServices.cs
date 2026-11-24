using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Inventario.Web.Models;

namespace Inventario.Web.Services
{
    // RF-GP-01..07: cliente de la API de gestión de permisos.
    // El Bearer se agrega en cada petición: un DelegatingHandler scoped recibiría
    // otra instancia de SesionActual y el token no llegaría actualizado.
    public class SolicitudesPermisosServices
    {
        private readonly HttpClient _http;
        private readonly SesionActual _sesion;

        public SolicitudesPermisosServices(HttpClient http, SesionActual sesion)
        {
            _http = http;
            _sesion = sesion;
        }

        // RF-GP-01: la solicitud nace Pendiente; la API devuelve el DTO creado.
        public Task<(bool Ok, string Mensaje)> CrearAsync(CrearSolicitudRequest solicitud) =>
            EnviarAsync(HttpMethod.Post, "api/solicitudes", solicitud,
                "No se pudo crear la solicitud.");

        // RF-GP-02: el backend filtra por el id del token, no por un parámetro del cliente.
        public Task<(bool Ok, List<SolicitudPermisoDto> Solicitudes, string Mensaje)> ObtenerMiasAsync() =>
            ObtenerListaAsync("api/solicitudes/mias", "No se pudieron cargar tus solicitudes.");

        // RF-GP-02: solo el rol Administrador puede consultar esta lista.
        public Task<(bool Ok, List<SolicitudPermisoDto> Solicitudes, string Mensaje)> ObtenerPendientesAsync() =>
            ObtenerListaAsync("api/solicitudes/pendientes", "No se pudieron cargar las solicitudes pendientes.");

        // RF-GP-03/05/06/07: aprobar aplica el permiso en la misma transacción.
        public Task<(bool Ok, string Mensaje)> AprobarAsync(Guid id) =>
            EnviarAsync(HttpMethod.Post, $"api/solicitudes/{id}/aprobar", new { },
                "No se pudo aprobar la solicitud.");

        // RF-GP-04/06/07: el motivo es obligatorio y lo valida también el backend.
        public Task<(bool Ok, string Mensaje)> RechazarAsync(Guid id, string motivo) =>
            EnviarAsync(HttpMethod.Post, $"api/solicitudes/{id}/rechazar",
                new RechazarSolicitudRequest { Motivo = motivo },
                "No se pudo rechazar la solicitud.");

        // RF-GP-05: consulta los permisos ya concedidos al usuario de la sesión.
        public async Task<(bool Ok, List<PermisoDto> Permisos, string Mensaje)> ObtenerMisPermisosAsync()
        {
            using var request = CrearRequest(HttpMethod.Get, "api/mis-permisos");
            var response = await _http.SendAsync(request);

            var (ok, mensaje) = await InterpretarAsync(response, "No se pudieron cargar tus permisos.");
            if (!ok) return (false, new(), mensaje);

            var permisos = await response.Content.ReadFromJsonAsync<List<PermisoDto>>();
            return (true, permisos ?? new(), string.Empty);
        }

        private async Task<(bool Ok, List<SolicitudPermisoDto> Solicitudes, string Mensaje)> ObtenerListaAsync(
            string url, string mensajePorDefecto)
        {
            using var request = CrearRequest(HttpMethod.Get, url);
            var response = await _http.SendAsync(request);

            var (ok, mensaje) = await InterpretarAsync(response, mensajePorDefecto);
            if (!ok) return (false, new(), mensaje);

            var solicitudes = await response.Content.ReadFromJsonAsync<List<SolicitudPermisoDto>>();
            return (true, solicitudes ?? new(), string.Empty);
        }

        private async Task<(bool Ok, string Mensaje)> EnviarAsync(
            HttpMethod metodo, string url, object body, string mensajePorDefecto)
        {
            using var request = CrearRequest(metodo, url);
            request.Content = JsonContent.Create(body);

            var response = await _http.SendAsync(request);
            return await InterpretarAsync(response, mensajePorDefecto);
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

            // RD-06: la autorización se verifica en el servidor; aquí solo se traduce el rechazo.
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return (false, "No tienes permisos para realizar esta acción.");
            }

            if (response.IsSuccessStatusCode)
            {
                return (true, string.Empty);
            }

            return (false, await LeerMensajeAsync(response, mensajePorDefecto));
        }

        private static async Task<string> LeerMensajeAsync(HttpResponseMessage response, string defecto)
        {
            try
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var raiz = doc.RootElement;

                foreach (var clave in new[] { "mensaje", "detail", "message", "title" })
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
