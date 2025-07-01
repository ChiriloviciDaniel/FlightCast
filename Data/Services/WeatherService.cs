using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Eventing.Reader;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlightCast.Data;
using FlightCast.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Models;

public class WeatherService : IWeatherService
{
    private readonly AppDbContext _context;
    private readonly HttpClient _httpClient;

    public WeatherService(HttpClient httpClient, AppDbContext context)
    {
        _context = context;
        _httpClient = httpClient;
    }

    public async Task SaveCityCoordinateAsync(City city)
    {
        var record = await _context.cities.FirstOrDefaultAsync(c => c.Name == city.Name);
        if (record == null)
        {
            _context.cities.Add(city);
            await _context.SaveChangesAsync();
        }
    }


    public async Task AddWeatherRecord(WeatherRecord record)
    {
        if (record == null)
            throw new ArgumentException("Record cannot be null", nameof(record));
        _context.WeatherRecords.Add(record);
        await _context.SaveChangesAsync();


    }

    public async Task<List<WeatherRecord>> GetWeatherRecordsAsync(string city, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrEmpty(city))
            throw new ArgumentException("City cannot be null or empty", nameof(city));

        if (startDate > endDate)
            throw new ArgumentException("Start date cannot be after endDate");

        var records = await _context.WeatherRecords
            .Where(w => w.City == city && w.Date >= startDate && w.Date <= endDate)
            .ToListAsync();

        return records;
    }

    public async Task<List<WeatherRecord>> GetHistoricalWeatherAsync(WeatherRequest request)
    {
        try
        {
            if (request.city == null)
                return new List<WeatherRecord>();
            if (request.city.Name == null)
                return new List<WeatherRecord>();

            double lat;
            double lon;
            var recordsDB = await GetWeatherRecordsAsync(request.city.Name, request.StartDate, request.EndDate);
            //Remove from request list the records from DB
            if (!recordsDB.Any())
            {
                var recordsList = new List<WeatherRecord>();
                if (request.city.Latitude == 0 || request.city.Longitude == 0)
                {
                    //Create a predefined list of cities with their coordinates to avoid multiple API calls
                    var geoResp = await _httpClient.GetFromJsonAsync<GeocodingResponse>(
                        $"https://geocoding-api.open-meteo.com/v1/search?name={request.city.Name}");

                    var location = geoResp?.Results?.FirstOrDefault();
                    if (location == null)
                        return new List<WeatherRecord>();

                    lat = location.Latitude;
                    lon = location.Longitude;
                    var cityToSave = new City
                    {
                        Name = request.city.Name,
                        Latitude = lat,
                        Longitude = lon
                    };
                    await SaveCityCoordinateAsync(cityToSave);
                }
                else
                {
                    lat = request.city.Latitude;
                    lon = request.city.Longitude;
                }

                for (int i = 1; i < 4; i++)
                {

                    string url = $"https://archive-api.open-meteo.com/v1/archive" +
                             $"?latitude={lat}" +
                             $"&longitude={lon}" +
                             $"&start_date={request.StartDate.AddYears(-i):yyyy-MM-dd}" +
                             $"&end_date={request.EndDate.AddYears(-i):yyyy-MM-dd}" +
                             $"&daily=temperature_2m_max,temperature_2m_min" +
                             $"&timezone=auto";

                    var WeatherResponse = await _httpClient.GetFromJsonAsync<WeatherApiResponse>(url);
                    if (WeatherResponse?.Daily?.TemperatureMax == null || WeatherResponse?.Daily?.TemperatureMin == null)
                        return new List<WeatherRecord>();

                    for (int j = 0; j < WeatherResponse.Daily.Time.Length; j++)
                    {

                        var max = WeatherResponse.Daily.TemperatureMax[j];
                        var min = WeatherResponse.Daily.TemperatureMin[j];

                        if (max == null || min == null)
                            continue;

                        var record = new WeatherRecord
                        {
                            Date = DateTime.Parse(WeatherResponse.Daily.Time[j]),
                            MaxTemperature = (float)max,
                            MinTemperature = (float)min,
                            AverageTemperature = ((float)max + (float)min) / 2,
                            City = request.city.Name
                        };
                        await AddWeatherRecord(record);
                        recordsList.Add(record);


                    }

                }
                return recordsList;
            }
            else
            {
                return recordsDB;
            }

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