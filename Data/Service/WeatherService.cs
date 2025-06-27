using System.Text.Json;
using System.Text.Json.Serialization;
using FlightCast.Models;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;

    public WeatherService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<WeatherRecord>> GetHistoricalWeatherAsync(string city, DateTime startDate, DateTime endDate)
    {
        try
        {
            var geoResp = await _httpClient.GetFromJsonAsync<GeocodingResponse>(
                $"https://geocoding-api.open-meteo.com/v1/search?name={city}");

            var location = geoResp?.Results?.FirstOrDefault();
            if (location == null)
                return new List<WeatherRecord>();

            double lat = location.Latitude;
            double lon = location.Longitude;

            string url = $"https://archive-api.open-meteo.com/v1/archive" +
                     $"?latitude={lat}" +
                     $"&longitude={lon}" +
                     $"&start_date={startDate:yyyy-MM-dd}" +
                     $"&end_date={endDate:yyyy-MM-dd}" +
                     $"&daily=temperature_2m_max,temperature_2m_min" +
                     $"&timezone=auto";

            var WeatherResponse = await _httpClient.GetFromJsonAsync<WeatherApiResponse>(url);

            //Check if the TemperatureMax and TempMin properties are not null
            if (WeatherResponse?.Daily?.TemperatureMax == null || WeatherResponse?.Daily?.TemperatureMin == null)
                return new List<WeatherRecord>();
            // Use ! to assert that the properties are not null
            //Check if the response has null temperatures and filter them out
            var maxTemps = WeatherResponse.Daily.TemperatureMax
                .Where(t => t.HasValue)
                .Select(t => t!.Value)
                .ToList();
            var minTemps = WeatherResponse.Daily.TemperatureMin
                .Where(t => t.HasValue)
                .Select(t => t!.Value)
                .ToList();

            if (minTemps == null || maxTemps == null || !maxTemps.Any() || !minTemps.Any())
                return new List<WeatherRecord>();

            var avgMax = maxTemps.Average();
            var avgMin = minTemps.Average();
            var avg = (avgMax + avgMin) / 2;

            return new List<WeatherRecord>
            {

                new WeatherRecord{
                    StartDate = startDate,
                    EndDate = endDate,
                    MaxTemperature = (float)avgMax,
                    MinTemperature = (float)avgMin,
                    AverageTemperature = (float)avg
            }

            };
        }
        catch (HttpRequestException ex)
        {
            // Error during HTTP request
            Console.WriteLine($"Error during HTTP: {ex.Message}");
            return new List<WeatherRecord>();
        }
        catch (NotSupportedException ex)
        {
            // Content type exception
            Console.WriteLine($"Format error: {ex.Message}");
            return new List<WeatherRecord>();
        }
        catch (JsonException ex)
        {
            // Error  parsing JSON
            Console.WriteLine($"Error parsing JSON: {ex.Message}");
            return new List<WeatherRecord>();
        }
        catch (Exception ex)
        {
            //log the exception
            Console.WriteLine($"Error fetching weather data: {ex.Message}");
            return new List<WeatherRecord>();
        }
    }
}

public class GeocodingResponse
{
    [JsonPropertyName("results")]
    public List<GeoResults> Results { get; set; } = new List<GeoResults>();
}

public class GeoResults
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}


public class WeatherApiResponse
{
    [JsonPropertyName("daily")]
    public DailyWeather? Daily { get; set; }
}

public class DailyWeather
{
    [JsonPropertyName("time")]
    public string[] Time { get; set; } = Array.Empty<string>();
    [JsonPropertyName("temperature_2m_max")]
    public double?[] TemperatureMax { get; set; } = Array.Empty<double?>();
    [JsonPropertyName("temperature_2m_min")]
    public double?[] TemperatureMin { get; set; } = Array.Empty<double?>();

}