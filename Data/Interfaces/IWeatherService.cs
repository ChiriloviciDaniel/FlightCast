using FlightCast.Models;
using Models;
public interface IWeatherService
{

    Task AddWeatherRecord(WeatherRecord record);
    Task SaveCityCoordinateAsync(City city);
    Task<List<WeatherRecord>> GetWeatherRecordsAsync(string city, DateTime startDate, DateTime endDate);
    Task<List<WeatherRecord>> GetHistoricalWeatherAsync(WeatherRequest request);
}