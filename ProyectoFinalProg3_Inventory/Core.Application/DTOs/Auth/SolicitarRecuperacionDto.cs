using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Auth
{
    public record SolicitarRecuperacionDto
    {
        // Dto para recibir la peticion de solicitud de recuperacion de contraseña
        public string Email { get; init; } = string.Empty;
    }
}
