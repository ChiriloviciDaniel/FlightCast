using Microsoft.AspNetCore.Routing.Constraints;

namespace Models
{
    public class City
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public Double Latitude { get; set; }
        public Double Longitude { get; set; }
    }
}