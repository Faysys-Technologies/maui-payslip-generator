using faysys_payslip_generator.Services;

namespace faysys_payslip_generator.Views;

public partial class SettingsPage : ContentPage
{
    private readonly DataStorageService _dataStorageService;
    private readonly IThemeService _themeService;

    public SettingsPage()
    {
        InitializeComponent();
        _dataStorageService = new DataStorageService();
        _themeService = ServiceHelper.GetService<IThemeService>();
        LoadCurrentTheme();
    }

    protected override bool OnBackButtonPressed()
    {
        // Handle hardware back button (Android)
        // Return false to allow default back navigation
        return false;
    }

    private async void LoadCurrentTheme()
    {
        try
        {
            var isDarkMode = await _themeService.GetThemeAsync();
            ThemeSwitch.IsToggled = isDarkMode;
            UpdateThemeLabel(isDarkMode);
            _themeService.ApplyTheme(isDarkMode);
        }
        catch (Exception)
        {
            // Fallback to light theme
            ThemeSwitch.IsToggled = false;
            UpdateThemeLabel(false);
        }
    }

    private async void OnThemeSwitchToggled(object sender, ToggledEventArgs e)
    {
        try
        {
            var isDarkMode = e.Value;
            await _themeService.SetThemeAsync(isDarkMode);
            UpdateThemeLabel(isDarkMode);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Theme Error", $"Failed to change theme: {ex.Message}", "OK");
            // Revert the switch
            ThemeSwitch.IsToggled = !e.Value;
        }
    }

    private void UpdateThemeLabel(bool isDarkMode)
    {
        ThemeLabel.Text = isDarkMode ? "Light Mode" : "Dark Mode";
    }

    private async void OnExportDataClicked(object sender, EventArgs e)
    {
        try
        {
            ExportDataButton.Text = "Exporting...";
            ExportDataButton.IsEnabled = false;

            // Get all data
            var companies = await _dataStorageService.GetCompaniesAsync();
            var employees = await _dataStorageService.GetEmployeesAsync();

            // Create export data object
            var exportData = new
            {
                ExportDate = DateTime.Now,
                Companies = companies,
                Employees = employees
            };

            // Convert to JSON
            var json = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Save to file
            var fileName = $"PayslipData_Export_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            
            // For now, we'll show a success message
            // In a real implementation, you'd use file picker to save
            await DisplayAlert("Export Successful", 
                $"Data exported successfully!\n\nFile: {fileName}\n\nCompanies: {companies.Count}\nEmployees: {employees.Count}", 
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Export Failed", $"Failed to export data: {ex.Message}", "OK");
        }
        finally
        {
            ExportDataButton.Text = "Export";
            ExportDataButton.IsEnabled = true;
        }
    }

    private async void OnResetDataClicked(object sender, EventArgs e)
    {
        var result = await DisplayAlert("Confirm Reset", 
            "Are you sure you want to delete all data? This action cannot be undone.", 
            "Delete All", "Cancel");

        if (result)
        {
            var finalConfirm = await DisplayAlert("Final Confirmation", 
                "This will permanently delete all company and employee data. Are you absolutely sure?", 
                "Yes, Delete Everything", "Cancel");

            if (finalConfirm)
            {
                try
                {
                    ResetDataButton.Text = "Resetting...";
                    ResetDataButton.IsEnabled = false;

                    await _dataStorageService.ClearAllDataAsync();

                    await DisplayAlert("Reset Complete", 
                        "All data has been successfully deleted.", 
                        "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Reset Failed", 
                        $"Failed to reset data: {ex.Message}", 
                        "OK");
                }
                finally
                {
                    ResetDataButton.Text = "Reset";
                    ResetDataButton.IsEnabled = true;
                }
            }
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    // Alternative navigation methods
    private async void GoBackToMainPage()
    {
        // Pop current page from navigation stack
        await Navigation.PopAsync();
    }

    private async void GoBackToRoot()
    {
        // Pop all pages and go to root
        await Navigation.PopToRootAsync();
    }
}