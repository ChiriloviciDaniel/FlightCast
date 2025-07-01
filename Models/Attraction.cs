namespace Models
{
    public class Attraction
    {
        public int Id { get; set; }
        public int CityId { get; set; }
        public required City City { get; set; }

        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<string>? Categories { get; set; } = new List<string>();

        public List<string>? ImageUrls { get; set; } = new List<string>();

        public double? Rating { get; set; }

        public string? Website { get; set; }

        // GPS Adress
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Adress
        public string? AddressStreet { get; set; }
        public string? AddressNumber { get; set; }
        public string? AddressCity { get; set; }
        public string? AddressCountry { get; set; }

        // Optional
        public string? OpeningHours { get; set; }
        public string? EntranceFee { get; set; }

    }
}