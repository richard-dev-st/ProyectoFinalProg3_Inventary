using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Inventario.Web.Services
{
    //Esta es la clase que Blazor usa para saber si hay un usuario logueado y qué claims tiene. La usamos para que el menú de navegación sepa si mostrar o no los links de administración.
    //Se registra en Program.cs como AuthenticationStateProvider, y se inyecta en el layout MainLayout.razor para que el menú sepa si mostrar o no los links de administración.
    public class SesionAuthStateProvider : AuthenticationStateProvider
    {
        private readonly SesionActual _sesion;

        public SesionAuthStateProvider(SesionActual sesion)
        {
            _sesion = sesion;
            _sesion.Cambio += NotificarCambioSesion;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (!_sesion.Autenticado)
            {
                var anonimo = new ClaimsPrincipal(new ClaimsIdentity());
                return await Task.FromResult(new AuthenticationState(anonimo));
            }

            //Claims que queremos que Blazor conozca. Por ejemplo, el ClaimTypes.Name se usa para mostrar el nombre del usuario en el menú de navegación.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, _sesion.Email ?? string.Empty)
            };

            if (!string.IsNullOrWhiteSpace(_sesion.Rol))
            {
                claims.Add(new Claim(ClaimTypes.Role, _sesion.Rol));
            }

            //El AuthenticationType no puede ser null, aunque no lo usemos. Ponemos un nombre cualquiera.
            var identidad = new ClaimsIdentity(claims, authenticationType: "SesionInventario");

            return await Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identidad)));
        }

        //Cuando la sesión cambia (login o logout), avisamos a Blazor para que vuelva a llamar a GetAuthenticationStateAsync y actualice el menú de navegación.
        private void NotificarCambioSesion()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}