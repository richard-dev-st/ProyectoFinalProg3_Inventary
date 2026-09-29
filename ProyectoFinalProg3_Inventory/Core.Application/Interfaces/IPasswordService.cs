namespace Application.Interfaces
{
    public interface IPasswordService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string passwordHash);
        bool ValidarPolitica(string password, out string mensajeError);
    }
}
