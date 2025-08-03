using System.Text.Json.Serialization;

namespace faysys_payslip_generator.Models
{
    public class EmployeeInformation
    {
        [JsonPropertyName("employeeCode")]
        public string EmployeeCode { get; set; } = string.Empty;

        [JsonPropertyName("employeeName")]
        public string EmployeeName { get; set; } = string.Empty;

        [JsonPropertyName("designation")]
        public string Designation { get; set; } = string.Empty;

        [JsonPropertyName("department")]
        public string Department { get; set; } = string.Empty;

        [JsonPropertyName("joiningDate")]
        public DateTime? JoiningDate { get; set; }

        [JsonPropertyName("basicSalary")]
        public decimal BasicSalary { get; set; }

        [JsonPropertyName("conveyanceAllowance")]
        public decimal ConveyanceAllowance { get; set; }

        [JsonPropertyName("houseRentAllowance")]
        public decimal HouseRentAllowance { get; set; }

        [JsonPropertyName("professionalTax")]
        public decimal ProfessionalTax { get; set; }

        [JsonPropertyName("bankAccount")]
        public string BankAccount { get; set; } = string.Empty;

        [JsonPropertyName("panNumber")]
        public string PanNumber { get; set; } = string.Empty;

        [JsonPropertyName("customFields")]
        public Dictionary<string, string> CustomFields { get; set; } = new();

        [JsonPropertyName("lastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    public class EmployeeInformationCollection
    {
        [JsonPropertyName("employees")]
        public List<EmployeeInformation> Employees { get; set; } = new();

        [JsonPropertyName("lastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}