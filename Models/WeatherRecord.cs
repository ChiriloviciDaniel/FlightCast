
namespace FlightCast.Models
{
    public class WeatherRecord
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public float MaxTemperature { get; set; }
        public float MinTemperature { get; set; }
        public float AverageTemperature { get; set; }
        public string City { get; set; } = string.Empty;

    }
}
