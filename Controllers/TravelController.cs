using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using FlightCast.Models;
using Microsoft.AspNetCore.Mvc;
using Models;

//[Authorize(Roles = "Admin")]
public class TravelController : Controller
{
    private readonly IWeatherService _weatherService;
    private readonly AttractionsService _attractionsService;
    private readonly HttpClient _httpClient;
    public TravelController(IWeatherService weatherService, AttractionsService attractionsService, HttpClient httpClient)
    {
        _weatherService = weatherService;
        _attractionsService = attractionsService;
        _httpClient = httpClient;
    }
    public IActionResult Index()
    {
        return View();
    }
    [HttpGet]
    public async Task<IActionResult> GetTTDPartial(string cityName)
    {
        if (string.IsNullOrEmpty(cityName))
            return BadRequest("City is required");

        var attractions = await _attractionsService.GetAttractionsFromDBAsync(cityName);
        return PartialView("TTDPartial", attractions);
    }


    [HttpPost]
    public async Task<IActionResult> GetTravelData(WeatherRequest request)
    {

        //ToDo: Search in Db after city first
        if (!ModelState.IsValid)
            return View(request);

        if (request.city == null)
            return BadRequest("City information is required!");

        var records = await _weatherService.GetHistoricalWeatherAsync(request);

        string? cityName = records.FirstOrDefault()?.City;

        if (string.IsNullOrEmpty(cityName))
        {
            // Handle no records or no city found (return empty attractions or error)
            return View("Results", new TravelResultsInfo
            {
                weatherRequest = request,
                weatherRecords = records,
                attractions = new List<Attraction>()
            });
        }
        var attractions = await _attractionsService.GetAttractionsFromDBAsync(cityName);
        if(attractions.Count<10)
            attractions = await _attractionsService.GetAttractionsAsync(cityName);
        

        var model = new TravelResultsInfo
        {
            weatherRequest = request,
            weatherRecords = records,
            attractions = attractions

        };

        return View("Results", model);
    }
}