using Microsoft.AspNetCore.SignalR;
using Models;

public class BackgroundTTDService : IBackgroundTTDService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly IHubContext<BackgroundTaskHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;

    public BackgroundTTDService(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        IHubContext<BackgroundTaskHub> hubContext,
        IServiceScopeFactory scopeFactory)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
    }

    public async Task FetchRemainingThingsToDoAsync(City city)
    {
        try
        {
            Console.WriteLine($"TETTTTTTTTTTTTTTTTTTTTTTTTTTTTTT@@@@@@@@@@@@@@@@@{city.Name}");
            var httpClient = _httpClientFactory.CreateClient();

            var apiKey = _config["ApiKeys:OpenTripMap"];
            var kinds = "historic,cultural,architecture,monuments,sport,beaches,museums,religion,natural";
            var url = $"https://api.opentripmap.com/0.1/en/places/radius?radius=10000&lon={city.Longitude}&lat={city.Latitude}&rate=3&format=json&limit=50&kinds={kinds}&lang=en&apikey={apiKey}";

            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadAsStringAsync();

            // Create a scope here to get scoped services
            using var scope = _scopeFactory.CreateScope();
            var attractionService = scope.ServiceProvider.GetRequiredService<AttractionsService>();

            await attractionService.ParseAttractionsFromJson(json, city);

            await _hubContext.Clients.All.SendAsync("ThingsToDoUpdated", city.Name);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Background fetch error: {ex.Message}");
        }
    }

}
