using System;
using System.Collections.Generic;
using System.Text;
using Application.DTOs.Auth;
using Application.DTOs.Login;

namespace Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegistrarAsync(RegistroRequestDto request);
        Task<AuthResponseDto> ActivarCuentaAsync(string token);
        Task<AuthResponseDto> ReenviarActivacionAsync(string email, string baseUrl);

        //Nuevos metodos del Modulo de Sesion (RF-CA-03, 07, 18, 19)
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task<UsuarioSesionDto> ObtenerUsuarioAutenticadoAsync(Guid usuarioId);
        Task<AuthResponseDto> LogoutAsync(string token);
    }
}
