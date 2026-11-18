using Inventario.Web.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Inventario.Web.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly SesionActual _sesionActual;

        public AuthService(HttpClient httpClient, SesionActual sesionActual)
        {
            _httpClient = httpClient;
            _sesionActual = sesionActual;
        }

        public async Task<(bool Ok, string Error)> LoginAsync(LoginRequest request)
        {
            //Implementamos la logica
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
            //Si la respuesta da error, retornamos false
            if (!response.IsSuccessStatusCode)
            {
                return (false, await LeerErrorAsync(response));
            }
            //Si responde 200, leemos el token
            //Este bloque hace que el contenido de la respuesta se lea como un objeto de tipo LoginResponse
            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (loginResponse is null) return (false, "Respuesta inesperada del servidor");

            //Guarda en memoria y en session storage los datos de la sesión actual
            await _sesionActual.IniciarAsync(loginResponse.Token, loginResponse.Email, loginResponse.Rol, loginResponse.Expiracion);
            return (true, string.Empty);
        }

        public async Task<UsuarioSesion?> ObtenerUsuarioActualAsync()
        {
            if (!_sesionActual.Autenticado)
            {
                return null;
            }
            //Hacemos la solicitud para obtener los datos del usuario actual
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");

            //Agregamos el token al encabezado de autorización
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _sesionActual.Token);

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _sesionActual.CerrarAsync();
                return null;
            }

            //Si la respuesta da error, retornamos null
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            //Si responde 200, leemos los datos del usuario actual
            return await response.Content.ReadFromJsonAsync<UsuarioSesion>();
        }

        public async Task LogoutAsync()
        {
            if (_sesionActual.Autenticado)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _sesionActual.Token);
                await _httpClient.SendAsync(request);
            }

            await _sesionActual.CerrarAsync();
        }

        private static async Task<string> LeerErrorAsync(HttpResponseMessage response)
        {
            try
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                foreach (var clave in new[] {"detail", "message", "mensaje", "title", "error" })
                {
                    if(doc.RootElement.TryGetProperty(clave, out var valor) && valor.ValueKind == JsonValueKind.String)
                    {
                        return valor.GetString()!;
                    }
                }
            }
            catch
            {
                return $"Error inesperado: {response.StatusCode}";
            }

            return "No se pudo iniciar sesión.";
        }
    }
}