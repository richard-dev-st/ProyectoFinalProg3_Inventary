using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Inventario.Web.Models;

namespace Inventario.Web.Services
{
    // RF-NOT-04/05/07: cliente de la bandeja interna y de la preferencia de correo.
    // El servidor decide el destinatario a partir del token; aquí no se envía ningún id de usuario.
    public class NotificacionesServices
    {
        private readonly HttpClient _http;
        private readonly SesionActual _sesion;

        public NotificacionesServices(HttpClient http, SesionActual sesion)
        {
            _http = http;
            _sesion = sesion;
        }

        // RF-NOT-04: solo las notificaciones propias, opcionalmente sin leer.
        public async Task<(bool Ok, List<NotificacionDto> Notificaciones, string Mensaje)> ListarAsync(
            bool soloNoLeidas = false)
        {
            var url = soloNoLeidas
                ? "api/notificaciones?soloNoLeidas=true"
                : "api/notificaciones";

            using var request = CrearRequest(HttpMethod.Get, url);
            var response = await _http.SendAsync(request);

            var (ok, mensaje) = await InterpretarAsync(
                response, "No se pudieron cargar tus notificaciones.");
            if (!ok) return (false, new(), mensaje);

            var notificaciones = await response.Content.ReadFromJsonAsync<List<NotificacionDto>>();
            return (true, notificaciones ?? new(), string.Empty);
        }

        // RF-NOT-05: marcar una propia como leída. La API responde 204 (idempotente).
        public async Task<(bool Ok, string Mensaje)> MarcarLeidaAsync(Guid id)
        {
            using var request = CrearRequest(HttpMethod.Post, $"api/notificaciones/{id}/leer");
            var response = await _http.SendAsync(request);

            return await InterpretarAsync(response, "No se pudo marcar la notificación como leída.");
        }

        // RF-NOT-07: consultar la preferencia actual del usuario de la sesión.
        public async Task<(bool Ok, bool RecibeCorreos, string Mensaje)> ObtenerPreferenciaAsync()
        {
            using var request = CrearRequest(HttpMethod.Get, "api/notificaciones/preferencia");
            var response = await _http.SendAsync(request);

            var (ok, mensaje) = await InterpretarAsync(
                response, "No se pudo cargar la preferencia de correo.");
            if (!ok) return (false, false, mensaje);

            var preferencia = await response.Content.ReadFromJsonAsync<PreferenciaCorreoDto>();
            return (true, preferencia?.RecibeCorreos ?? false, string.Empty);
        }

        // RF-NOT-07: el canal interno no se desactiva; solo se apaga la copia por correo.
        public async Task<(bool Ok, string Mensaje)> ActualizarPreferenciaAsync(bool recibeCorreos)
        {
            using var request = CrearRequest(HttpMethod.Put, "api/notificaciones/preferencia");
            request.Content = JsonContent.Create(new PreferenciaCorreoDto(recibeCorreos));

            var response = await _http.SendAsync(request);
            return await InterpretarAsync(response, "No se pudo guardar la preferencia de correo.");
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

            // RF-NOT-05: 404 también cuando la notificación es de otro usuario.
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return (false, "La notificación no existe o no es tuya.");
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
