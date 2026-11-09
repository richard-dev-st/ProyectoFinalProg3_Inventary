namespace Application.Configurations
{
    public class ApiExternaSettings
    {
        //Nombre de la sección en appsettings.json y de las variables de entorno.
        public const string SectionName = "ApiExterna";

        //URL base de la API externa.
        public string BaseUrl { get; set; } = string.Empty;

        //Clave de API. Nunca se guarda en el repositorio: llega desde .env o variables de entorno.
        public string ApiKey { get; set; } = string.Empty;

        //Tiempo máximo de espera para la API externa, en segundos.
        public int TimeoutSeconds { get; set; } = 10;
    }
}
