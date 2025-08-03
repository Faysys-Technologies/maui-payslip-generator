using System.Text.Json;
using faysys_payslip_generator.Models;

namespace faysys_payslip_generator.Services
{
    public interface IDataStorageService
    {
        Task<CompanyInformation> LoadCompanyInformationAsync();
        Task SaveCompanyInformationAsync(CompanyInformation companyInfo);
        Task<EmployeeInformationCollection> LoadEmployeeInformationsAsync();
        Task SaveEmployeeInformationAsync(EmployeeInformation employeeInfo);
        Task<EmployeeInformation?> GetEmployeeByCodeAsync(string employeeCode);
        Task DeleteEmployeeAsync(string employeeCode);
        Task<List<CompanyInformation>> GetCompaniesAsync();
        Task<List<EmployeeInformation>> GetEmployeesAsync();
        Task ClearAllDataAsync();
        
        // Income Details methods
        Task<IncomeDetailsCollection> LoadIncomeDetailsAsync();
        Task SaveIncomeDetailsAsync(IncomeDetails incomeDetails);
        Task<IncomeDetails?> GetIncomeDetailsByEmployeeCodeAsync(string employeeCode);
        Task DeleteIncomeDetailsAsync(string employeeCode);
        Task<List<IncomeDetails>> GetAllIncomeDetailsAsync();
    }

    public class DataStorageService : IDataStorageService
    {
        private readonly string _appDataPath;
        private readonly string _companyInfoPath;
        private readonly string _employeeInfoPath;
        private readonly string _incomeDetailsPath;

        public DataStorageService()
        {
            _appDataPath = Path.Combine(FileSystem.AppDataDirectory, "PayslipGenerator");
            _companyInfoPath = Path.Combine(_appDataPath, "companyInformation.json");
            _employeeInfoPath = Path.Combine(_appDataPath, "employeeInformations.json");
            _incomeDetailsPath = Path.Combine(_appDataPath, "incomeDetails.json");

            // Ensure directory exists
            Directory.CreateDirectory(_appDataPath);
        }

        public async Task<CompanyInformation> LoadCompanyInformationAsync()
        {
            try
            {
                if (!File.Exists(_companyInfoPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Company information file not found at: {_companyInfoPath}");
                    return new CompanyInformation();
                }

                var json = await File.ReadAllTextAsync(_companyInfoPath);
                System.Diagnostics.Debug.WriteLine($"Loaded company info: {json}");
                return JsonSerializer.Deserialize<CompanyInformation>(json) ?? new CompanyInformation();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading company information: {ex}");
                return new CompanyInformation();
            }
        }

        public async Task SaveCompanyInformationAsync(CompanyInformation companyInfo)
        {
            try
            {
                companyInfo.LastUpdated = DateTime.Now;
                var json = JsonSerializer.Serialize(companyInfo, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                await File.WriteAllTextAsync(_companyInfoPath, json);
            }
            catch (Exception ex)
            {
                // Log error or handle as needed
                throw new InvalidOperationException("Failed to save company information", ex);
            }
        }

        public async Task<EmployeeInformationCollection> LoadEmployeeInformationsAsync()
        {
            try
            {
                if (!File.Exists(_employeeInfoPath))
                    return new EmployeeInformationCollection();

                var json = await File.ReadAllTextAsync(_employeeInfoPath);
                return JsonSerializer.Deserialize<EmployeeInformationCollection>(json) ?? new EmployeeInformationCollection();
            }
            catch
            {
                return new EmployeeInformationCollection();
            }
        }

        public async Task SaveEmployeeInformationAsync(EmployeeInformation employeeInfo)
        {
            try
            {
                var collection = await LoadEmployeeInformationsAsync();
                
                // Remove existing employee with same code
                collection.Employees.RemoveAll(e => e.EmployeeCode == employeeInfo.EmployeeCode);
                
                // Add updated employee info
                employeeInfo.LastUpdated = DateTime.Now;
                collection.Employees.Add(employeeInfo);
                collection.LastUpdated = DateTime.Now;

                var json = JsonSerializer.Serialize(collection, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                await File.WriteAllTextAsync(_employeeInfoPath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save employee information", ex);
            }
        }

        public async Task<EmployeeInformation?> GetEmployeeByCodeAsync(string employeeCode)
        {
            var collection = await LoadEmployeeInformationsAsync();
            return collection.Employees.FirstOrDefault(e => e.EmployeeCode == employeeCode);
        }

        public async Task DeleteEmployeeAsync(string employeeCode)
        {
            try
            {
                var collection = await LoadEmployeeInformationsAsync();
                collection.Employees.RemoveAll(e => e.EmployeeCode == employeeCode);
                collection.LastUpdated = DateTime.Now;

                var json = JsonSerializer.Serialize(collection, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                await File.WriteAllTextAsync(_employeeInfoPath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to delete employee information", ex);
            }
        }

        public async Task<List<CompanyInformation>> GetCompaniesAsync()
        {
            var companyInfo = await LoadCompanyInformationAsync();
            return new List<CompanyInformation> { companyInfo };
        }

        public async Task<List<EmployeeInformation>> GetEmployeesAsync()
        {
            var collection = await LoadEmployeeInformationsAsync();
            return collection.Employees;
        }

        public async Task ClearAllDataAsync()
        {
            try
            {
                if (File.Exists(_companyInfoPath))
                    File.Delete(_companyInfoPath);
                
                if (File.Exists(_employeeInfoPath))
                    File.Delete(_employeeInfoPath);
                    
                if (File.Exists(_incomeDetailsPath))
                    File.Delete(_incomeDetailsPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to clear all data", ex);
            }
        }
        
        // Income Details methods
        public async Task<IncomeDetailsCollection> LoadIncomeDetailsAsync()
        {
            try
            {
                if (!File.Exists(_incomeDetailsPath))
                    return new IncomeDetailsCollection();

                var json = await File.ReadAllTextAsync(_incomeDetailsPath);
                return JsonSerializer.Deserialize<IncomeDetailsCollection>(json) ?? new IncomeDetailsCollection();
            }
            catch
            {
                return new IncomeDetailsCollection();
            }
        }

        public async Task SaveIncomeDetailsAsync(IncomeDetails incomeDetails)
        {
            try
            {
                var collection = await LoadIncomeDetailsAsync();
                
                // Remove existing income details with same employee code
                collection.IncomeDetailsList.RemoveAll(i => i.EmployeeCode == incomeDetails.EmployeeCode);
                
                // Add updated income details
                incomeDetails.LastUpdated = DateTime.Now;
                collection.IncomeDetailsList.Add(incomeDetails);
                collection.LastUpdated = DateTime.Now;

                var json = JsonSerializer.Serialize(collection, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                await File.WriteAllTextAsync(_incomeDetailsPath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save income details", ex);
            }
        }

        public async Task<IncomeDetails?> GetIncomeDetailsByEmployeeCodeAsync(string employeeCode)
        {
            var collection = await LoadIncomeDetailsAsync();
            return collection.IncomeDetailsList.FirstOrDefault(i => i.EmployeeCode == employeeCode);
        }

        public async Task DeleteIncomeDetailsAsync(string employeeCode)
        {
            try
            {
                var collection = await LoadIncomeDetailsAsync();
                collection.IncomeDetailsList.RemoveAll(i => i.EmployeeCode == employeeCode);
                collection.LastUpdated = DateTime.Now;

                var json = JsonSerializer.Serialize(collection, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                await File.WriteAllTextAsync(_incomeDetailsPath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to delete income details", ex);
            }
        }

        public async Task<List<IncomeDetails>> GetAllIncomeDetailsAsync()
        {
            var collection = await LoadIncomeDetailsAsync();
            return collection.IncomeDetailsList;
        }
    }
}