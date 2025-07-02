namespace Models
{
    public class Attraction
    {
        public int Id { get; set; }
        public string Xid { get; set; } = string.Empty;
        public int CityId { get; set; }
        public required City City { get; set; }

        public string? Name { get; set; }
        public string? Description { get; set; }

        // From "kinds" — split into list
        public List<string>? Categories { get; set; } = new List<string>();

        // Usually only one image is available
        public List<string>? ImageUrls { get; set; } = new List<string>();

        // OpenTripMap uses 1-3 for "rate" → consider renaming or documenting
        public int? Popularity { get; set; }

        public string? Website { get; set; }

        // GPS
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Address fields
        public string? AddressStreet { get; set; }
        public string? AddressNumber { get; set; }
        public string? AddressCity { get; set; }
        public string? AddressCountry { get; set; }

        // Rarely available
        public string? OpeningHours { get; set; }
        public string? EntranceFee { get; set; }

        // Optional extra info
        public string? WikipediaUrl { get; set; }

    }
}