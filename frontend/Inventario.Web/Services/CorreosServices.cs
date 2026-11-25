using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Inventario.Web.Models;

namespace Inventario.Web.Services
{
    // RF-NOT-11: cliente de la cola de correos. Solo responde al Administrador;
    // el enlace del menú y la página también lo restringen, pero el servidor manda.
    public class CorreosServices
    {
        private readonly HttpClient _http;
        private readonly SesionActual _sesion;

        public CorreosServices(HttpClient http, SesionActual sesion)
        {
            _http = http;
            _sesion = sesion;
        }

        public async Task<(bool Ok, List<CorreoEnColaDto> Correos, string Mensaje)> ListarAsync(
            string? estado = null)
        {
            var url = string.IsNullOrWhiteSpace(estado)
                ? "api/correos"
                : $"api/correos?estado={Uri.EscapeDataString(estado)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _sesion.Token);

            var response = await _http.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _sesion.CerrarAsync();
                return (false, new(), "Tu sesión no es válida. Inicia sesión de nuevo.");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return (false, new(), "No tienes permisos para consultar la cola de correos.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return (false, new(), await LeerMensajeAsync(
                    response, "No se pudo cargar la cola de correos."));
            }

            var correos = await response.Content.ReadFromJsonAsync<List<CorreoEnColaDto>>();
            return (true, correos ?? new(), string.Empty);
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
