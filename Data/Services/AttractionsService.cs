using System.Text.Json;
using FlightCast.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Models;

public class AttractionsService
{
    private readonly AppDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly IHubContext<BackgroundTaskHub> _hubContext;
    private readonly IBackgroundTTDService _backgroundTTDService;

    public AttractionsService(IHttpClientFactory httpClientFactory, IConfiguration config, AppDbContext context, IHubContext<BackgroundTaskHub> hubContext, IBackgroundTTDService backgroundTTDService)
    {
        _backgroundTTDService = backgroundTTDService;
        _hubContext = hubContext;
        _context = context;
        _httpClient = httpClientFactory.CreateClient();
        _config = config;
        _hubContext = hubContext;
    }
    public async Task<List<Attraction>> GetAttractionsFromDBAsync(string city)
    {
        var query = _context.attractions
            .Include(a => a.City)
            .Where(a => a.City.Name == city);
        return await query.ToListAsync();
    }
    public async Task<List<Attraction>> GetAttractionsAsync(string city)
    {
        // Find city in the database by name
        var City = await _context.cities.FirstOrDefaultAsync(c => c.Name == city);

        if (City == null)
        {
            Console.WriteLine($"City '{City}' not found in database.");
            return new List<Attraction>();
        }

        // Make sure city has valid coordinates
        if (City.Latitude == 0 || City.Longitude == 0)
        {
            Console.WriteLine("Warning: City coordinates are zero, skipping API call");
            return new List<Attraction>();
        }
        var apiKey = _config["ApiKeys:OpenTripMap"];
        var kinds = "historic,cultural,architecture,monuments,sport,beaches,museums,religion,natural";
        var url = $"https://api.opentripmap.com/0.1/en/places/radius?radius=10000&lon={City.Longitude}&lat={City.Latitude}&rate=3&format=json&limit=10&kinds={kinds}&lang=en&apikey={apiKey}";

        try
        {
            var response = await SendWithRetryAsync(() => _httpClient.GetAsync(url));

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                Console.WriteLine($"OpenTripMap API rate limit exceeded (429) for URL: {url}");
                return new List<Attraction>();  // Return empty list so app keeps working
            }

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            var initialThingsToDo = await ParseAttractionsFromJson(json, City);

            //background task to fetch re memaining 40
            _ = Task.Run(() => _backgroundTTDService.FetchRemainingThingsToDoAsync(City));
            return initialThingsToDo;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HttpRequestException calling OpenTripMap for URL {url}: {ex.Message}");
            return new List<Attraction>(); // Return empty list on other HTTP errors too
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected exception calling OpenTripMap: {ex.Message}");
            return new List<Attraction>();
        }
    }

    public async Task<List<Attraction>> ParseAttractionsFromJson(string json, City city)
    {

        if (city.Latitude == 0 || city.Longitude == 0)
        {
            Console.WriteLine("Warning: City coordinates are zero, skipping API call");
            return new List<Attraction>();
        }

        // Try to load the city from the database to get a tracked entity
        var cityFromDb = await _context.cities.FirstOrDefaultAsync(c => c.Id == city.Id);

        if (cityFromDb == null)
        {
            Console.WriteLine($"City with Id {city.Id} not found in DB.");

            // Option 1: Add new city but without setting Id manually
            // Clear the Id if it is set, so EF can generate it
            city.Id = 0; // or default(int)
            _context.cities.Add(city);
            await _context.SaveChangesAsync();
            Console.WriteLine("Add it city to db@@@@@@@@@@@@@@@@@@@@@@@@@@");

            cityFromDb = city; // now tracked with new Id
        }
        var attractions = new List<Attraction>();
        var apiKey = _config["ApiKeys:OpenTripMap"];

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // First, collect basic attraction data
        var basicAttractions = new List<(string Xid, string Name, string KindsRaw, double Lat, double Lon)>();

        foreach (var item in root.EnumerateArray())
        {
            try
            {
                string xid = item.GetProperty("xid").GetString() ?? "";
                string name = item.GetProperty("name").GetString() ?? "";
                string kindsRaw = item.GetProperty("kinds").GetString() ?? "";
                var point = item.GetProperty("point");
                double latitude = point.GetProperty("lat").GetDouble();
                double longitude = point.GetProperty("lon").GetDouble();

                basicAttractions.Add((xid, name, kindsRaw, latitude, longitude));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing basic attraction info: {ex.Message}");
            }
        }
        Console.WriteLine($"Total attractions fetched from API: {basicAttractions.Count}");
        var xids = basicAttractions.Select(x => x.Xid).ToList();

        var existingXids = _context.attractions
            .Where(a => xids.Contains(a.Xid))
            .Select(a => a.Xid)
            .ToHashSet();
        Console.WriteLine($"Attractions already in DB: {existingXids.Count}");
        var newAttractions = basicAttractions.Where(x => !existingXids.Contains(x.Xid)).ToList();
        // Prepare tasks for fetching details in parallel
        Console.WriteLine($"New attractions to process: {newAttractions.Count}");
        var detailTasks = newAttractions.Select(async basic =>
        {
            try
            {
                var detailUrl = $"https://api.opentripmap.com/0.1/en/places/xid/{basic.Xid}?lang=en&apikey={apiKey}";

                // Optional small delay to reduce risk of rate limiting per request
                await Task.Delay(100);

                var detailResponse = await SendWithRetryAsync(() => _httpClient.GetAsync(detailUrl));

                if (detailResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    Console.WriteLine("Rate limit hit. Skipping this request.");
                    return null; // or handle differently if you want to retry or abort
                }

                detailResponse.EnsureSuccessStatusCode();

                using var detailDoc = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
                var detail = detailDoc.RootElement;

                string description = "";
                if (detail.TryGetProperty("wikipedia_extracts", out var wikiExtract) &&
                    wikiExtract.TryGetProperty("text", out var textProp))
                {
                    description = textProp.GetString() ?? "";
                }

                string website = detail.TryGetProperty("url", out var urlProp) ? urlProp.GetString() ?? "" : "";

                var categories = basic.KindsRaw.Split(',').Select(c => c.Trim()).ToList();

                var imageUrls = new List<string>();
                if (detail.TryGetProperty("preview", out var preview) &&
                    preview.TryGetProperty("source", out var imageUrl))
                {
                    var img = imageUrl.GetString();
                    if (!string.IsNullOrEmpty(img))
                    {
                        imageUrls.Add(img);
                    }
                }

                string street = "";
                string number = "";
                string cityName = "";
                string country = "";

                if (detail.TryGetProperty("address", out var address))
                {
                    street = address.TryGetProperty("road", out var road) ? road.GetString() ?? "" : "";
                    number = address.TryGetProperty("house_number", out var houseNumber) ? houseNumber.GetString() ?? "" : "";
                    cityName = address.TryGetProperty("city", out var cityProp) ? cityProp.GetString() ?? "" : "";
                    country = address.TryGetProperty("country", out var countryProp) ? countryProp.GetString() ?? "" : "";
                }

                return new Attraction
                {
                    Xid = basic.Xid,
                    Name = basic.Name,
                    Description = description,
                    Categories = categories,
                    ImageUrls = imageUrls,
                    Website = website,
                    Latitude = basic.Lat,
                    Longitude = basic.Lon,
                    AddressStreet = street,
                    AddressNumber = number,
                    AddressCity = cityName,
                    AddressCountry = country,
                    City = cityFromDb
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching/parsing detail for xid {basic.Xid}: {ex.Message}");
                return null;
            }
        });

        var fetchedAttractions = await Task.WhenAll(detailTasks);
        var newAttractionsList = fetchedAttractions
            .Where(a => a != null)
            .Cast<Attraction>()
            .ToList();

        Console.WriteLine($"Fetched detailed info for {newAttractionsList.Count} new attractions");
        // Save new attractions to the database
        if (newAttractionsList.Count > 0)
        {
            _context.attractions.AddRange(newAttractionsList);
            await _context.SaveChangesAsync();
            Console.WriteLine($"Saved {newAttractionsList.Count} new attractions to database");
        }
        else
        {
            Console.WriteLine("No new attractions to save");
        }

        // Return all attractions: both cached and just-fetched
        var allAttractions = await _context.attractions
            .Where(a => xids.Contains(a.Xid))
            .Include(a => a.City)
            .ToListAsync();

        Console.WriteLine($"Returning {allAttractions.Count} total attractions");
        return allAttractions;
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<Task<HttpResponseMessage>> sendRequest, int maxRetries = 3, int baseDelayMs = 1000)
    {
        int attempt = 0;
        while (attempt < maxRetries)
        {
            var response = await sendRequest();

            if (response.StatusCode != System.Net.HttpStatusCode.TooManyRequests)
            {
                return response;
            }

            attempt++;
            int delay = baseDelayMs * (int)Math.Pow(2, attempt);
            Console.WriteLine($"Rate limit hit. Retrying attempt {attempt} after {delay}ms..");
            await Task.Delay(delay);
        }
        return await sendRequest();
    }


}