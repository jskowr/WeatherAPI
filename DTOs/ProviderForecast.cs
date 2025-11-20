namespace WeatherAPI.DTOs;

public sealed record ProviderForecast(
    string ProviderName,
    decimal TemperatureCelsius,
    string Summary,
    string SourceUrl);
