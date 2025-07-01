using System.Text.Json;
using Models;

public class AttractionsService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public AttractionsService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<List<Attraction>> GetAttractionsAsync(City city)
    {
        var apiKey = _config["ApiKeys:Geoapify"];
        var url = $"https://api.geoapify.com/v2/places?categories=tourism.sights&filter=circle:{city.Longitude},{city.Latitude},5000&limit=10&apiKey={apiKey}";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        Console.WriteLine(json);
        return ParseAttractionsFromJson(json, city);
    }

    private List<Attraction> ParseAttractionsFromJson(string json, City city)
    {
        var attractions = new List<Attraction>();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("features", out var features))
        {
            foreach (var feature in features.EnumerateArray())
            {
                var properties = feature.GetProperty("properties");

                // Extract name
                string name = properties.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : "";

                // Extract description
                string description = properties.TryGetProperty("description", out var descriptionProp)
                    ? descriptionProp.GetString() ?? ""
                    : "";

                // Extract categories - join array or fallback to single string
                List<string> categories = new List<string>();

                if (properties.TryGetProperty("categories", out var categoriesProp) && categoriesProp.ValueKind == JsonValueKind.Array)
                {
                    categories = categoriesProp.EnumerateArray().Select(c => c.GetString() ?? "").ToList();
                }
                else if (properties.TryGetProperty("category", out var categoryProp))
                {
                    categories.Add(categoryProp.GetString() ?? "");
                }


                // Extract image URL - try multiple possible keys
                List<string> imageUrls = new List<string>();

                if (properties.TryGetProperty("images", out var imagesProp) && imagesProp.ValueKind == JsonValueKind.Array)
                {
                    imageUrls = imagesProp.EnumerateArray().Select(i => i.GetString() ?? "").ToList();
                }
                else if (properties.TryGetProperty("image", out var imageProp))
                {
                    var img = imageProp.GetString();
                    if (!string.IsNullOrEmpty(img))
                        imageUrls.Add(img);
                }
                else if (properties.TryGetProperty("image_url", out var imageUrlProp))
                {
                    var img = imageUrlProp.GetString();
                    if (!string.IsNullOrEmpty(img))
                        imageUrls.Add(img);
                }

                // Extract rating as double
                double rating = 0.0;
                if (properties.TryGetProperty("rating", out var ratingProp) && ratingProp.ValueKind == JsonValueKind.Number)
                {
                    ratingProp.TryGetDouble(out rating);
                }

                // Extract website URL - try multiple possible keys
                string website = "";
                if (properties.TryGetProperty("website", out var websiteProp))
                {
                    website = websiteProp.GetString() ?? "";
                }
                else if (properties.TryGetProperty("url", out var urlProp))
                {
                    website = urlProp.GetString() ?? "";
                }

                // Create attraction instance
                var attraction = new Attraction
                {
                    Name = name,
                    Description = description,
                    Categories = categories,
                    ImageUrls = imageUrls,
                    Rating = rating,
                    Website = website,
                    City = city
                };
                //ToDo:Add it to DB
                attractions.Add(attraction);
            }
        }

        return attractions;
    }
}