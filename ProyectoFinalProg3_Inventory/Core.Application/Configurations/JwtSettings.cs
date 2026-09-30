namespace Core.Application.Configurations
{
    public class JwtSettings
    {
        //Nombre de la sección en el archivo de configuración (appsettings.json)
        public const string SectionName = "JwtSettings";
        //Clave secreta para firmar el token JWT
        public string SecretKey { get; set; } = string.Empty;
        //Emisor del token JWT
        public string Issuer { get; set; } = string.Empty;
        //Audiencia del token JWT
        public string Audience { get; set; } = string.Empty;
        //Tiempo de expiración del token JWT en horas
        public int ExpirationInHours { get; set; } = 8;
    }
}