using WeatherAPI.Options;
using WeatherAPI.Providers;
using WeatherAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddMemoryCache();

builder.Services.Configure<OpenWeatherOptions>(
    builder.Configuration.GetSection(OpenWeatherOptions.SectionName));
builder.Services.Configure<WeatherApiComOptions>(
    builder.Configuration.GetSection(WeatherApiComOptions.SectionName));
builder.Services.Configure<WeatherBitOptions>(
    builder.Configuration.GetSection(WeatherBitOptions.SectionName));

builder.Services.AddHttpClient<OpenWeatherProvider>((sp, client) =>
{
    var options = sp.GetRequiredService<
        Microsoft.Extensions.Options.IOptions<OpenWeatherOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});

builder.Services.AddHttpClient<WeatherApiComProvider>((sp, client) =>
{
    var options = sp.GetRequiredService<
        Microsoft.Extensions.Options.IOptions<WeatherApiComOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});

builder.Services.AddHttpClient<WeatherBitProvider>((sp, client) =>
{
    var options = sp.GetRequiredService<
        Microsoft.Extensions.Options.IOptions<WeatherBitOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});

builder.Services.AddTransient<IWeatherProvider, OpenWeatherProvider>();
builder.Services.AddTransient<IWeatherProvider, WeatherApiComProvider>();
builder.Services.AddTransient<IWeatherProvider, WeatherBitProvider>();

builder.Services.AddScoped<IWeatherForecastAggregator, WeatherForecastAggregator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.Run();