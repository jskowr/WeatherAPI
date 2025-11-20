using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using WeatherAPI.DTOs;
using WeatherAPI.Options;
using WeatherAPI.Providers;

public sealed class WeatherBitProvider : IWeatherProvider
{
    private readonly HttpClient _httpClient;
    private readonly WeatherBitOptions _options;
    private readonly ILogger<WeatherBitProvider> _logger;

    public string Name => "WeatherBit";

    public WeatherBitProvider(
        HttpClient httpClient,
        IOptions<WeatherBitOptions> options,
        ILogger<WeatherBitProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ProviderForecast?> GetForecastAsync(
        DateOnly date,
        string city,
        string country,
        CancellationToken cancellationToken = default)
    {
        var encodedCity = Uri.EscapeDataString(city);
        var encodedCountry = Uri.EscapeDataString(country);

        var requestUri =
            $"{_options.BaseUrl}/v2.0/forecast/daily?city={encodedCity}&country={encodedCountry}&days=16&key={_options.ApiKey}";

        WeatherBitForecastResponse? apiResponse;

        try
        {
            apiResponse = await _httpClient
                .GetFromJsonAsync<WeatherBitForecastResponse>(requestUri, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error calling Weatherbit for {City}, {Country}, date {Date}",
                city, country, date);
            return null;
        }

        if (apiResponse?.Data is null || apiResponse.Data.Count == 0)
        {
            _logger.LogWarning(
                "Weatherbit returned no forecast data for {City}, {Country}, date {Date}",
                city, country, date);
            return null;
        }

        var targetDateString = date.ToString("yyyy-MM-dd");

        var day = apiResponse.Data
            .FirstOrDefault(d => string.Equals(d.DateTime, targetDateString, StringComparison.Ordinal));

        day ??= apiResponse.Data.FirstOrDefault();

        if (day is null)
        {
            _logger.LogWarning(
                "Weatherbit response contained data but could not select a day for {City}, {Country}, date {Date}",
                city, country, date);
            return null;
        }

        var tempC = (decimal)day.Temp;
        var description = day.Weather?.Description ?? "N/A";

        return new ProviderForecast(
            ProviderName: Name,
            TemperatureCelsius: tempC,
            Summary: description,
            SourceUrl: "https://www.weatherbit.io/");
    }

    private sealed class WeatherBitForecastResponse
    {
        [JsonPropertyName("data")]
        public List<WeatherBitDay> Data { get; set; } = new();
    }

    private sealed class WeatherBitDay
    {
        [JsonPropertyName("datetime")]
        public string DateTime { get; set; } = string.Empty;

        [JsonPropertyName("temp")]
        public double Temp { get; set; }

        [JsonPropertyName("weather")]
        public WeatherBitCondition? Weather { get; set; }
    }

    private sealed class WeatherBitCondition
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }
}