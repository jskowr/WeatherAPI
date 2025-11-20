using WeatherAPI.DTOs;

namespace WeatherAPI.Services
{
    public interface IWeatherForecastAggregator
    {
        Task<ForecastResponse> GetForecastAsync(
            DateOnly date,
            string city,
            string country,
            CancellationToken cancellationToken = default);
    }
}
