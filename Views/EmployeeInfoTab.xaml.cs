using faysys_payslip_generator.Models;
using faysys_payslip_generator.Services;

namespace faysys_payslip_generator.Views;

public partial class EmployeeInfoTab : ContentView
{
    private readonly IDataStorageService _dataStorageService;
    private EmployeeInformation? _currentEmployee;
    private List<DynamicEmployeeField> _dynamicFields = new List<DynamicEmployeeField>();

    public EmployeeInfoTab(IDataStorageService dataStorageService)
    {
        InitializeComponent();
        _dataStorageService = dataStorageService;
        InitializePayPeriodPicker();
        LoadEmployeesForDropdown();
    }

    private void InitializePayPeriodPicker()
    {
        // Initialize month picker
        var months = new List<string>
        {
            "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December"
        };
        MonthPicker.ItemsSource = months;
        MonthPicker.SelectedIndex = DateTime.Now.Month - 1;

        // Initialize year picker
        var years = new List<string>();
        for (int year = DateTime.Now.Year - 5; year <= DateTime.Now.Year + 5; year++)
        {
            years.Add(year.ToString());
        }
        YearPicker.ItemsSource = years;
        YearPicker.SelectedItem = DateTime.Now.Year.ToString();

        // Set default pay date
        PayDatePicker.Date = DateTime.Now;
    }

    private async void LoadEmployeesForDropdown()
    {
        try
        {
            var employeeCollection = await _dataStorageService.LoadEmployeeInformationsAsync();
            var employeeList = employeeCollection.Employees
                .Select(e => $"{e.EmployeeName} ({e.EmployeeCode})")
                .ToList();
            
            EmployeeDropdown.ItemsSource = employeeList;
        }
        catch (Exception ex)
        {
            // Handle error silently or show a message
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load employees: {ex.Message}", "OK");
        }
    }

    private async void OnEmployeeDropdownChanged(object sender, EventArgs e)
    {
        var picker = sender as Picker;
        if (picker?.SelectedItem != null)
        {
            var selectedText = picker.SelectedItem.ToString();
            // Extract employee code from "Name (Code)" format
            var codeStart = selectedText.LastIndexOf('(');
            var codeEnd = selectedText.LastIndexOf(')');
            if (codeStart > 0 && codeEnd > codeStart)
            {
                var employeeCode = selectedText.Substring(codeStart + 1, codeEnd - codeStart - 1);
                EmployeeCodeEntry.Text = employeeCode;
                await SearchEmployeeAsync(employeeCode);
            }
        }
    }

    private void OnEmployeeCodeChanged(object sender, TextChangedEventArgs e)
    {
        // Auto-search when employee code is entered
        if (!string.IsNullOrWhiteSpace(e.NewTextValue) && e.NewTextValue.Length >= 3)
        {
            _ = SearchEmployeeAsync(e.NewTextValue);
        }
    }

    private async void OnSearchEmployeeClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EmployeeCodeEntry.Text))
        {
            await Application.Current.MainPage.DisplayAlert("Validation Error", "Please enter an employee code to search.", "OK");
            return;
        }

        await SearchEmployeeAsync(EmployeeCodeEntry.Text.Trim());
    }

    private async Task SearchEmployeeAsync(string employeeCode)
    {
        try
        {
            var employee = await _dataStorageService.GetEmployeeByCodeAsync(employeeCode);
            
            if (employee != null)
            {
                LoadEmployeeData(employee);
                EmployeeDetailsFrame.IsVisible = true;
                DeleteEmployeeButton.IsVisible = true;
            }
            else
            {
                EmployeeDetailsFrame.IsVisible = false;
                await Application.Current.MainPage.DisplayAlert("Not Found", $"No employee found with code: {employeeCode}", "OK");
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to search employee: {ex.Message}", "OK");
        }
    }

    private void OnNewEmployeeClicked(object sender, EventArgs e)
    {
        ClearForm();
        EmployeeDetailsFrame.IsVisible = true;
        DeleteEmployeeButton.IsVisible = false;
        _currentEmployee = null;
    }

    private void LoadEmployeeData(EmployeeInformation employee)
    {
        _currentEmployee = employee;
        
        EmployeeNameEntry.Text = employee.EmployeeName;
        EmployeeCodeDisplayEntry.Text = employee.EmployeeCode;
        PaidDaysEntry.Text = "22"; // Default paid days
        LossOfPayDaysEntry.Text = "0"; // Default LOP days
        PayDatePicker.Date = DateTime.Now;
        
        // Load custom fields
        _dynamicFields.Clear();
        foreach (var customField in employee.CustomFields)
        {
            _dynamicFields.Add(new DynamicEmployeeField 
            { 
                Name = customField.Key, 
                Value = customField.Value 
            });
        }
        RefreshDynamicFields();
    }

    private void ClearForm()
    {
        EmployeeNameEntry.Text = string.Empty;
        EmployeeCodeDisplayEntry.Text = string.Empty;
        PaidDaysEntry.Text = "22";
        LossOfPayDaysEntry.Text = "0";
        PayDatePicker.Date = DateTime.Today;
        MonthPicker.SelectedIndex = DateTime.Now.Month - 1;
        YearPicker.SelectedItem = DateTime.Now.Year.ToString();
        
        _dynamicFields.Clear();
        RefreshDynamicFields();
    }

    private async void OnSaveEmployeeClicked(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(EmployeeCodeDisplayEntry.Text))
            {
                await Application.Current.MainPage.DisplayAlert("Validation Error", "Employee code is required.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(EmployeeNameEntry.Text))
            {
                await Application.Current.MainPage.DisplayAlert("Validation Error", "Employee name is required.", "OK");
                return;
            }

            var employee = new EmployeeInformation
            {
                EmployeeCode = EmployeeCodeDisplayEntry.Text.Trim(),
                EmployeeName = EmployeeNameEntry.Text.Trim()
            };

            // Add custom fields
            foreach (var field in _dynamicFields)
            {
                if (!string.IsNullOrWhiteSpace(field.Name) && !string.IsNullOrWhiteSpace(field.Value))
                {
                    employee.CustomFields[field.Name] = field.Value;
                }
            }

            await _dataStorageService.SaveEmployeeInformationAsync(employee);
            
            _currentEmployee = employee;
            DeleteEmployeeButton.IsVisible = true;
            
            // Refresh the dropdown to include the new/updated employee
            LoadEmployeesForDropdown();
            
            await Application.Current.MainPage.DisplayAlert("Success", "Employee information saved successfully!", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to save employee information: {ex.Message}", "OK");
        }
    }

    private async void OnDeleteEmployeeClicked(object sender, EventArgs e)
    {
        if (_currentEmployee == null)
            return;

        var result = await Application.Current.MainPage.DisplayAlert(
            "Confirm Delete", 
            $"Are you sure you want to delete employee {_currentEmployee.EmployeeName} ({_currentEmployee.EmployeeCode})?", 
            "Delete", 
            "Cancel");

        if (result)
        {
            try
            {
                await _dataStorageService.DeleteEmployeeAsync(_currentEmployee.EmployeeCode);
                
                ClearForm();
                EmployeeCodeEntry.Text = string.Empty;
                EmployeeDetailsFrame.IsVisible = false;
                _currentEmployee = null;
                
                // Refresh the dropdown to remove the deleted employee
                LoadEmployeesForDropdown();
                
                await Application.Current.MainPage.DisplayAlert("Success", "Employee deleted successfully!", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete employee: {ex.Message}", "OK");
            }
        }
    }

    private async void OnAddEmployeeFieldClicked(object sender, EventArgs e)
    {
        string fieldName = await Application.Current.MainPage.DisplayPromptAsync(
            "Add Custom Field", 
            "Enter the name for the custom field:", 
            "Add",
            "Cancel",
            placeholder: "e.g. Department, Designation, Location");
            
        if (string.IsNullOrWhiteSpace(fieldName)) return;

        string fieldValue = await Application.Current.MainPage.DisplayPromptAsync(
            "Add Custom Field", 
            $"Enter the value for '{fieldName}':", 
            "Add",
            "Cancel",
            placeholder: $"Enter {fieldName.ToLower()}");
            
        if (string.IsNullOrWhiteSpace(fieldValue)) return;

        _dynamicFields.Add(new DynamicEmployeeField 
        { 
            Name = fieldName.Trim(), 
            Value = fieldValue.Trim() 
        });
        
        RefreshDynamicFields();
    }

    private void RefreshDynamicFields()
    {
        DynamicEmployeeFieldsContainer.Children.Clear();

        foreach (var field in _dynamicFields)
        {
            var fieldContainer = new VerticalStackLayout
            {
                Spacing = 8
            };

            var nameLabel = new Label
            {
                Text = field.Name,
                FontSize = 14,
                TextColor = Color.FromArgb("#6b7280"),
                FontAttributes = FontAttributes.None
            };

            var fieldGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(40, GridUnitType.Absolute) }
                },
                ColumnSpacing = 12
            };

            var valueEntry = new Entry
            {
                Text = field.Value,
                FontSize = 14,
                BackgroundColor = Color.FromArgb("#f8fafc"),
                TextColor = Color.FromArgb("#111827"),
                HeightRequest = 44,
                Placeholder = $"Enter {field.Name.ToLower()}",
                PlaceholderColor = Color.FromArgb("#9ca3af")
            };
            valueEntry.TextChanged += (s, e) => field.Value = e.NewTextValue;

            var deleteButton = new Button
            {
                Text = "×",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                BackgroundColor = Color.FromArgb("#ef4444"),
                TextColor = Colors.White,
                CornerRadius = 6,
                WidthRequest = 40,
                HeightRequest = 44
            };
            deleteButton.Clicked += (s, e) => 
            {
                _dynamicFields.Remove(field);
                RefreshDynamicFields();
            };

            Grid.SetColumn(valueEntry, 0);
            Grid.SetColumn(deleteButton, 1);

            fieldGrid.Children.Add(valueEntry);
            fieldGrid.Children.Add(deleteButton);

            fieldContainer.Children.Add(nameLabel);
            fieldContainer.Children.Add(fieldGrid);

            DynamicEmployeeFieldsContainer.Children.Add(fieldContainer);
        }
    }

    public class DynamicEmployeeField
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}