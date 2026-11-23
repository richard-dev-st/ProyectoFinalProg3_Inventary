namespace Core.Domain.Constants
{
    /// <summary>
    /// Vocabulario controlado de acciones auditadas (RF-AUD-01..05).
    /// Se usan valores estables para que los filtros y las pruebas no dependan de texto libre.
    /// </summary>
    public static class AccionesAuditoria
    {
        public const string Aprobar = "Aprobar";
        public const string Rechazar = "Rechazar";
        public const string Crear = "Crear";
        public const string Eliminar = "Eliminar";
        public const string CambiarRol = "CambiarRol";
        public const string Desactivar = "Desactivar";
        public const string Reactivar = "Reactivar";
        public const string RestablecerPassword = "RestablecerPassword";
    }
}
