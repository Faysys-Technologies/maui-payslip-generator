using faysys_payslip_generator.Models;
using faysys_payslip_generator.Services;

namespace faysys_payslip_generator.Views;

public partial class CompanyInfoTab : ContentView
{
    private readonly IDataStorageService _dataStorageService;

    public CompanyInfoTab(IDataStorageService dataStorageService)
    {
        InitializeComponent();
        _dataStorageService = dataStorageService;
        LoadCompanyInformation();
    }

    private async void LoadCompanyInformation()
    {
        try
        {
            var companyInfo = await _dataStorageService.LoadCompanyInformationAsync();
            
            CompanyNameEntry.Text = companyInfo.CompanyName;
            CompanyAddressEntry.Text = companyInfo.Address;
            CityPincodeEntry.Text = companyInfo.CityPincode;
            CountryEntry.Text = companyInfo.Country;
            
            if (!string.IsNullOrEmpty(companyInfo.LogoPath) && File.Exists(companyInfo.LogoPath))
            {
                CompanyLogoImage.Source = ImageSource.FromFile(companyInfo.LogoPath);
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load company information: {ex.Message}", "OK");
        }
    }

    private async void OnUploadLogoClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Select Company Logo",
                FileTypes = FilePickerFileType.Images
            });

            if (result != null)
            {
                // Copy file to app data directory
                var fileName = $"company_logo_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(result.FileName)}";
                var destinationPath = Path.Combine(FileSystem.AppDataDirectory, "PayslipGenerator", fileName);
                
                using var sourceStream = await result.OpenReadAsync();
                using var destinationStream = File.Create(destinationPath);
                await sourceStream.CopyToAsync(destinationStream);

                CompanyLogoImage.Source = ImageSource.FromFile(destinationPath);
                
                // Save the logo path
                var companyInfo = await _dataStorageService.LoadCompanyInformationAsync();
                companyInfo.LogoPath = destinationPath;
                await _dataStorageService.SaveCompanyInformationAsync(companyInfo);
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to upload logo: {ex.Message}", "OK");
        }
    }

    private async void OnSaveCompanyClicked(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(CompanyNameEntry.Text))
            {
                await Application.Current.MainPage.DisplayAlert("Validation Error", "Company name is required.", "OK");
                return;
            }

            var companyInfo = new CompanyInformation
            {
                CompanyName = CompanyNameEntry.Text?.Trim() ?? string.Empty,
                Address = CompanyAddressEntry.Text?.Trim() ?? string.Empty,
                CityPincode = CityPincodeEntry.Text?.Trim() ?? string.Empty,
                Country = CountryEntry.Text?.Trim() ?? "India"
            };

            // Preserve existing logo path
            var existingInfo = await _dataStorageService.LoadCompanyInformationAsync();
            companyInfo.LogoPath = existingInfo.LogoPath;

            await _dataStorageService.SaveCompanyInformationAsync(companyInfo);
            
            await Application.Current.MainPage.DisplayAlert("Success", "Company information saved successfully!", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to save company information: {ex.Message}", "OK");
        }
    }
}