namespace ProvidingShelter.Infrastructure.Persistence.Models
{

    public class RisCityCode
    {
        public string ResourceUrl { get; set; } = default!;
        public string CityCode { get; set; } = default!; // PK
        public string CityName { get; set; } = default!;
        public bool? IsCurrent { get; set; }
    }
}
