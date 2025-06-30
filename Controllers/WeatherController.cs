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
    public async Task<IActionResult> GetWeather(WeatherRequest request)
    {
        if (!ModelState.IsValid)
            return View(request);

        var records = await _weatherService.GetHistoricalWeatherAsync(request);


        return View("Results", records);
    }
}