using System.Text.Json.Serialization;

namespace faysys_payslip_generator.Models
{
    public class IncomeDetails
    {
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string PayPeriod { get; set; } = string.Empty;
        public DateTime PayDate { get; set; } = DateTime.Now;
        public int PaidDays { get; set; } = 0;
        public int LossOfPayDays { get; set; } = 0;
        
        // Predefined earnings
        public decimal BasicEarning { get; set; } = 0;
        public decimal HraEarning { get; set; } = 0;
        
        // Predefined deductions
        public decimal IncomeTaxDeduction { get; set; } = 0;
        public decimal PfDeduction { get; set; } = 0;
        
        // Dynamic earnings and deductions
        public List<EarningDeductionItem> DynamicEarnings { get; set; } = new List<EarningDeductionItem>();
        public List<EarningDeductionItem> DynamicDeductions { get; set; } = new List<EarningDeductionItem>();
        
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        
        // Calculated totals
        public decimal GrossEarnings { get; set; } = 0;
        public decimal TotalDeductions { get; set; } = 0;
        public decimal NetPayable { get; set; } = 0;
    }
    
    public class IncomeDetailsCollection
    {
        public List<IncomeDetails> IncomeDetailsList { get; set; } = new List<IncomeDetails>();
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
} 