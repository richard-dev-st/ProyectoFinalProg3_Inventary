namespace Core.Domain.Constants
{
    /// <summary>
    /// Entidades sobre las que se registra auditoría (RF-AUD-02..05).
    /// El valor debe coincidir con el filtro por entidad de RF-AUD-07.
    /// </summary>
    public static class EntidadesAuditoria
    {
        public const string SolicitudPermiso = "SolicitudPermiso";
        public const string Documento = "Documento";
        public const string Usuario = "Usuario";

        public static readonly string[] Todas =
            { SolicitudPermiso, Documento, Usuario };
    }
}
