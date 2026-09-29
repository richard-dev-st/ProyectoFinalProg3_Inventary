namespace Application.DTOs.Auth
{
    //Dto para recibir la peticion de registro
    public record RegistroRequestDto(string Email, string Password, string BaseUrl);

    //Dto para enviar la respuesta del servicio
    public record AuthResponseDto(bool Exito, string Mensaje);
}