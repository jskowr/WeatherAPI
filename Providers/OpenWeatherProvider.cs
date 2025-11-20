using Microsoft.Extensions.Options;
using WeatherAPI.DTOs;
using WeatherAPI.Options;

namespace WeatherAPI.Providers;

public sealed class OpenWeatherProvider : IWeatherProvider
{
    private readonly HttpClient _httpClient;
    private readonly OpenWeatherOptions _options;
    private readonly ILogger<OpenWeatherProvider> _logger;

    public string Name => "OpenWeather";

    public OpenWeatherProvider(
        HttpClient httpClient,
        IOptions<OpenWeatherOptions> options,
        ILogger<OpenWeatherProvider> logger)
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
        var geoUri = $"{_options.BaseUrl}/geo/1.0/direct?q={Uri.EscapeDataString(city)},{Uri.EscapeDataString(country)}&limit=1&appid={_options.ApiKey}";
        var geoResponse = await _httpClient.GetFromJsonAsync<List<OpenWeatherGeoResponse>>(geoUri, cancellationToken);

        var location = geoResponse?.FirstOrDefault();
        if (location is null)
        {
            _logger.LogWarning("OpenWeather could not resolve location for {City}, {Country}", city, country);
            return null;
        }

        var forecastUri =
            $"{_options.BaseUrl}/data/2.5/forecast?lat={location.Lat}&lon={location.Lon}&units=metric&appid={_options.ApiKey}";

        var forecastResponse = await _httpClient.GetFromJsonAsync<OpenWeatherForecastResponse>(forecastUri, cancellationToken);
        if (forecastResponse?.List is null || forecastResponse.List.Count == 0)
        {
            _logger.LogWarning("OpenWeather returned no forecast data for {City}, {Country}", city, country);
            return null;
        }

        var targetDateTime = date.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)));
        var bestMatch = forecastResponse.List
            .OrderBy(x => Math.Abs((DateTimeOffset.FromUnixTimeSeconds(x.Dt).UtcDateTime - targetDateTime).TotalHours))
            .FirstOrDefault();

        if (bestMatch is null)
            return null;

        var temperature = (decimal)bestMatch.Main.Temp;
        var summary = bestMatch.Weather?.FirstOrDefault()?.Description ?? "N/A";

        return new ProviderForecast(
            ProviderName: Name,
            TemperatureCelsius: temperature,
            Summary: summary,
            SourceUrl: "https://openweathermap.org/");
    }

    private sealed class OpenWeatherGeoResponse
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
    }

    private sealed class OpenWeatherForecastResponse
    {
        public List<ForecastItem> List { get; set; } = new();

        public sealed class ForecastItem
        {
            public long Dt { get; set; }
            public MainInfo Main { get; set; } = new();
            public List<WeatherInfo> Weather { get; set; } = new();
        }

        public sealed class MainInfo
        {
            public double Temp { get; set; }
        }

        public sealed class WeatherInfo
        {
            public string Description { get; set; } = string.Empty;
        }
    }
}