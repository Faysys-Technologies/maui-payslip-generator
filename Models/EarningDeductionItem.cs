using System.Text.Json.Serialization;

namespace faysys_payslip_generator.Models
{
    public class EarningDeductionItem
    {
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsReadOnly { get; set; }
        
        [JsonIgnore]
        public string AmountString 
        { 
            get => Amount.ToString("N2");
            set 
            {
                if (decimal.TryParse(value, out decimal result))
                    Amount = result;
            }
        }
    }
} 