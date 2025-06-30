
namespace FlightCast.Models
{
    public class WeatherRequest
    {
        public string City { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
