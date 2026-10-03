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

        //RF-CA-09: Iniciar recuperacion enviando codigo por correo
        Task SolicitarRecuperacionPasswordAsync(SolicitarRecuperacionDto dto);

        //RF-CA-10 / RF-CA-11 / RF-CA-12: Restablecer password con el codigo enviado por correo
        Task RestablecerPasswordAsync(RestablecerPasswordDto dto);

        //RF-CA-22: Cambiar password con sesion activa
        Task CambiarPasswordAsync(Guid usuarioId, CambiarPasswordDto dto);

        //RF-CA-13: Administrador fuerza el envio de un codigo de recuperacion
        Task ForzarRestablecimientoPasswordAsync(Guid usuarioId);


    }
}
