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

    public async Task<City?> SaveCityCoordinate(City city)
    {
        if (string.IsNullOrWhiteSpace(city.Name))
        {
            // Handle null or empty city.Name as needed (e.g., throw exception or return)
            return null;
        }

        var cityNameNormalized = city.Name.Trim().ToLower();

        var record = await _context.cities.FirstOrDefaultAsync(c =>
            c.Name != null && c.Name.ToLower() == cityNameNormalized);
        if (record == null)
        {
            _context.cities.Add(city);
            await _context.SaveChangesAsync();
            return city;
        }
        return record;
    }


    public async Task AddWeatherRecord(WeatherRecord record)
    {
        if (record == null)
            throw new ArgumentException("Record cannot be null", nameof(record));
        _context.WeatherRecords.Add(record);
        await _context.SaveChangesAsync();
    }
    public async Task<List<WeatherRecord>> GetHistoricalWeatherAsync(WeatherRequest request)
    {
        try
        {
            if (request?.city?.Name == null)
                return new List<WeatherRecord>();

            double lat, lon;

            // Try to find city with valid coordinates in DB
            var existingCity = await _context.cities.FirstOrDefaultAsync(c =>
                c.Name == request.city.Name &&
                c.Latitude != 0 &&
                c.Longitude != 0);

            if (existingCity != null)
            {
                lat = existingCity.Latitude;
                lon = existingCity.Longitude;
            }
            else
            {
                // Call geocoding API to get city coordinates
                var geoResp = await _httpClient.GetFromJsonAsync<GeocodingResponse>(
                    $"https://geocoding-api.open-meteo.com/v1/search?name={request.city.Name}");

                var location = geoResp?.Results?.FirstOrDefault();
                if (location == null)
                    return new List<WeatherRecord>();

                lat = location.Latitude;
                lon = location.Longitude;

                // Save city coordinates to DB
                var cityToSave = new City
                {
                    Name = request.city.Name,
                    Latitude = lat,
                    Longitude = lon
                };
                await SaveCityCoordinate(cityToSave);
            }

            int yearsToCheck = 3;
            var recordsList = new List<WeatherRecord>();

            // Get all existing records for the entire 3-year range upfront (to avoid multiple DB calls)
            var earliestStartDate = request.StartDate.AddYears(-yearsToCheck);
            var latestEndDate = request.EndDate.AddYears(-1);

            var existingRecords = await _context.WeatherRecords
                .Where(w => w.City == request.city.Name && w.Date >= earliestStartDate && w.Date <= latestEndDate)
                .ToListAsync();

            // Add all existing records to the final result list
            recordsList.AddRange(existingRecords);

            // Loop over each year and find missing dates
            for (int i = 1; i <= yearsToCheck; i++)
            {
                var startDate = request.StartDate.AddYears(-i);
                var endDate = request.EndDate.AddYears(-i);

                // Get dates already present in DB for this year range
                var datesInDb = existingRecords
                    .Where(r => r.Date >= startDate && r.Date <= endDate)
                    .Select(r => r.Date)
                    .ToHashSet();

                // Identify missing dates for this year
                var missingDates = new List<DateTime>();
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    if (!datesInDb.Contains(date))
                        missingDates.Add(date);
                }

                if (missingDates.Count == 0)
                    continue; // No missing data for this year, skip API call

                // Fetch weather data for each missing date individually
                foreach (var date in missingDates)
                {
                    string url = $"https://archive-api.open-meteo.com/v1/archive" +
                                 $"?latitude={lat}" +
                                 $"&longitude={lon}" +
                                 $"&start_date={date:yyyy-MM-dd}" +
                                 $"&end_date={date:yyyy-MM-dd}" +
                                 $"&daily=temperature_2m_max,temperature_2m_min" +
                                 $"&timezone=auto";

                    var WeatherResponse = await _httpClient.GetFromJsonAsync<WeatherApiResponse>(url);

                    // Skip this date if no data returned
                    if (WeatherResponse?.Daily?.TemperatureMax == null || WeatherResponse?.Daily?.TemperatureMin == null)
                        continue;

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
                        //Console.WriteLine($"@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@mising date ultima oara 2#@3213213123{record.Date} ");
                        _context.WeatherRecords.Add(record);
                        recordsList.Add(record);
                    }
                }
            }

            // Save all newly added records at once
            await _context.SaveChangesAsync();

            return recordsList;
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