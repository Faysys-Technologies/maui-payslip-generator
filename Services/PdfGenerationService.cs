using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using faysys_payslip_generator.Models;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using CommunityToolkit.Maui.Storage;

namespace faysys_payslip_generator.Services
{
    public class PdfGenerationService : IPdfGenerationService
    {
        public async Task GeneratePayslipAsync(PayslipData payslipData)
        {
            string fileName = $"Payslip_{payslipData.EmployeeName}_{payslipData.PayPeriod.Replace(" ", "_")}.pdf";
            var result = await GeneratePayslipPdfAsync(payslipData, fileName);
            if (!result)
            {
                throw new InvalidOperationException("Failed to generate payslip PDF");
            }
        }

        public async Task<bool> GeneratePayslipPdfAsync(PayslipData payslipData, string fileName)
        {
            try
            {
                using var document = new PdfDocument();
                document.Info.Title = "Payslip";
                document.Info.Author = "Faysys Technologies";
                document.Info.Creator = "Payslip Generator";
                
                var page = document.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                
                var gfx = XGraphics.FromPdfPage(page);
                
                // Set high quality rendering for crisp text
                gfx.SmoothingMode = XSmoothingMode.HighQuality;

                await DrawPayslipContent(gfx, page, payslipData);

                using var stream = new MemoryStream();
                document.Save(stream, false);
                stream.Position = 0;

                #if WINDOWS || MACCATALYST
                var fileSaverResult = await FileSaver.Default.SaveAsync(fileName, stream, new CancellationToken());
                return fileSaverResult.IsSuccessful;
                #else
                string defaultPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    fileName);

                using var fileStream = File.OpenWrite(defaultPath);
                await stream.CopyToAsync(fileStream);
                return true;
                #endif
            }
            catch
            {
                return false;
            }
        }

        private async Task DrawPayslipContent(XGraphics gfx, PdfPage page, PayslipData data)
        {
            // Define fonts with proper fallback - PdfSharpCore works better with single font names
            string fontFamily = GetBestAvailableFont();
            
            // Use slightly larger sizes for better readability and professional appearance
            var fontLogo = new XFont(fontFamily, 20, XFontStyle.Bold);
            var fontTitle = new XFont(fontFamily, 16, XFontStyle.Bold);
            var fontHeader = new XFont(fontFamily, 12, XFontStyle.Bold);
            var fontBody = new XFont(fontFamily, 11, XFontStyle.Regular);
            var fontSmall = new XFont(fontFamily, 9, XFontStyle.Regular);
            var fontLarge = new XFont(fontFamily, 20, XFontStyle.Bold);

            var lightGrayBrush = new XSolidBrush(XColor.FromArgb(249, 250, 251));
            var greenBrush = new XSolidBrush(XColor.FromArgb(240, 253, 244));
            var darkGreenBrush = new XSolidBrush(XColor.FromArgb(34, 197, 94));
            var grayTextBrush = new XSolidBrush(XColor.FromArgb(107, 114, 128));
            var subtleLine = new XPen(XColor.FromArgb(229, 231, 235), 0.5);

            double y = 40;
            double leftMargin = 40;
            double rightMargin = page.Width - 40;

            // Header Section
            await DrawHeader(gfx, data, fontLogo, fontTitle, fontBody, grayTextBrush, leftMargin, rightMargin, y);
            y += 100;

            // Separator line
            gfx.DrawLine(subtleLine, leftMargin, y, rightMargin, y);
            y += 30;

            // Employee Summary and Net Pay Box
            y = await DrawEmployeeSummary(gfx, data, fontHeader, fontBody, fontSmall, fontLarge, 
                lightGrayBrush, greenBrush, darkGreenBrush, grayTextBrush, leftMargin, rightMargin, y);

            // Earnings and Deductions Table
            y = await DrawEarningsDeductionsTable(gfx, data, fontHeader, fontBody, 
                lightGrayBrush, subtleLine, leftMargin, rightMargin, y);

            // Final Net Payable
            y = await DrawFinalNetPayable(gfx, data, fontHeader, fontLarge, fontSmall,
                greenBrush, leftMargin, rightMargin, y);

            // Footer
            await DrawFooter(gfx, fontBody, fontSmall, grayTextBrush, subtleLine, 
                leftMargin, rightMargin, page.Height - 120);
        }

        private async Task DrawHeader(XGraphics gfx, PayslipData data, XFont fontLogo, XFont fontTitle, 
            XFont fontBody, XBrush grayTextBrush, double leftMargin, double rightMargin, double y)
        {
            // Company logo
            string logoPath = !string.IsNullOrEmpty(data.CompanyLogoPath) ? data.CompanyLogoPath : data.LogoPath;
            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
            {
                try
                {
                    var logoImage = XImage.FromFile(logoPath);
                    gfx.DrawImage(logoImage, leftMargin, y, 45, 45);
                }
                catch { /* Logo loading failed */ }
            }

            // Company details
            gfx.DrawString(data.CompanyName, fontLogo, XBrushes.Black, leftMargin + 55, y + 15);
            gfx.DrawString(data.CompanyCountry, fontBody, grayTextBrush, leftMargin + 55, y + 35);

            // Payslip title
            gfx.DrawString("Payslip For the Month", fontBody, grayTextBrush, rightMargin - 140, y + 5);
            gfx.DrawString(data.PayPeriod, fontTitle, XBrushes.Black, rightMargin - 140, y + 25);
        }

        private async Task<double> DrawEmployeeSummary(XGraphics gfx, PayslipData data, XFont fontHeader, 
            XFont fontBody, XFont fontSmall, XFont fontLarge, XBrush lightGrayBrush, XBrush greenBrush, 
            XBrush darkGreenBrush, XBrush grayTextBrush, double leftMargin, double rightMargin, double y)
        {
            // Calculate totals
            decimal grossEarnings = data.Earnings.Sum(x => x.Value);
            decimal totalDeductions = data.Deductions.Sum(x => x.Value);
            decimal netPayable = grossEarnings - totalDeductions;

            gfx.DrawString("EMPLOYEE SUMMARY", fontHeader, grayTextBrush, leftMargin, y);
            y += 25;

            // Net Pay Box
            double boxWidth = 220;
            double boxHeight = 120;
            double boxX = rightMargin - boxWidth;
            double boxY = y;

            gfx.DrawRectangle(greenBrush, boxX, boxY, boxWidth, boxHeight);
            gfx.DrawRectangle(darkGreenBrush, boxX + 10, boxY + 15, 4, 35);

            gfx.DrawString($"Rs.{netPayable:N2}", fontLarge, XBrushes.Black, boxX + 25, boxY + 35);
            gfx.DrawString("Total Net Pay", fontBody, grayTextBrush, boxX + 25, boxY + 55);

            gfx.DrawString("Paid Days", fontSmall, grayTextBrush, boxX + 25, boxY + 80);
            gfx.DrawString($": {data.PaidDays}", fontSmall, XBrushes.Black, boxX + 80, boxY + 80);
            gfx.DrawString("LOP Days", fontSmall, grayTextBrush, boxX + 25, boxY + 95);
            gfx.DrawString($": {data.LossOfPayDays}", fontSmall, XBrushes.Black, boxX + 80, boxY + 95);

            // Employee details
            double labelWidth = 110;
            gfx.DrawString("Employee Name", fontBody, grayTextBrush, leftMargin, y);
            gfx.DrawString($": {data.EmployeeName}", fontBody, XBrushes.Black, leftMargin + labelWidth, y);

            gfx.DrawString("Employee Code", fontBody, grayTextBrush, leftMargin, y + 25);
            gfx.DrawString($": {data.EmployeeCode}", fontBody, XBrushes.Black, leftMargin + labelWidth, y + 25);

            gfx.DrawString("Pay Period", fontBody, grayTextBrush, leftMargin, y + 50);
            gfx.DrawString($": {data.PayPeriod}", fontBody, XBrushes.Black, leftMargin + labelWidth, y + 50);

            gfx.DrawString("Pay Date", fontBody, grayTextBrush, leftMargin, y + 75);
            gfx.DrawString($": {data.PayDate:dd/MM/yyyy}", fontBody, XBrushes.Black, leftMargin + labelWidth, y + 75);

            // Dynamic fields
            double dynamicY = y + 100;
            foreach (var field in data.DynamicFields.Where(f => !string.IsNullOrWhiteSpace(f.Name)))
            {
                gfx.DrawString(field.Name, fontBody, grayTextBrush, leftMargin, dynamicY);
                gfx.DrawString($": {field.Value}", fontBody, XBrushes.Black, leftMargin + labelWidth, dynamicY);
                dynamicY += 25;
            }

            return Math.Max(y + 140, dynamicY + 40);
        }

        private async Task<double> DrawEarningsDeductionsTable(XGraphics gfx, PayslipData data, 
            XFont fontHeader, XFont fontBody, XBrush lightGrayBrush, XPen subtleLine, 
            double leftMargin, double rightMargin, double y)
        {
            double tableWidth = rightMargin - leftMargin;
            double tableY = y;

            // Table header
            gfx.DrawRectangle(lightGrayBrush, leftMargin, tableY, tableWidth, 35);

            double col1X = leftMargin + 15;
            double col2X = leftMargin + (tableWidth * 0.35);
            double col3X = leftMargin + (tableWidth * 0.55);
            double col4X = leftMargin + (tableWidth * 0.80);

            gfx.DrawString("EARNINGS", fontHeader, XBrushes.Black, col1X, tableY + 22);
            gfx.DrawString("AMOUNT", fontHeader, XBrushes.Black, col2X, tableY + 22);
            gfx.DrawString("DEDUCTIONS", fontHeader, XBrushes.Black, col3X, tableY + 22);
            gfx.DrawString("AMOUNT", fontHeader, XBrushes.Black, col4X, tableY + 22);

            y = tableY + 45;

            // Table rows
            var earningsList = data.Earnings.ToList();
            var deductionsList = data.Deductions.ToList();
            int maxRows = Math.Max(earningsList.Count, deductionsList.Count);
            
            for (int i = 0; i < maxRows; i++)
            {
                if (i % 2 == 1)
                {
                    gfx.DrawRectangle(lightGrayBrush, leftMargin, y - 5, tableWidth, 25);
                }

                if (i < earningsList.Count)
                {
                    var earning = earningsList[i];
                    gfx.DrawString(earning.Key, fontBody, XBrushes.Black, col1X, y + 8);
                    gfx.DrawString($"Rs.{earning.Value:N2}", fontBody, XBrushes.Black, col2X, y + 8);
                }

                if (i < deductionsList.Count)
                {
                    var deduction = deductionsList[i];
                    gfx.DrawString(deduction.Key, fontBody, XBrushes.Black, col3X, y + 8);
                    gfx.DrawString($"Rs.{deduction.Value:N2}", fontBody, XBrushes.Black, col4X, y + 8);
                }
                y += 25;
            }

            // Totals
            y += 15;
            gfx.DrawLine(subtleLine, leftMargin, y - 5, rightMargin, y - 5);
            gfx.DrawRectangle(lightGrayBrush, leftMargin, y, tableWidth, 30);

            decimal grossEarnings = data.Earnings.Sum(x => x.Value);
            decimal totalDeductions = data.Deductions.Sum(x => x.Value);

            gfx.DrawString("Gross Earnings", fontHeader, XBrushes.Black, col1X, y + 18);
            gfx.DrawString($"Rs.{grossEarnings:N2}", fontHeader, XBrushes.Black, col2X, y + 18);
            gfx.DrawString("Total Deductions", fontHeader, XBrushes.Black, col3X, y + 18);
            gfx.DrawString($"Rs.{totalDeductions:N2}", fontHeader, XBrushes.Black, col4X, y + 18);

            return y + 50;
        }

        private async Task<double> DrawFinalNetPayable(XGraphics gfx, PayslipData data, 
            XFont fontHeader, XFont fontLarge, XFont fontSmall, XBrush greenBrush, 
            double leftMargin, double rightMargin, double y)
        {
            decimal netPayable = data.Earnings.Sum(x => x.Value) - data.Deductions.Sum(x => x.Value);
            double tableWidth = rightMargin - leftMargin;

            gfx.DrawRectangle(greenBrush, leftMargin, y, tableWidth, 50);

            gfx.DrawString("TOTAL NET PAYABLE", fontHeader, XBrushes.Black, leftMargin + 15, y + 20);
            gfx.DrawString($"Rs.{netPayable:N2}", fontLarge, XBrushes.Black, rightMargin - 150, y + 30);
            gfx.DrawString("Gross Earnings - Total Deductions", fontSmall, XBrushes.Gray, leftMargin + 15, y + 40);

            y += 70;
            gfx.DrawString($"Amount In Words : {ConvertAmountToWords(netPayable)}", 
                fontSmall, XBrushes.Black, leftMargin, y);

            return y + 40;
        }

        private async Task DrawFooter(XGraphics gfx, XFont fontBody, XFont fontSmall, 
            XBrush grayTextBrush, XPen subtleLine, double leftMargin, double rightMargin, double y)
        {
            gfx.DrawLine(subtleLine, leftMargin, y, rightMargin, y);
            y += 20;

            // System generated document
            string systemDocText = "— This is a system-generated document. —";
            var systemDocSize = gfx.MeasureString(systemDocText, fontSmall);
            double docCenterX = (leftMargin + rightMargin) / 2;
            gfx.DrawString(systemDocText, fontSmall, grayTextBrush, docCenterX - (systemDocSize.Width / 2), y);
            y += 40;

            // Powered by with logo
            await DrawPoweredByWithLogo(gfx, fontBody, leftMargin, rightMargin, y);
        }

        private async Task DrawPoweredByWithLogo(XGraphics gfx, XFont fontBody, double leftMargin, double rightMargin, double y)
        {
            string poweredByText = "Powered by";
            string companyText = "Faysys Technologies";
            
            // Measure text sizes
            var poweredBySize = gfx.MeasureString(poweredByText, fontBody);
            var companySize = gfx.MeasureString(companyText, fontBody);
            
            // Logo dimensions
            double logoWidth = 20;
            double logoHeight = 20;
            double spacing = 8;
            
            // Calculate total width: "Powered by" + spacing + logo + spacing + "Faysys Technologies"
            double totalWidth = poweredBySize.Width + spacing + logoWidth + spacing + companySize.Width;
            
            // Center the entire footer
            double startX = (leftMargin + rightMargin - totalWidth) / 2;
            double currentX = startX;
            
            // Draw "Powered by"
            gfx.DrawString(poweredByText, fontBody, XBrushes.Black, currentX, y);
            currentX += poweredBySize.Width + spacing;
            
            // Draw logo
            await DrawFaysysLogo(gfx, currentX, y - 15, logoWidth, logoHeight);
            currentX += logoWidth + spacing;
            
            // Draw "Faysys Technologies"
            gfx.DrawString(companyText, fontBody, XBrushes.Black, currentX, y);
        }

        private async Task DrawFaysysLogo(XGraphics gfx, double x, double y, double width, double height)
        {
            // Try multiple approaches to load the logo
            string[] logoAttempts = {
                "faysys_logo.png",           // Direct access
                "Images/faysys_logo.png",    // Images folder
                "Resources/Images/faysys_logo.png", // Full path
                "Raw/faysys_logo.png"        // Raw resources folder
            };

            foreach (string logoPath in logoAttempts)
            {
                if (await TryLoadLogoFromPath(gfx, logoPath, x, y, width, height))
                {
                    // Logo loaded successfully
                    return;
                }
            }

            // If all attempts fail, draw fallback logo
            // For now, let's make the fallback more obvious so we know it's being used
            DrawFallbackLogo(gfx, x, y, width, height);
        }

        private async Task<bool> TryLoadLogoFromPath(XGraphics gfx, string logoPath, double x, double y, double width, double height)
        {
            try
            {
                // Method 1: Try to load from app package
                using var logoStream = await FileSystem.Current.OpenAppPackageFileAsync(logoPath);
                if (logoStream != null && logoStream.Length > 0)
                {
                    return await CreateImageFromStream(gfx, logoStream, x, y, width, height);
                }
            }
            catch { }

            try
            {
                // Method 2: Try to load as embedded resource
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string resourceName = $"faysys_payslip_generator.Resources.Images.faysys_logo.png";
                
                using var logoStream = assembly.GetManifestResourceStream(resourceName);
                if (logoStream != null && logoStream.Length > 0)
                {
                    return await CreateImageFromStream(gfx, logoStream, x, y, width, height);
                }
            }
            catch { }

            return false;
        }

        private async Task<bool> CreateImageFromStream(XGraphics gfx, Stream logoStream, double x, double y, double width, double height)
        {
            try
            {
                // Create a temporary file to work with PdfSharpCore
                string tempPath = Path.Combine(Path.GetTempPath(), $"faysys_logo_temp_{Guid.NewGuid()}.png");
                
                try
                {
                    // Copy stream to temporary file
                    using (var fileStream = File.Create(tempPath))
                    {
                        logoStream.Position = 0; // Reset stream position
                        await logoStream.CopyToAsync(fileStream);
                    }
                    
                    // Verify file was created and has content
                    if (File.Exists(tempPath) && new FileInfo(tempPath).Length > 0)
                    {
                        // Load image from temporary file
                        var logoImage = XImage.FromFile(tempPath);
                        gfx.DrawImage(logoImage, x, y, width, height);
                        
                        // Clean up temporary file
                        File.Delete(tempPath);
                        return true;
                    }
                }
                catch
                {
                    // Clean up temporary file if it exists
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
            }
            catch { }

            return false;
        }

        private void DrawFallbackLogo(XGraphics gfx, double x, double y, double width, double height)
        {
            // Create a professional-looking Faysys logo as fallback
            var logoBrush = new XSolidBrush(XColor.FromArgb(37, 99, 235)); // Professional blue
            var borderPen = new XPen(XColor.FromArgb(29, 78, 216), 0.5); // Subtle border
            
            // Draw rounded rectangle background
            gfx.DrawRectangle(logoBrush, x, y, width, height);
            gfx.DrawRectangle(borderPen, x, y, width, height);
            
            // Draw "F" in the center with better positioning
            var logoFont = new XFont(GetBestAvailableFont(), 10, XFontStyle.Bold);
            var textSize = gfx.MeasureString("F", logoFont);
            double textX = x + (width - textSize.Width) / 2;
            double textY = y + (height / 2) + (textSize.Height / 3);
            
            gfx.DrawString("F", logoFont, XBrushes.White, textX, textY);
        }

        private string GetBestAvailableFont()
        {
            // For PDF generation, we want fonts that render cleanly and professionally
            // PdfSharpCore works best with standard system fonts
            
            // On Windows, try Calibri first (modern, professional)
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    var testFont = new XFont("Calibri", 10, XFontStyle.Regular);
                    return "Calibri";
                }
                catch { }
            }
            
            // Try Arial as universal fallback (available on all platforms)
            try
            {
                var testFont = new XFont("Arial", 10, XFontStyle.Regular);
                return "Arial";
            }
            catch { }
            
            // Try Helvetica (common on Mac/Linux)
            try
            {
                var testFont = new XFont("Helvetica", 10, XFontStyle.Regular);
                return "Helvetica";
            }
            catch { }
            
            // Last resort - Times New Roman (should be available everywhere)
            return "Times New Roman";
        }

        public string ConvertAmountToWords(decimal amount)
        {
            if (amount == 0) return "Zero Rupees Only";

            string[] ones = { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
                             "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
                             "Seventeen", "Eighteen", "Nineteen" };

            string[] tens = { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

            long intAmount = (long)Math.Floor(amount);
            string result = "";

            if (intAmount >= 10000000)
            {
                long crores = intAmount / 10000000;
                result += ConvertHundreds(crores, ones, tens) + " Crore ";
                intAmount %= 10000000;
            }

            if (intAmount >= 100000)
            {
                long lakhs = intAmount / 100000;
                result += ConvertHundreds(lakhs, ones, tens) + " Lakh ";
                intAmount %= 100000;
            }

            if (intAmount >= 1000)
            {
                long thousands = intAmount / 1000;
                result += ConvertHundreds(thousands, ones, tens) + " Thousand ";
                intAmount %= 1000;
            }

            if (intAmount >= 100)
            {
                result += ones[intAmount / 100] + " Hundred ";
                intAmount %= 100;
            }

            if (intAmount > 0)
            {
                if (intAmount < 20)
                    result += ones[intAmount] + " ";
                else
                {
                    result += tens[intAmount / 10] + " ";
                    if (intAmount % 10 > 0)
                        result += ones[intAmount % 10] + " ";
                }
            }

            return result.Trim() + " Rupees Only";
        }

        private string ConvertHundreds(long number, string[] ones, string[] tens)
        {
            string result = "";

            if (number >= 100)
            {
                result += ones[number / 100] + " Hundred ";
                number %= 100;
            }

            if (number >= 20)
            {
                result += tens[number / 10] + " ";
                if (number % 10 > 0)
                    result += ones[number % 10] + " ";
            }
            else if (number > 0)
            {
                result += ones[number] + " ";
            }

            return result.Trim();
        }
    }
}