using WeatherAPI.DTOs;

namespace WeatherAPI.Providers;

public interface IWeatherProvider
{
    string Name { get; }

    Task<ProviderForecast?> GetForecastAsync(
        DateOnly date,
        string city,
        string country,
        CancellationToken cancellationToken = default);
}
