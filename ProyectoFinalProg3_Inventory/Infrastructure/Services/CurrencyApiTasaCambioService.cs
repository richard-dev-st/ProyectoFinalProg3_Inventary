using Application.Configurations;
using Application.DTOs.Divisa;
using Application.Exceptions;
using Application.Interfaces;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Services
{
    /// <summary>
    /// Cliente tipado de currencyapi.com.
    /// Obtiene la tasa de cambio y traduce cualquier falla a una ServicioExternoException.
    /// </summary>
    public class CurrencyApiTasaCambioService : ITasaCambioService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiExternaSettings _settings;

        public CurrencyApiTasaCambioService(
            HttpClient httpClient,
            IOptions<ApiExternaSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
        }

        public async Task<TasaCambioDto> ObtenerTasaAsync(
            string origen,
            string destino,
            CancellationToken ct = default)
        {
            // Sin clave no se llama al proveedor: se falla de forma controlada.
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                throw new ServicioExternoException(
                    "La clave de la API externa no está configurada.",
                    ServicioExternoException.Causa.NoConfigurado);
            }

            if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
            {
                throw new ServicioExternoException(
                    "La URL de la API externa no está configurada.",
                    ServicioExternoException.Causa.NoConfigurado);
            }

            var monedaOrigen = origen.ToUpperInvariant();
            var monedaDestino = destino.ToUpperInvariant();

            var url = $"{_settings.BaseUrl.TrimEnd('/')}/latest" +
                      $"?base_currency={Uri.EscapeDataString(monedaOrigen)}" +
                      $"&currencies={Uri.EscapeDataString(monedaDestino)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            // La clave viaja en una cabecera, no en la URL, para que no quede en logs.
            request.Headers.Add("apikey", _settings.ApiKey);

            // Timeout propio sin tocar el HttpClient compartido.
            var segundos = _settings.TimeoutSeconds > 0 ? _settings.TimeoutSeconds : 10;
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(segundos));

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new ServicioExternoException(
                    "La API externa no respondió a tiempo.",
                    ServicioExternoException.Causa.Timeout);
            }
            catch (HttpRequestException ex)
            {
                throw new ServicioExternoException(
                    "No se pudo contactar la API externa.",
                    ServicioExternoException.Causa.RechazadoPorProveedor,
                    ex);
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    // 401/403/429 y errores 5xx del proveedor se tratan igual: no hay tasa disponible.
                    throw new ServicioExternoException(
                        $"La API externa respondió {(int)response.StatusCode}.",
                        ServicioExternoException.Causa.RechazadoPorProveedor);
                }

                CurrencyApiRespuesta? payload;
                try
                {
                    payload = await response.Content
                        .ReadFromJsonAsync<CurrencyApiRespuesta>(cancellationToken: ct);
                }
                catch (Exception ex) when (ex is JsonException or NotSupportedException)
                {
                    throw new ServicioExternoException(
                        "La API externa devolvió una respuesta inválida.",
                        ServicioExternoException.Causa.RespuestaInvalida,
                        ex);
                }

                if (payload?.Data is null ||
                    !payload.Data.TryGetValue(monedaDestino, out var moneda) ||
                    moneda.Value <= 0)
                {
                    throw new ServicioExternoException(
                        "La API externa no devolvió la tasa solicitada.",
                        ServicioExternoException.Causa.RespuestaInvalida);
                }

                return new TasaCambioDto(
                    moneda.Value,
                    monedaOrigen,
                    monedaDestino,
                    payload.Meta?.LastUpdatedAt.UtcDateTime ?? DateTime.UtcNow);
            }
        }

        // Forma de la respuesta de currencyapi.com/v3/latest
        private sealed class CurrencyApiRespuesta
        {
            [JsonPropertyName("meta")]
            public Meta? Meta { get; set; }

            [JsonPropertyName("data")]
            public Dictionary<string, Moneda>? Data { get; set; }
        }

        private sealed class Meta
        {
            [JsonPropertyName("last_updated_at")]
            public DateTimeOffset LastUpdatedAt { get; set; }
        }

        private sealed class Moneda
        {
            [JsonPropertyName("value")]
            public decimal Value { get; set; }
        }
    }
}
