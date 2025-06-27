using FlightCast.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

//[Authorize(Roles = "Admin")]
public class WeatherController : Controller
{
    private readonly IWeatherService _weatherService;
    public WeatherController(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }
    public IActionResult Index()
    {
        return View();
    }


   
    [HttpPost]
    public async Task<IActionResult> GetWeather(WeatherRecord weatherRecord)
    {
        if (!ModelState.IsValid)
            return View(weatherRecord);

        var records = await _weatherService.GetHistoricalWeatherAsync(
            weatherRecord.City,
            weatherRecord.StartDate,
            weatherRecord.EndDate
        );

        return View("Results", records);
    }
}