using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Inventario.Web.Services
{
    public class SesionActual
    {
        private const string ClaveToken = "sesion.token";
        private const string ClaveEmail = "sesion.email";
        private const string ClaveRol = "sesion.rol";
        private const string ClaveExpiracion = "sesion.expiracion";

        private readonly ProtectedSessionStorage _storage;
        private bool _cargada;

        public SesionActual(ProtectedSessionStorage storage)
        {
            _storage = storage;
        }

        public string? Token { get; private set; }
        public string? Email { get; private set; }
        public string? Rol { get; private set; }
        public DateTime? Expiracion { get; private set; }

        public event Action? Cambio;

        //¿Hay sesion utilizable? Token presente y no caducado
        public bool Autenticado =>
            !string.IsNullOrWhiteSpace(Token) && (Expiracion is null || Expiracion.Value > DateTime.UtcNow);

        //Lee del navegador una sola vez por circuito
        public async Task CargarAsync()
        {
            if (_cargada) return;
            _cargada = true;

            try
            {
                var token = await _storage.GetAsync<string>(ClaveToken);
                if (!token.Success || string.IsNullOrWhiteSpace(token.Value)) return;

                var email = await _storage.GetAsync<string>(ClaveEmail);
                var rol = await _storage.GetAsync<string>(ClaveRol);
                var expiracion = await _storage.GetAsync<DateTime>(ClaveExpiracion);

                Token = token.Value;
                Email = email.Success ? email.Value : null;
                Rol = rol.Success ? rol.Value : null;
                Expiracion = expiracion.Success ? expiracion.Value : null;

                if (Expiracion is not null && Expiracion.Value <= DateTime.UtcNow)
                {
                    await CerrarAsync(); // Estaba caducado: limpiamos
                }
            }
            catch
            {
                //Si las claves de cifrado cambiaron, el dato viejo es ilegible.
                await CerrarAsync();
            }
        }

        //Guarda en memoria y en el navegador.
        public async Task IniciarAsync(string token, string email, string rol, DateTime expiracion)
        {
            Token = token;
            Email = email;
            Rol = rol;
            Expiracion = expiracion;

            await _storage.SetAsync(ClaveToken, token);
            await _storage.SetAsync(ClaveEmail, email);
            await _storage.SetAsync(ClaveRol, rol);
            await _storage.SetAsync(ClaveExpiracion, expiracion);

            Cambio?.Invoke();
        }

        //Borra de memoria y del navegador
        public async Task CerrarAsync()
        {
            Token = null;
            Email = null;
            Rol = null;
            Expiracion = null;

            await _storage.DeleteAsync(ClaveToken);
            await _storage.DeleteAsync(ClaveEmail);
            await _storage.DeleteAsync(ClaveRol);
            await _storage.DeleteAsync(ClaveExpiracion);
            //Cuando el login guarda o borra, avisa a quien esté escuchando (por ejemplo, el layout que muestra el menú)
            Cambio?.Invoke();
        }
    }
}