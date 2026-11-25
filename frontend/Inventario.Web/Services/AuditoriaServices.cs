using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Inventario.Web.Models;

namespace Inventario.Web.Services
{
    // RF-AUD-07: cliente de la consulta administrativa. El backend no expone
    // POST/PUT/DELETE (RF-AUD-06), así que aquí solo se lee el historial.
    public class AuditoriaServices
    {
        private readonly HttpClient _http;
        private readonly SesionActual _sesion;

        public AuditoriaServices(HttpClient http, SesionActual sesion)
        {
            _http = http;
            _sesion = sesion;
        }

        public async Task<(bool Ok, ResultadoAuditoriaDto? Resultado, string Mensaje)> ConsultarAsync(
            Guid? usuarioId, string? entidad, DateTime? desde, DateTime? hasta,
            int pagina, int tamanioPagina)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                ConstruirUrl(usuarioId, entidad, desde, hasta, pagina, tamanioPagina));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _sesion.Token);

            var response = await _http.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _sesion.CerrarAsync();
                return (false, null, "Tu sesión no es válida. Inicia sesión de nuevo.");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return (false, null, "Solo un Administrador puede consultar el historial de auditoría.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return (false, null, await LeerMensajeAsync(
                    response, "No se pudo cargar el historial de auditoría."));
            }

            var resultado = await response.Content.ReadFromJsonAsync<ResultadoAuditoriaDto>();
            if (resultado is null)
            {
                return (false, null, "La API no devolvió el historial de auditoría.");
            }

            return (true, resultado, string.Empty);
        }

        private static string ConstruirUrl(Guid? usuarioId, string? entidad,
            DateTime? desde, DateTime? hasta, int pagina, int tamanioPagina)
        {
            var parametros = new List<string>();

            if (usuarioId.HasValue)
            {
                parametros.Add("usuarioId=" + usuarioId.Value.ToString("D"));
            }

            if (!string.IsNullOrWhiteSpace(entidad))
            {
                parametros.Add("entidad=" + Uri.EscapeDataString(entidad.Trim()));
            }

            // RF-AUD-07: una fecha sin hora incluye el día completo en el servicio.
            if (desde.HasValue)
            {
                parametros.Add("desde=" + desde.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }

            if (hasta.HasValue)
            {
                parametros.Add("hasta=" + hasta.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }

            parametros.Add("pagina=" + pagina.ToString(CultureInfo.InvariantCulture));
            parametros.Add("tamanioPagina=" + tamanioPagina.ToString(CultureInfo.InvariantCulture));

            return $"api/auditoria?{string.Join("&", parametros)}";
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
