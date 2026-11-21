using Inventario.Web.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Inventario.Web.Services
{
    public class AuthService
    {
        private const string MensajeReenvio =
            "Si el correo está registrado y no ha sido activado, se ha enviado un nuevo enlace de activación.";

        private readonly HttpClient _httpClient;
        private readonly SesionActual _sesionActual;

        public AuthService(HttpClient httpClient, SesionActual sesionActual)
        {
            _httpClient = httpClient;
            _sesionActual = sesionActual;
        }

        public async Task<(bool Ok, string Mensaje)> RegistrarAsync(RegistroRequest request, string baseUrl)
        {
            // RF-CA-15: la API exige este campo aunque después usa su URL configurada.
            request.BaseUrl = baseUrl;
            var response = await _httpClient.PostAsJsonAsync("api/auth/registrar", request);
            return await LeerRespuestaApiAsync(response, "No se pudo completar el registro.");
        }

        public async Task<(bool Ok, string Mensaje)> ActivarAsync(string token)
        {
            var response = await _httpClient.GetAsync($"api/auth/activar?token={Uri.EscapeDataString(token)}");
            return await LeerRespuestaApiAsync(response, "No se pudo activar la cuenta.");
        }

        public async Task<string> ReenviarActivacionAsync(string email)
        {
            try
            {
                await _httpClient.PostAsync(
                    $"api/auth/reenviar-activacion?email={Uri.EscapeDataString(email)}",
                    content: null);
            }
            catch (HttpRequestException)
            {
                // RF-CA-17: ni un fallo de red debe revelar si el correo existe.
            }

            return MensajeReenvio;
        }

        public async Task<(bool Ok, string Error)> LoginAsync(LoginRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
            if (!response.IsSuccessStatusCode)
            {
                return (false, await LeerErrorAsync(response));
            }

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (loginResponse is null) return (false, "Respuesta inesperada del servidor");

            await _sesionActual.IniciarAsync(loginResponse.Token, loginResponse.Email, loginResponse.Rol, loginResponse.Expiracion);
            return (true, string.Empty);
        }

        public async Task<UsuarioSesion?> ObtenerUsuarioActualAsync()
        {
            if (!_sesionActual.Autenticado)
            {
                return null;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _sesionActual.Token);

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _sesionActual.CerrarAsync();
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

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
            var (_, mensaje) = await LeerRespuestaApiAsync(response, "No se pudo iniciar sesión.");
            return mensaje;
        }

        private static async Task<(bool Ok, string Mensaje)> LeerRespuestaApiAsync(
            HttpResponseMessage response,
            string mensajePorDefecto)
        {
            try
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var raiz = doc.RootElement;

                // RF-CA-01/14/15/16: la API responde { exito, mensaje } también en los errores.
                if (raiz.TryGetProperty("exito", out var exito)
                    && exito.ValueKind is JsonValueKind.True or JsonValueKind.False
                    && raiz.TryGetProperty("mensaje", out var mensaje)
                    && mensaje.ValueKind == JsonValueKind.String)
                {
                    return (response.IsSuccessStatusCode && exito.GetBoolean(), mensaje.GetString()!);
                }

                foreach (var clave in new[] { "detail", "message", "mensaje", "title", "error" })
                {
                    if (raiz.TryGetProperty(clave, out var valor) && valor.ValueKind == JsonValueKind.String)
                    {
                        return (response.IsSuccessStatusCode, valor.GetString()!);
                    }
                }
            }
            catch (JsonException)
            {
                // Cuerpo no JSON: se usa el mensaje por defecto.
            }

            return (false, response.IsSuccessStatusCode
                ? mensajePorDefecto
                : $"Error inesperado: {(int)response.StatusCode}");
        }
    }
}
