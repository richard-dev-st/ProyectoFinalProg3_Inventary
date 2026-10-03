using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Core.Application.Configurations;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services
{
    public class JwtService : IJwtService
    {
        private readonly JwtSettings _jwtSettings;

        public JwtService(JwtSettings jwtSettings)
        {
            _jwtSettings = jwtSettings;
        }

        public string GenerarToken(Usuario usuario)
        {
            //Armamos la clave de la firma a partir de la llave configurada
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            //Armamos las credenciales de firma clave y algoritmo
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            //Armamos los claims (afirmaciones sobre el usuario)
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), //Identificador único del 
                new Claim(ClaimTypes.Role, usuario.Rol.ToString()) // <-- Asegura que el claim de rol se mapee correctamente
            };

            //Armamos el objeto del token
            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(_jwtSettings.ExpirationInHours),
                signingCredentials: credentials
                );

            //Convertir el objeto token a string
            var handler = new JwtSecurityTokenHandler();
            var tokenComoString = handler.WriteToken(token);

            //Devolvemos el token como string
            return tokenComoString;
        }

    }
}