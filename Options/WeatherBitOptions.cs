namespace WeatherAPI.Options
{
    public sealed class WeatherBitOptions
    {
        public const string SectionName = "WeatherBit";
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }
}
