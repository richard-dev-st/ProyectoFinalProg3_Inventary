using Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;

namespace Infrastructure.Services
{
    public class PasswordService : IPasswordService
    {
        //Llamamos a la clase PasswordHasher de Microsoft.AspNetCore.Identity para hashear y verificar contraseñas
        private readonly PasswordHasher<string> _hasher = new();
        //Implementamos la interfaz IPasswordService
        public string HashPassword(string password)
        {
            //Usamos "user" como el usuario, ya que no tenemos un objeto de usuario en este contexto
            return _hasher.HashPassword("user", password);
        }
        public bool ValidarPolitica(string password, out string mensajeError)
        {
            //Esto se encarga de inicializar mensaje error como una cadena vacía
            mensajeError = string.Empty;

            //Validamos que la contraseña no sea nula, vacía o contenga solo espacios en blanco y que tenga al menos 8 caracteres
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            {
                //Si la contraseña no cumple con la política, asignamos un mensaje de error y retornamos false
                mensajeError = "La contraseña debe tener al menos 8 caracteres.";
                return false;
            }

            //Esto se encarga de verificar si la contraseña contiene al menos una letra mayúscula, una letra minúscula, un número y un carácter especial
            bool tieneLetra = Regex.IsMatch(password, @"[a-zA-Z]");
            bool tieneNumero = Regex.IsMatch(password, @"[0-9]");

            if(!tieneLetra || !tieneNumero) 
            {
                mensajeError = "La contraseña debe contener al menos una letra y un número.";
                return false;
            }

            return true;
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            //Declaramos una variable para almacenar el resultado de la verificación de la contraseña
            var result = _hasher.VerifyHashedPassword("user", passwordHash, password);
            //Retornamos el resultado de la verificación, si es diferente de PasswordVerificationResult.Failed, significa que la contraseña es válida
            return result != PasswordVerificationResult.Failed;
        }
    }
}
