using System.Text.Json.Serialization;

namespace faysys_payslip_generator.Models
{
    public class CompanyInformation
    {
        [JsonPropertyName("companyName")]
        public string CompanyName { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("cityPincode")]
        public string CityPincode { get; set; } = string.Empty;

        [JsonPropertyName("country")]
        public string Country { get; set; } = "India";

        [JsonPropertyName("logoPath")]
        public string LogoPath { get; set; } = string.Empty;

        [JsonPropertyName("lastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}