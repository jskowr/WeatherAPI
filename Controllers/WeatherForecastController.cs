using Microsoft.AspNetCore.Mvc;
using WeatherAPI.DTOs;
using WeatherAPI.Services;

namespace WeatherAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class WeatherForecastController : ControllerBase
    {
        private readonly IWeatherForecastAggregator _aggregator;
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(
            IWeatherForecastAggregator aggregator,
            ILogger<WeatherForecastController> logger)
        {
            _aggregator = aggregator;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ForecastResponse>> Get(
            [FromQuery] DateOnly date,
            [FromQuery] string city,
            [FromQuery] string country,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(country))
            {
                return BadRequest("City and country are required.");
            }

            var response = await _aggregator.GetForecastAsync(date, city, country, cancellationToken);
            return Ok(response);
        }
    }
}
