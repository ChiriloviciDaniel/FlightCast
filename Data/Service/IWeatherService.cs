using FlightCast.Models;
public interface IWeatherService
{
    Task<List<WeatherRecord>> GetHistoricalWeatherAsync(string city, DateTime startDate, DateTime endDate);
}