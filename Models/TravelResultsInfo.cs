using FlightCast.Models;

namespace FlightCast.Models
{
    public class TravelResultsInfo
    {
        public WeatherRequest? weatherRequest{ get; set; }
        public List<WeatherRecord>? weatherRecords{ get; set; }
    }
}