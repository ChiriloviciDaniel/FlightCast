using FlightCast.Models;
using Models;
public interface IWeatherService
{

    Task AddWeatherRecord(WeatherRecord record);
    Task<City?> SaveCityCoordinate(City city);
    Task<List<WeatherRecord>> GetHistoricalWeatherAsync(WeatherRequest request);
}