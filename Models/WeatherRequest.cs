
using Models;

namespace FlightCast.Models
{
    public class WeatherRequest
    {
        public City? city { get; set; } 
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
