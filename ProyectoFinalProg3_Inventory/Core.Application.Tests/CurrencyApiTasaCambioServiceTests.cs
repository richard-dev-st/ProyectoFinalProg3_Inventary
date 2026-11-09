using System.Net;
using System.Text;
using Application.Configurations;
using Application.Exceptions;
using Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace Core.Application.Tests;

public class CurrencyApiTasaCambioServiceTests
{
    //Handler falso: reemplaza la red real por una función que decide la respuesta.
    private sealed class HandlerFalso : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;

        public HandlerFalso(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
            => _responder = responder;

        public HttpRequestMessage? UltimaPeticion { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            UltimaPeticion = request;
            return _responder(request, cancellationToken);
        }
    }

    private static CurrencyApiTasaCambioService CrearServicio(
        HttpMessageHandler handler,
        string apiKey = "clave-de-prueba",
        int timeoutSeconds = 5)
    {
        var http = new HttpClient(handler);

        var settings = Options.Create(new ApiExternaSettings
        {
            BaseUrl = "https://api.currencyapi.com/v3",
            ApiKey = apiKey,
            TimeoutSeconds = timeoutSeconds
        });

        return new CurrencyApiTasaCambioService(http, settings);
    }

    private static HttpResponseMessage Ok(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    [Fact]
    public async Task RespuestaValida_DevuelveTasaYUsaCabecera()
    {
        var handler = new HandlerFalso((_, _) => Task.FromResult(Ok(
            """{"meta":{"last_updated_at":"2026-11-09T00:00:00Z"},"data":{"DOP":{"code":"DOP","value":59.42}}}""")));

        var tasa = await CrearServicio(handler).ObtenerTasaAsync("USD", "DOP");

        Assert.Equal(59.42m, tasa.Tasa);
        Assert.Equal("USD", tasa.MonedaOrigen);
        Assert.Equal("DOP", tasa.MonedaDestino);

        //La clave debe ir en la cabecera y no en la URL.
        Assert.Equal(
            "clave-de-prueba",
            handler.UltimaPeticion!.Headers.GetValues("apikey").Single());
        Assert.DoesNotContain("clave-de-prueba", handler.UltimaPeticion.RequestUri!.Query);
    }

    [Fact]
    public async Task SinClave_LanzaNoConfiguradoSinLlamar()
    {
        var llamado = false;

        var handler = new HandlerFalso((_, _) =>
        {
            llamado = true;
            return Task.FromResult(Ok("{}"));
        });

        var ex = await Assert.ThrowsAsync<ServicioExternoException>(
            () => CrearServicio(handler, apiKey: "").ObtenerTasaAsync("USD", "DOP"));

        Assert.Equal(ServicioExternoException.Causa.NoConfigurado, ex.Motivo);
        Assert.False(llamado);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task ProveedorRechaza_LanzaRechazado(HttpStatusCode status)
    {
        var handler = new HandlerFalso((_, _) =>
            Task.FromResult(new HttpResponseMessage(status)));

        var ex = await Assert.ThrowsAsync<ServicioExternoException>(
            () => CrearServicio(handler).ObtenerTasaAsync("USD", "DOP"));

        Assert.Equal(ServicioExternoException.Causa.RechazadoPorProveedor, ex.Motivo);
    }

    [Fact]
    public async Task JsonInvalido_LanzaRespuestaInvalida()
    {
        var handler = new HandlerFalso((_, _) =>
            Task.FromResult(Ok("esto no es json")));

        var ex = await Assert.ThrowsAsync<ServicioExternoException>(
            () => CrearServicio(handler).ObtenerTasaAsync("USD", "DOP"));

        Assert.Equal(ServicioExternoException.Causa.RespuestaInvalida, ex.Motivo);
    }

    [Fact]
    public async Task SinMonedaDestino_LanzaRespuestaInvalida()
    {
        var handler = new HandlerFalso((_, _) => Task.FromResult(Ok(
            """{"meta":{"last_updated_at":"2026-11-09T00:00:00Z"},"data":{}}""")));

        var ex = await Assert.ThrowsAsync<ServicioExternoException>(
            () => CrearServicio(handler).ObtenerTasaAsync("USD", "DOP"));

        Assert.Equal(ServicioExternoException.Causa.RespuestaInvalida, ex.Motivo);
    }

    [Fact]
    public async Task Timeout_LanzaTimeout()
    {
        var handler = new HandlerFalso(async (_, ct) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var ex = await Assert.ThrowsAsync<ServicioExternoException>(
            () => CrearServicio(handler, timeoutSeconds: 1)
                .ObtenerTasaAsync("USD", "DOP"));

        Assert.Equal(ServicioExternoException.Causa.Timeout, ex.Motivo);
    }
}
