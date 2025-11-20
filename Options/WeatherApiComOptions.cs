namespace WeatherAPI.Options
{
    public sealed class WeatherApiComOptions
    {
        public const string SectionName = "WeatherApiCom";
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }
}
