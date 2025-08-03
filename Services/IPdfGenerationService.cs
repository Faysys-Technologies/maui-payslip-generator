using faysys_payslip_generator.Models;

namespace faysys_payslip_generator.Services
{
    public interface IPdfGenerationService
    {
        Task<bool> GeneratePayslipPdfAsync(PayslipData payslipData, string fileName);
        Task GeneratePayslipAsync(PayslipData payslipData);
        string ConvertAmountToWords(decimal amount);
    }
}