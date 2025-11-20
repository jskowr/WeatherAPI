using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using WeatherAPI.DTOs;
using WeatherAPI.Options;

namespace WeatherAPI.Providers;

public sealed class WeatherApiComProvider : IWeatherProvider
{
    private readonly HttpClient _httpClient;
    private readonly WeatherApiComOptions _options;
    private readonly ILogger<WeatherApiComProvider> _logger;

    public string Name => "WeatherApiCom";

    public WeatherApiComProvider(
        HttpClient httpClient,
        IOptions<WeatherApiComOptions> options,
        ILogger<WeatherApiComProvider> logger)
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
        var locationQuery = $"{city},{country}";
        var encodedLocation = Uri.EscapeDataString(locationQuery);

        var requestUri =
            $"{_options.BaseUrl}/v1/forecast.json?key={_options.ApiKey}&q={encodedLocation}&days=3&aqi=no&alerts=no";

        WeatherApiForecastResponse? apiResponse;

        try
        {
            apiResponse = await _httpClient
                .GetFromJsonAsync<WeatherApiForecastResponse>(requestUri, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error calling WeatherAPI.com for {City}, {Country}, date {Date}",
                city, country, date);
            return null;
        }

        if (apiResponse?.Forecast?.ForecastDay == null ||
            apiResponse.Forecast.ForecastDay.Count == 0)
        {
            if (apiResponse?.Current is not null)
            {
                var temp = (decimal)apiResponse.Current.TempC;
                var summary = apiResponse.Current.Condition?.Text ?? "N/A";

                return new ProviderForecast(
                    ProviderName: Name,
                    TemperatureCelsius: temp,
                    Summary: summary,
                    SourceUrl: "https://www.weatherapi.com/");
            }

            _logger.LogWarning(
                "WeatherAPI.com returned no forecast and no current data for {City}, {Country}, date {Date}",
                city, country, date);

            return null;
        }

        var targetDateString = date.ToString("yyyy-MM-dd");
        var day = apiResponse.Forecast.ForecastDay
            .FirstOrDefault(d => string.Equals(d.Date, targetDateString, StringComparison.Ordinal));

        day ??= apiResponse.Forecast.ForecastDay.First();

        var tempC = (decimal)day.Day.AvgTempC;
        var summaryText = day.Day.Condition?.Text ?? "N/A";

        return new ProviderForecast(
            ProviderName: Name,
            TemperatureCelsius: tempC,
            Summary: summaryText,
            SourceUrl: "https://www.weatherapi.com/");
    }

    private sealed class WeatherApiForecastResponse
    {
        public Location? Location { get; set; }
        public Current? Current { get; set; }
        public Forecast? Forecast { get; set; }
    }

    private sealed class Current
    {
        [JsonPropertyName("temp_c")]
        public double TempC { get; set; }

        [JsonPropertyName("condition")]
        public Condition? Condition { get; set; }
    }

    private sealed class Location
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("country")]
        public string Country { get; set; } = string.Empty;
    }

    private sealed class Forecast
    {
        [JsonPropertyName("forecastday")]
        public List<ForecastDay> ForecastDay { get; set; } = new();
    }

    private sealed class ForecastDay
    {
        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("day")]
        public Day Day { get; set; } = new();
    }

    private sealed class Day
    {
        [JsonPropertyName("avgtemp_c")]
        public double AvgTempC { get; set; }

        [JsonPropertyName("maxtemp_c")]
        public double MaxTempC { get; set; }

        [JsonPropertyName("mintemp_c")]
        public double MinTempC { get; set; }

        [JsonPropertyName("condition")]
        public Condition? Condition { get; set; }
    }

    private sealed class Condition
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = string.Empty;
    }
}
