using Application.Interfaces;
using Core.Domain.Entities;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Text;
using Application.DTOs.Auth;

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordService _passwordService;

        public AuthService(IApplicationDbContext context, IPasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public async Task<AuthResponseDto> ActivarCuentaAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return new AuthResponseDto(false, "El token de activación es requerido.");
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.TokenActivacion == token);

            if (usuario == null)
            {
                return new AuthResponseDto(false, "El token de activación no es válido o no existe.");
            }

            //Llamamos al método ActivarCuenta del usuario para cambiar su estado a activado
            bool activado = usuario.ActivarCuenta(token);

            if (!activado)
            {
                return new AuthResponseDto(false, "No se pudo activar la cuenta. El token puede estar expirado o la cuenta ya está activada.");
            }

            await _context.SaveChangesAsync();

            return new AuthResponseDto(true, "Cuenta activada con éxito. Ya puedes iniciar sesión.");
        }

        public async Task<AuthResponseDto> ReenviarActivacionAsync(string email, string baseUrl)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (usuario == null)
            {
                return new AuthResponseDto(false, "No existe un usuario registrado con ese correo.");
            }

            if (usuario.Activo)
            {
                return new AuthResponseDto(false, "Esta cuenta ya está activada.");
            }

            //Generamos un nuevo token de activación y lo asignamos al usuario
            usuario.GenerarTokenActivacion();

            string enlaceActivacion = $"{baseUrl}/api/auth/activar?token={usuario.TokenActivacion}";
            string cuerpoCorreo = $"<p>Hola,</p><p>Has solicitado un nuevo enlace de activación:</p><p><a href='{enlaceActivacion}'>Activar Cuenta</a>";

            _context.CorreosEnCola.Add(new CorreoEnCola(usuario.Email, "Nuevo enlace de activación", cuerpoCorreo));

            await _context.SaveChangesAsync();

            return new AuthResponseDto(true, "Se ha enviado el correo de activación.");
        }

        public async Task<AuthResponseDto> RegistrarAsync(RegistroRequestDto request)
        {
            // Verificamos si el usuario ya existe
            var existeUsuario = await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());
            if (existeUsuario)
            {
                return new AuthResponseDto(false, "El correo electrónico ya está registrado.");
            }

            string passwordHash = _passwordService.HashPassword(request.Password);

            var nuevoUsuario = new Usuario(request.Email, passwordHash);

            _context.Usuarios.Add(nuevoUsuario);

            string enlaceActivacion = $"{request.BaseUrl}/api/auth/activar?token={nuevoUsuario.TokenActivacion}";
            string cuerpoCorreo = $"<p>Hola,</p><p>Para activar tu cuenta, haz clic en el siguiente enlace:</p><p><a href='{enlaceActivacion}'>Activar Cuenta</a>";

            var correo = new CorreoEnCola(nuevoUsuario.Email, "Confirmación de cuenta", cuerpoCorreo);
            _context.CorreosEnCola.Add(correo);

            await _context.SaveChangesAsync();

            return new AuthResponseDto(true, "Registro exitoso. Se ha enviado un correo para activar tu cuenta.");

        }
    }
}
