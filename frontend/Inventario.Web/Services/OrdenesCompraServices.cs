using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Inventario.Web.Models;

namespace Inventario.Web.Services
{
    // RF-NEG-02..06: cliente de las operaciones del módulo de negocio.
    // Igual que en permisos, el Bearer se agrega por petición desde SesionActual.
    public class OrdenesCompraServices
    {
        private readonly HttpClient _http;
        private readonly SesionActual _sesion;
        private readonly AuthService _auth;

        public OrdenesCompraServices(HttpClient http, SesionActual sesion, AuthService auth)
        {
            _http = http;
            _sesion = sesion;
            _auth = auth;
        }

        // RF-NEG-02: listado de órdenes (el backend filtra por usuario si no es Administrador).
        public async Task<(bool Ok, List<OrdenCompraResumenDto> Ordenes, string Mensaje)> ListarAsync()
        {
            using var request = CrearRequest(HttpMethod.Get, "api/ordenescompra");
            var response = await _http.SendAsync(request);

            var (ok, mensaje) = await InterpretarAsync(response, "No se pudieron cargar las órdenes.");
            if (!ok) return (false, new(), mensaje);

            var ordenes = await response.Content.ReadFromJsonAsync<List<OrdenCompraResumenDto>>();
            return (true, ordenes ?? new(), string.Empty);
        }

        // RF-NEG-02: proveedores y productos existentes para los desplegables del formulario.
        public async Task<(bool Ok, CatalogoOrdenDto? Catalogo, string Mensaje)> ObtenerCatalogoAsync()
        {
            using var request = CrearRequest(HttpMethod.Get, "api/ordenescompra/catalogo");
            var response = await _http.SendAsync(request);

            var (ok, mensaje) = await InterpretarAsync(response, "No se pudo cargar el catálogo.");
            if (!ok) return (false, null, mensaje);

            var catalogo = await response.Content.ReadFromJsonAsync<CatalogoOrdenDto>();
            return (true, catalogo, string.Empty);
        }

        // RF-NEG-02: detalle de una orden (el backend valida dueño o Administrador).
        public async Task<(bool Ok, OrdenCompraDetalleDto? Detalle, string Mensaje)> ObtenerDetalleAsync(int id)
        {
            using var request = CrearRequest(HttpMethod.Get, $"api/ordenescompra/{id}");
            var response = await _http.SendAsync(request);

            var (ok, mensaje) = await InterpretarAsync(response, "No se pudo cargar el detalle de la orden.");
            if (!ok) return (false, null, mensaje);

            var detalle = await response.Content.ReadFromJsonAsync<OrdenCompraDetalleDto>();
            return (true, detalle, string.Empty);
        }

        // RF-NEG-02: edición de un borrador (el backend rechaza si ya no está en Borrador).
        public async Task<(bool Ok, string Mensaje)> ActualizarAsync(int id, CrearOrdenCompraRequest orden)
        {
            using var request = CrearRequest(HttpMethod.Put, $"api/ordenescompra/{id}");
            request.Content = JsonContent.Create(orden);

            var response = await _http.SendAsync(request);
            return await InterpretarAsync(response, "No se pudo actualizar la orden.");
        }

        // RF-NEG-02: crear borrador. La API toma el usuario del cuerpo/query y valida la sesión.
        public async Task<(bool Ok, int OrdenId, string Mensaje)> CrearAsync(CrearOrdenCompraRequest orden)
        {
            var usuario = await _auth.ObtenerUsuarioActualAsync();
            if (usuario is null)
            {
                return (false, 0, "No se pudo identificar al usuario de la sesión.");
            }

            using var request = CrearRequest(
                HttpMethod.Post, $"api/ordenescompra/crear-borrador?usuarioId={usuario.Id}");
            request.Content = JsonContent.Create(orden);

            var response = await _http.SendAsync(request);
            var (ok, mensaje) = await InterpretarAsync(response, "No se pudo crear la orden.");
            if (!ok) return (false, 0, mensaje);

            var resultado = await response.Content.ReadFromJsonAsync<CrearOrdenResponse>();
            return (true, resultado?.OrdenId ?? 0, string.Empty);
        }

        // RF-NEG-03/04: Borrador -> PendienteAprobacion.
        public Task<(bool Ok, string Mensaje)> EnviarAprobacionAsync(int id) =>
            EnviarAsync(HttpMethod.Post, $"api/ordenescompra/{id}/enviar-aprobacion",
                "No se pudo enviar la orden a aprobación.");

        // RF-NEG-06: solo Administrador (el backend verifica el rol).
        public Task<(bool Ok, string Mensaje)> AprobarAsync(int id) =>
            EnviarAsync(HttpMethod.Post, $"api/ordenescompra/{id}/aprobar",
                "No se pudo aprobar la orden.");

        // RF-NEG-01/05: depende del permiso 'recibir-orden' concedido por Gestión de permisos.
        public Task<(bool Ok, string Mensaje)> RecibirAsync(int id) =>
            EnviarAsync(HttpMethod.Post, $"api/ordenescompra/{id}/recibir",
                "No se pudo recibir la orden.");

        // RF-NEG-06: solo Administrador (el backend verifica el rol).
        public Task<(bool Ok, string Mensaje)> CancelarAsync(int id) =>
            EnviarAsync(HttpMethod.Post, $"api/ordenescompra/{id}/cancelar",
                "No se pudo cancelar la orden.");

        // RF-NEG-02: total en DOP con la tasa de cambio vigente.
        public async Task<(bool Ok, TotalEnDopDto? Total, string Mensaje)> ObtenerTotalEnDopAsync(int id)
        {
            using var request = CrearRequest(HttpMethod.Get, $"api/ordenescompra/{id}/total-en-dop");
            var response = await _http.SendAsync(request);

            var (ok, mensaje) = await InterpretarAsync(response, "No se pudo obtener el total en DOP.");
            if (!ok) return (false, null, mensaje);

            var total = await response.Content.ReadFromJsonAsync<TotalEnDopDto>();
            return (true, total, string.Empty);
        }

        private async Task<(bool Ok, string Mensaje)> EnviarAsync(
            HttpMethod metodo, string url, string mensajePorDefecto)
        {
            using var request = CrearRequest(metodo, url);
            request.Content = JsonContent.Create(new { });

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

            // RD-06: el permiso se verifica en el servidor; aquí se muestra su mensaje.
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return (false, await LeerMensajeAsync(response, "No tienes permisos para realizar esta acción."));
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

        private sealed record CrearOrdenResponse(int OrdenId);
    }
}
