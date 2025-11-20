namespace WeatherAPI.DTOs;

public sealed record ForecastResponse(
    DateOnly Date,
    string City,
    string Country,
    IReadOnlyCollection<ProviderForecast> ProviderForecasts);
