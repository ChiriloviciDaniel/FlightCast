using FlightCast.Models;
public interface IWeatherService
{

    Task AddWeatherRecord(WeatherRecord record);
    Task<List<WeatherRecord>> GetWeatherRecordsAsync(string city, DateTime startDate, DateTime endDate);
    Task<List<WeatherRecord>> GetHistoricalWeatherAsync(WeatherRequest request);
}