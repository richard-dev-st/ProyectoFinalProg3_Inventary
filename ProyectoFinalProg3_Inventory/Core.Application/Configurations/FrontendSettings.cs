namespace Core.Application.Configurations
{
    // RF-CA-15 / RF-CA-17: URL pública del frontend usada en los enlaces de activación.
    public class FrontendSettings
    {
        public const string SectionName = "Frontend";

        // Si está vacía se conserva el comportamiento anterior: el enlace apunta a la API.
        public string BaseUrl { get; set; } = string.Empty;
    }
}
