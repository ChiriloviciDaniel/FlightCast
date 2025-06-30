
namespace FlightCast.Models
{
    public class WeatherRecord
    {

        public int Id { get; set; }
        public DateTime Date { get; set; }
        //public DateTime EndDate { get; set; }
        public float MaxTemperature { get; set; }
        public float MinTemperature { get; set; }
        public float AverageTemperature { get; set; }
        public string City { get; set; } = string.Empty;

    }
}
