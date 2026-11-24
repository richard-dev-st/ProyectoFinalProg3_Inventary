using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Inventario.Web.Models;

namespace Inventario.Web.Services
{
    // Datos maestros del módulo de negocio. La lectura la usa cualquier usuario con
    // sesión; el alta solo responde al Administrador (el servidor lo verifica).
    public class CatalogoServices
    {
        private readonly HttpClient _http;
        private readonly SesionActual _sesion;

        public CatalogoServices(HttpClient http, SesionActual sesion)
        {
            _http = http;
            _sesion = sesion;
        }

        public async Task<(bool Ok, List<ProveedorDto> Proveedores, string Mensaje)> ListarProveedoresAsync()
        {
            using var request = CrearRequest(HttpMethod.Get, "api/catalogo/proveedores");
            var response = await _http.SendAsync(request);

            var (ok, mensaje) = await InterpretarAsync(response, "No se pudieron cargar los proveedores.");
            if (!ok) return (false, new(), mensaje);

            var proveedores = await response.Content.ReadFromJsonAsync<List<ProveedorDto>>();
            return (true, proveedores ?? new(), string.Empty);
        }

        public async Task<(bool Ok, ProveedorDto? Proveedor, string Mensaje)> CrearProveedorAsync(
            CrearProveedorRequest proveedor)
        {
            using var request = CrearRequest(HttpMethod.Post, "api/catalogo/proveedores");
            request.Content = JsonContent.Create(proveedor);

            var response = await _http.SendAsync(request);
            var (ok, mensaje) = await InterpretarAsync(response, "No se pudo crear el proveedor.");
            if (!ok) return (false, null, mensaje);

            var creado = await response.Content.ReadFromJsonAsync<ProveedorDto>();
            return (true, creado, string.Empty);
        }

        public async Task<(bool Ok, List<ProductoDto> Productos, string Mensaje)> ListarProductosAsync()
        {
            using var request = CrearRequest(HttpMethod.Get, "api/catalogo/productos");
            var response = await _http.SendAsync(request);

            var (ok, mensaje) = await InterpretarAsync(response, "No se pudieron cargar los productos.");
            if (!ok) return (false, new(), mensaje);

            var productos = await response.Content.ReadFromJsonAsync<List<ProductoDto>>();
            return (true, productos ?? new(), string.Empty);
        }

        public async Task<(bool Ok, ProductoDto? Producto, string Mensaje)> CrearProductoAsync(
            CrearProductoRequest producto)
        {
            using var request = CrearRequest(HttpMethod.Post, "api/catalogo/productos");
            request.Content = JsonContent.Create(producto);

            var response = await _http.SendAsync(request);
            var (ok, mensaje) = await InterpretarAsync(response, "No se pudo crear el producto.");
            if (!ok) return (false, null, mensaje);

            var creado = await response.Content.ReadFromJsonAsync<ProductoDto>();
            return (true, creado, string.Empty);
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

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return (false, "Solo un Administrador puede crear datos maestros.");
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
