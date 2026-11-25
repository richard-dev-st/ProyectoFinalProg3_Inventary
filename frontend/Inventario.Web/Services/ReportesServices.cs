using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Inventario.Web.Models;

namespace Inventario.Web.Services
{
    // RF-REP-01..05: cliente de los reportes. La agregación la hace el servidor;
    // aquí solo se envían las fechas y se muestra el resultado.
    public class ReportesServices
    {
        private readonly HttpClient _http;
        private readonly SesionActual _sesion;

        public ReportesServices(HttpClient http, SesionActual sesion)
        {
            _http = http;
            _sesion = sesion;
        }

        // RF-REP-01/04/05: solicitudes agrupadas por estado dentro del rango.
        public async Task<(bool Ok, ReporteSolicitudesDto? Reporte, string Mensaje)> ObtenerSolicitudesAsync(
            DateTime? desde, DateTime? hasta)
        {
            var url = ConstruirUrl("api/reportes/solicitudes", desde, hasta);
            using var request = CrearRequest(url);
            var response = await _http.SendAsync(request);

            var (ok, mensaje) = await InterpretarAsync(
                response, "No se pudo cargar el reporte de solicitudes.");
            if (!ok) return (false, null, mensaje);

            var reporte = await response.Content.ReadFromJsonAsync<ReporteSolicitudesDto>();
            return (true, reporte, string.Empty);
        }

        // RF-REP-02/04/05: compras por proveedor y estado dentro del rango.
        public async Task<(bool Ok, ReporteOrdenesDto? Reporte, string Mensaje)> ObtenerOrdenesAsync(
            DateTime? desde, DateTime? hasta)
        {
            var url = ConstruirUrl("api/reportes/ordenes", desde, hasta);
            using var request = CrearRequest(url);
            var response = await _http.SendAsync(request);

            var (ok, mensaje) = await InterpretarAsync(
                response, "No se pudo cargar el reporte de órdenes.");
            if (!ok) return (false, null, mensaje);

            var reporte = await response.Content.ReadFromJsonAsync<ReporteOrdenesDto>();
            return (true, reporte, string.Empty);
        }

        private static string ConstruirUrl(string baseUrl, DateTime? desde, DateTime? hasta)
        {
            var partes = new List<string>();

            if (desde.HasValue)
            {
                partes.Add("desde=" + desde.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }

            if (hasta.HasValue)
            {
                partes.Add("hasta=" + hasta.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }

            return partes.Count == 0 ? baseUrl : $"{baseUrl}?{string.Join("&", partes)}";
        }

        private HttpRequestMessage CrearRequest(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
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

            // RF-REP-05: rango invertido responde 400 con { message }.
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
