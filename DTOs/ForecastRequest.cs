namespace WeatherAPI.DTOs;

public sealed record ForecastRequest(
    DateOnly Date,
    string City,
    string Country);
