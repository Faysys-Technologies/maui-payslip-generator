using System.Collections.Generic;

namespace faysys_payslip_generator.Models
{
    public class PayslipData
    {
        // Company Information
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyAddress { get; set; } = string.Empty;
        public string CompanyCityPincode { get; set; } = string.Empty;
        public string CompanyCountry { get; set; } = "India";
        public string CompanyLogoPath { get; set; } = string.Empty;

        // Employee Information
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string BankAccount { get; set; } = string.Empty;
        public string PanNumber { get; set; } = string.Empty;

        // Pay Period Information
        public string PayPeriod { get; set; } = string.Empty;
        public DateTime PayDate { get; set; } = DateTime.Today;
        public int PaidDays { get; set; }
        public int LossOfPayDays { get; set; }

        // Financial Information
        public Dictionary<string, decimal> Earnings { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> Deductions { get; set; } = new Dictionary<string, decimal>();
        public decimal GrossEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetPayable { get; set; }

        // Legacy support
        public List<DynamicEmployeeField> DynamicFields { get; set; } = new List<DynamicEmployeeField>();
        public string LogoPath { get; set; } = string.Empty;
    }



    public class DynamicEmployeeField
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}