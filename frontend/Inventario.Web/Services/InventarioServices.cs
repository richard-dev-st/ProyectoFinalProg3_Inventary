using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Inventario.Web.Services
{
    // RF-GP-05: consumidor real del permiso concedido automáticamente tras aprobar.
    public class InventarioServices
    {
        private readonly HttpClient _http;
        private readonly SesionActual _sesion;

        public InventarioServices(HttpClient http, SesionActual sesion)
        {
            _http = http;
            _sesion = sesion;
        }

        public async Task<(bool Ok, string Csv, string Mensaje)> ExportarCsvAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/inventario/exportar");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _sesion.Token);

            var response = await _http.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _sesion.CerrarAsync();
                return (false, string.Empty, "Tu sesión no es válida. Inicia sesión de nuevo.");
            }

            // Sin el permiso 'exportar-inventario' la policy del backend devuelve 403.
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return (false, string.Empty,
                    "No tienes el permiso 'exportar-inventario'. Solicítalo y espera a que sea aprobado.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return (false, string.Empty,
                    await LeerMensajeAsync(response, "No se pudo exportar el inventario."));
            }

            return (true, await response.Content.ReadAsStringAsync(), string.Empty);
        }

        private static async Task<string> LeerMensajeAsync(HttpResponseMessage response, string defecto)
        {
            try
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("mensaje", out var valor)
                    && valor.ValueKind == JsonValueKind.String)
                {
                    return valor.GetString()!;
                }
            }
            catch (JsonException)
            {
                // Respuesta no JSON: se usa el mensaje por defecto.
            }

            return defecto;
        }
    }
}
