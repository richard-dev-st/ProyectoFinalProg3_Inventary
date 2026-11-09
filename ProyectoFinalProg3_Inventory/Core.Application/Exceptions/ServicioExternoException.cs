namespace Application.Exceptions
{
    /// <summary>
    /// Error controlado al comunicarse con un servicio externo.
    /// La causa permite que el controlador traduzca el fallo a un código HTTP distinto.
    /// </summary>
    public class ServicioExternoException : Exception
    {
        public enum Causa
        {
            NoConfigurado,
            RechazadoPorProveedor,
            Timeout,
            RespuestaInvalida
        }

        public Causa Motivo { get; }

        public ServicioExternoException(string mensaje, Causa motivo, Exception? interna = null)
            : base(mensaje, interna)
        {
            Motivo = motivo;
        }
    }
}
