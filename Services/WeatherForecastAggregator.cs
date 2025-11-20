using Microsoft.Extensions.Caching.Memory;
using WeatherAPI.DTOs;
using WeatherAPI.Providers;

namespace WeatherAPI.Services
{
    public sealed class WeatherForecastAggregator : IWeatherForecastAggregator
    {
        private readonly IEnumerable<IWeatherProvider> _providers;
        private readonly IMemoryCache _cache;
        private readonly ILogger<WeatherForecastAggregator> _logger;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        public WeatherForecastAggregator(
            IEnumerable<IWeatherProvider> providers,
            IMemoryCache cache,
            ILogger<WeatherForecastAggregator> logger)
        {
            _providers = providers;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ForecastResponse> GetForecastAsync(
            DateOnly date,
            string city,
            string country,
            CancellationToken cancellationToken = default)
        {
            var normalizedCity = city.Trim();
            var normalizedCountry = country.Trim();
            var cacheKey = BuildCacheKey(date, normalizedCity, normalizedCountry);

            if (_cache.TryGetValue<ForecastResponse>(cacheKey, out var cached))
            {
                _logger.LogInformation("Cache hit for {CacheKey}", cacheKey);
                return cached!;
            }

            _logger.LogInformation("Cache miss for {CacheKey}. Querying providers...", cacheKey);

            var providerTasks = _providers
                .Select(p => GetSafeForecast(p, date, normalizedCity, normalizedCountry, cancellationToken))
                .ToArray();

            var results = await Task.WhenAll(providerTasks);
            var forecasts = results
                .Where(r => r is not null)
                .Cast<ProviderForecast>()
                .ToArray();

            var response = new ForecastResponse(
                Date: date,
                City: normalizedCity,
                Country: normalizedCountry,
                ProviderForecasts: forecasts);

            _cache.Set(cacheKey, response, CacheDuration);

            return response;
        }

        private static string BuildCacheKey(DateOnly date, string city, string country)
            => $"forecast:{date:yyyy-MM-dd}:{city.ToLowerInvariant()}:{country.ToLowerInvariant()}";

        private async Task<ProviderForecast?> GetSafeForecast(
            IWeatherProvider provider,
            DateOnly date,
            string city,
            string country,
            CancellationToken cancellationToken)
        {
            try
            {
                return await provider.GetForecastAsync(date, city, country, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while calling provider {ProviderName}", provider.Name);
                return null;
            }
        }
    }
}
