using System.Net;
using System.Text.Json;
using Inventario.Web.Models;

namespace Inventario.Web.Services
{
    public class AdminUsuariosServices
    {
        private readonly HttpClient _http;
        private readonly SesionActual _sesion;

        public AdminUsuariosServices(HttpClient http, SesionActual sesion)
        {
            _http = http;
            _sesion = sesion;
        }

        public async Task<(bool Ok, List<UsuarioAdmin> Usuarios, string Mensaje)> ObtenerUsuariosAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/admin/usuarios");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _sesion.Token);
            var response = await _http.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _sesion.CerrarAsync();
                return (false, new(), "Tu sesión no es válida.");
            }
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return (false, new(), "No tienes permisos para listar usuarios.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return (false, new(), await LeerMensajeAsync(response, "No se pudo cargar la lista de usuarios."));
            }

            var usuarios = await response.Content.ReadFromJsonAsync<List<UsuarioAdmin>>();
            return (true, usuarios ?? new(), string.Empty);
        }

        public Task<(bool Ok, string Mensaje)> CambiarRolAsync(Guid id, RolUsuario nuevoRol) => EnviarAsync(
            HttpMethod.Put,
            $"api/admin/usuarios/{id}/rol",
            new CambiarRolRequest { NuevoRol = nuevoRol },
            "No se pudo cambiar el rol del usuario.");

        public Task<(bool Ok, string Mensaje)> CambiarEstadoAsync(Guid id, bool activo) => EnviarAsync(
            HttpMethod.Put,
            $"api/admin/usuarios/{id}/estado",
            new CambiarEstadoRequest { Activo = activo },
            "No se pudo cambiar el estado del usuario.");

        // RF-CA-13: la ruta vive en AuthController y exige el rol Administrador.
        public Task<(bool Ok, string Mensaje)> ForzarRestablecimientoAsync(Guid id) => EnviarAsync(
            HttpMethod.Post,
            $"api/auth/{id}/forzar-restablecimiento",
            new { },
            "No se pudo forzar el restablecimiento.");

        private async Task<(bool Ok, string Mensaje)> EnviarAsync(HttpMethod metodo, string url, object body, string mensajePorDefecto)
        {
            using var request = new HttpRequestMessage(metodo, url)
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _sesion.Token);

            var response = await _http.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _sesion.CerrarAsync();
                return (false, "Tu sesión no es válida.");
            }
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

                if (doc.RootElement.TryGetProperty("mensaje", out var valor) && valor.ValueKind == JsonValueKind.String)
                {
                    return valor.GetString()!;
                }
            }
            catch (JsonException)
            {
                //Cuerpo vacio del JSON: se usa el mensaje por defecto
            }
            return defecto;
        }
    }
}