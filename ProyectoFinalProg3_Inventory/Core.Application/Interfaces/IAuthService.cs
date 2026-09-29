using System;
using System.Collections.Generic;
using System.Text;
using Application.DTOs.Auth;

namespace Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegistrarAsync(RegistroRequestDto request);
        Task<AuthResponseDto> ActivarCuentaAsync(string token);
        Task<AuthResponseDto> ReenviarActivacionAsync(string email, string baseUrl);
    }
}
