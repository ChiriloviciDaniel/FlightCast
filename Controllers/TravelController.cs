using FlightCast.Models;
using Microsoft.AspNetCore.Mvc;

//[Authorize(Roles = "Admin")]
public class TravelController : Controller
{
    private readonly IWeatherService _weatherService;
    public TravelController(IWeatherService weatherService)
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

        var model = new TravelResultsInfo
        {
            weatherRequest = request,
            weatherRecords = records

        };

        return View("Results", model);
    }
}