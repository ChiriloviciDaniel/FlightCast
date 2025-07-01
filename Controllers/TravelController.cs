using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using FlightCast.Models;
using Microsoft.AspNetCore.Mvc;

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



    [HttpPost]
    public async Task<IActionResult> GetTravelData(WeatherRequest request)
    {

        //ToDo: Search in Db after city first
        if (!ModelState.IsValid)
            return View(request);

        if (request.city == null)
            return BadRequest("City information is required!");

        if (request.city.Latitude == 0 || request.city.Longitude == 0)
        {
            var geoResp = await _httpClient.GetFromJsonAsync<GeocodingResponse>(
             $"https://geocoding-api.open-meteo.com/v1/search?name={request.city.Name}");

            var location = geoResp?.Results?.FirstOrDefault();
            if (location == null)
                return Content("City not found!");

            request.city.Latitude = location.Latitude;
            request.city.Longitude = location.Longitude;

            await _weatherService.SaveCityCoordinateAsync(request.city);

        }
        var records = await _weatherService.GetHistoricalWeatherAsync(request);
        var attractions = await _attractionsService.GetAttractionsAsync(request.city);

        var model = new TravelResultsInfo
        {
            weatherRequest = request,
            weatherRecords = records,
            attractions = attractions

        };

        return View("Results", model);
    }
}