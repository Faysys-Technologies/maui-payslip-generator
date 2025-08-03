using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using faysys_payslip_generator.Models;
using faysys_payslip_generator.Services;

namespace faysys_payslip_generator.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly IPdfGenerationService _pdfGenerationService;
        private readonly IDataStorageService _dataStorageService;
        private List<EarningDeductionItem> _earningsItems = new List<EarningDeductionItem>();
        private List<EarningDeductionItem> _deductionsItems = new List<EarningDeductionItem>();
        private EmployeeInformation? _selectedEmployee;
        private string? _pdfPath;
        
        // Tab content references
        private Views.CompanyInfoTab CompanyInfoTabContent;
        private Views.EmployeeInfoTab EmployeeInfoTabContent;

        public MainPage(IPdfGenerationService pdfGenerationService, IDataStorageService dataStorageService)
        {
            _pdfGenerationService = pdfGenerationService;
            _dataStorageService = dataStorageService;
            InitializeComponent();
            UpdateTotals();
            
            // Initialize tab content with services
            InitializeTabContent();
        }

        private void InitializeTabContent()
        {
            // Create and add company info tab
            var companyTab = new Views.CompanyInfoTab(_dataStorageService);
            companyTab.IsVisible = true;
            TabContentGrid.Children.Add(companyTab);
            CompanyInfoTabContent = companyTab;
            
            // Create and add employee info tab
            var employeeTab = new Views.EmployeeInfoTab(_dataStorageService);
            employeeTab.IsVisible = false;
            TabContentGrid.Children.Add(employeeTab);
            EmployeeInfoTabContent = employeeTab;
            
            // Load employees for dropdown
            LoadEmployeesForDropdown();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Use Dispatcher.Dispatch to ensure UI is ready
            Dispatcher.Dispatch(() =>
            {
                InitializePayPeriodPicker();
            });
        }

        private async void LoadEmployeesForDropdown()
        {
            try
            {
                var employeeCollection = await _dataStorageService.LoadEmployeeInformationsAsync();
                if (employeeCollection?.Employees != null)
                {
                    var employeeList = employeeCollection.Employees
                        .Where(e => e != null && !string.IsNullOrEmpty(e.EmployeeName) && !string.IsNullOrEmpty(e.EmployeeCode))
                        .Select(e => $"{e.EmployeeName.Trim()} ({e.EmployeeCode.Trim()})")
                        .OrderBy(name => name)
                        .ToList();
                    
                    EmployeeDropdown.ItemsSource = employeeList;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load employees: {ex.Message}", "OK");
                // Consider logging the error to a logging service
                System.Diagnostics.Debug.WriteLine($"Error loading employees: {ex}");
            }
        }

        private void InitializePayPeriodPicker()
        {
            try
            {
                // Initialize month picker
                var months = new List<string>
                {
                    "January", "February", "March", "April", "May", "June",
                    "July", "August", "September", "October", "November", "December"
                };
                
                if (MonthPicker != null)
                {
                    MonthPicker.ItemsSource = months;
                    MonthPicker.SelectedIndex = DateTime.Now.Month - 1;
                    System.Diagnostics.Debug.WriteLine($"Month picker initialized with {months.Count} items, selected index: {MonthPicker.SelectedIndex}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("MonthPicker is null");
                }

                // Initialize year picker
                var years = new List<string>();
                for (int year = DateTime.Now.Year - 5; year <= DateTime.Now.Year + 5; year++)
                {
                    years.Add(year.ToString());
                }
                
                if (YearPicker != null)
                {
                    YearPicker.ItemsSource = years;
                    YearPicker.SelectedItem = DateTime.Now.Year.ToString();
                    System.Diagnostics.Debug.WriteLine($"Year picker initialized with {years.Count} items, selected item: {YearPicker.SelectedItem}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("YearPicker is null");
                }

                // Set default pay date
                if (PayDatePicker != null)
                {
                    PayDatePicker.Date = DateTime.Now;
                    System.Diagnostics.Debug.WriteLine($"Pay date picker initialized with date: {PayDatePicker.Date}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("PayDatePicker is null");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing pay period picker: {ex}");
            }
        }

        // Tab Navigation Methods
        private void OnCompanyTabClicked(object sender, EventArgs e)
        {
            SetActiveTab(0);
        }

        private void OnEmployeeTabClicked(object sender, EventArgs e)
        {
            SetActiveTab(1);
        }

        private void OnPayslipTabClicked(object sender, EventArgs e)
        {
            SetActiveTab(2);
            // Refresh pickers when Income Details tab is activated
            Dispatcher.Dispatch(() =>
            {
                InitializePayPeriodPicker();
            });
        }
        
        private void OnMonthPickerChanged(object sender, EventArgs e)
        {
            // Update any dependent fields when month changes
            UpdateTotals();
        }
        
        private void OnYearPickerChanged(object sender, EventArgs e)
        {
            // Update any dependent fields when year changes
            UpdateTotals();
        }
        
        private void OnPaidDaysChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(e.NewTextValue))
                {
                    // Handle empty input
                    UpdateTotals();
                    return;
                }

                // Allow only numeric input
                string filteredText = new string(e.NewTextValue.Where(c => char.IsDigit(c)).ToArray());

                if (int.TryParse(filteredText, out int paidDays))
                {
                    if (paidDays < 0)
                    {
                        // Reset to 0 if negative
                        if (sender is Entry entry)
                        {
                            entry.Text = "0";
                        }
                        paidDays = 0;
                    }
                    
                    // Recalculate daily rate if basic amount is available
                    if (decimal.TryParse(BasicEarningEntry?.Text, out decimal basicAmount))
                    {
                        CalculateDailyRate(basicAmount);
                    }
                    
                    // Update any calculations based on paid days
                    UpdateTotals();
                }
                else if (!string.IsNullOrEmpty(e.OldTextValue) && !string.IsNullOrEmpty(e.NewTextValue))
                {
                    // Only revert if the new value is not empty and invalid
                    if (sender is Entry entry)
                    {
                        entry.Text = e.OldTextValue;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnPaidDaysChanged: {ex}");
            }
        }
        
        private void OnLossOfPayDaysChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(e.NewTextValue))
                {
                    // Handle empty input
                    UpdateTotals();
                    return;
                }

                // Allow only numeric input
                string filteredText = new string(e.NewTextValue.Where(c => char.IsDigit(c)).ToArray());

                if (int.TryParse(filteredText, out int lopDays))
                {
                    if (lopDays < 0)
                    {
                        // Reset to 0 if negative
                        if (sender is Entry entry)
                        {
                            entry.Text = "0";
                        }
                        lopDays = 0;
                    }
                    
                    // Update any calculations based on loss of pay days
                    UpdateTotals();
                }
                else if (!string.IsNullOrEmpty(e.OldTextValue) && !string.IsNullOrEmpty(e.NewTextValue))
                {
                    // Only revert if the new value is not empty and invalid
                    if (sender is Entry entry)
                    {
                        entry.Text = e.OldTextValue;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnLossOfPayDaysChanged: {ex}");
            }
        }

        private void OnBasicEarningChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(e.NewTextValue))
                {
                    UpdateTotals();
                    return;
                }

                // Allow only numeric input with decimal point
                string filteredText = new string(e.NewTextValue.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
                
                // Ensure only one decimal point
                int decimalCount = filteredText.Count(c => c == '.' || c == ',');
                if (decimalCount > 1)
                {
                    filteredText = filteredText.Substring(0, filteredText.LastIndexOf('.'));
                }

                if (decimal.TryParse(filteredText, out decimal amount))
                {
                    if (amount < 0)
                    {
                        if (sender is Entry entry)
                        {
                            entry.Text = "0";
                        }
                    }
                    UpdateTotals();
                }
                else if (!string.IsNullOrEmpty(e.OldTextValue) && !string.IsNullOrEmpty(e.NewTextValue))
                {
                    if (sender is Entry entry)
                    {
                        entry.Text = e.OldTextValue;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnBasicEarningChanged: {ex}");
            }
        }

        private void OnHraEarningChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(e.NewTextValue))
                {
                    UpdateTotals();
                    return;
                }

                string filteredText = new string(e.NewTextValue.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
                
                int decimalCount = filteredText.Count(c => c == '.' || c == ',');
                if (decimalCount > 1)
                {
                    filteredText = filteredText.Substring(0, filteredText.LastIndexOf('.'));
                }

                if (decimal.TryParse(filteredText, out decimal amount))
                {
                    if (amount < 0)
                    {
                        if (sender is Entry entry)
                        {
                            entry.Text = "0";
                        }
                    }
                    UpdateTotals();
                }
                else if (!string.IsNullOrEmpty(e.OldTextValue) && !string.IsNullOrEmpty(e.NewTextValue))
                {
                    if (sender is Entry entry)
                    {
                        entry.Text = e.OldTextValue;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnHraEarningChanged: {ex}");
            }
        }

        private void OnIncomeTaxDeductionChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(e.NewTextValue))
                {
                    UpdateTotals();
                    return;
                }

                string filteredText = new string(e.NewTextValue.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
                
                int decimalCount = filteredText.Count(c => c == '.' || c == ',');
                if (decimalCount > 1)
                {
                    filteredText = filteredText.Substring(0, filteredText.LastIndexOf('.'));
                }

                if (decimal.TryParse(filteredText, out decimal amount))
                {
                    if (amount < 0)
                    {
                        if (sender is Entry entry)
                        {
                            entry.Text = "0";
                        }
                    }
                    UpdateTotals();
                }
                else if (!string.IsNullOrEmpty(e.OldTextValue) && !string.IsNullOrEmpty(e.NewTextValue))
                {
                    if (sender is Entry entry)
                    {
                        entry.Text = e.OldTextValue;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnIncomeTaxDeductionChanged: {ex}");
            }
        }

        private void OnPfDeductionChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(e.NewTextValue))
                {
                    UpdateTotals();
                    return;
                }

                string filteredText = new string(e.NewTextValue.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
                
                int decimalCount = filteredText.Count(c => c == '.' || c == ',');
                if (decimalCount > 1)
                {
                    filteredText = filteredText.Substring(0, filteredText.LastIndexOf('.'));
                }

                if (decimal.TryParse(filteredText, out decimal amount))
                {
                    if (amount < 0)
                    {
                        if (sender is Entry entry)
                        {
                            entry.Text = "0";
                        }
                    }
                    UpdateTotals();
                }
                else if (!string.IsNullOrEmpty(e.OldTextValue) && !string.IsNullOrEmpty(e.NewTextValue))
                {
                    if (sender is Entry entry)
                    {
                        entry.Text = e.OldTextValue;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnPfDeductionChanged: {ex}");
            }
        }

        private void CalculateDailyRate(decimal basicAmount)
        {
            try
            {
                // Daily rate calculation is no longer needed in new design
                // This method is kept for compatibility but doesn't update any UI
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CalculateDailyRate: {ex}");
            }
        }

        private void SetActiveTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex > 2) return;

            // Reset all tab buttons
            var inactiveTextColor = Application.Current.RequestedTheme == AppTheme.Dark ? 
                Color.FromArgb("#9CA3AF") : // Gray-400 for dark theme
                Color.FromArgb("#6B7280");   // Gray-500 for light theme
                
            CompanyTabButton.TextColor = inactiveTextColor;
            EmployeeTabButton.TextColor = inactiveTextColor;
            PayslipTabButton.TextColor = inactiveTextColor;
            
            CompanyTabButton.FontAttributes = FontAttributes.None;
            EmployeeTabButton.FontAttributes = FontAttributes.None;
            PayslipTabButton.FontAttributes = FontAttributes.None;

            // Hide all tab content and reset animations
            if (CompanyInfoTabContent != null)
            {
                CompanyInfoTabContent.IsVisible = false;
                CompanyInfoTabContent.Opacity = 0;
            }
            if (EmployeeInfoTabContent != null)
            {
                EmployeeInfoTabContent.IsVisible = false;
                EmployeeInfoTabContent.Opacity = 0;
            }
            if (PayslipTabContent != null)
            {
                PayslipTabContent.IsVisible = false;
                PayslipTabContent.Opacity = 0;
            }

            // Show selected tab
            var activeTextColor = Application.Current.RequestedTheme == AppTheme.Dark ? 
                Color.FromArgb("#93C5FD") : // Blue-300 for dark theme
                Color.FromArgb("#1A6AB3");   // Your primary blue for light theme

            switch (tabIndex)
            {
                case 0:
                    if (CompanyInfoTabContent != null)
                    {
                        CompanyInfoTabContent.IsVisible = true;
                        CompanyInfoTabContent.FadeTo(1, 150);
                    }
                    CompanyTabButton.TextColor = activeTextColor;
                    CompanyTabButton.FontAttributes = FontAttributes.Bold;
                    // Move indicator to Company tab
                    ActiveTabIndicator.TranslateTo(0, 0, 150, Easing.CubicOut);
                    break;
                case 1:
                    if (EmployeeInfoTabContent != null)
                    {
                        EmployeeInfoTabContent.IsVisible = true;
                        EmployeeInfoTabContent.FadeTo(1, 150);
                    }
                    EmployeeTabButton.TextColor = activeTextColor;
                    EmployeeTabButton.FontAttributes = FontAttributes.Bold;
                    // Move indicator to Employee tab
                    ActiveTabIndicator.TranslateTo(CompanyTabBorder.Width, 0, 150, Easing.CubicOut);
                    break;
                case 2:
                    if (PayslipTabContent != null)
                    {
                        PayslipTabContent.IsVisible = true;
                        PayslipTabContent.FadeTo(1, 150);
                    }
                    PayslipTabButton.TextColor = activeTextColor;
                    PayslipTabButton.FontAttributes = FontAttributes.Bold;
                    // Move indicator to Income Details tab
                    ActiveTabIndicator.TranslateTo(CompanyTabBorder.Width + EmployeeTabBorder.Width, 0, 150, Easing.CubicOut);
                    break;
            }
        }

        // Payslip Tab Methods
        private async void OnPayslipEmployeeCodeChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.NewTextValue) && e.NewTextValue.Length >= 3)
            {
                await LoadEmployeeForPayslipAsync(e.NewTextValue);
            }
        }

        private async void OnEmployeeDropdownChanged(object sender, EventArgs e)
        {
            try
            {
                if (sender is not Picker picker || picker.SelectedItem == null) return;
                
                var selectedText = picker.SelectedItem.ToString();
                if (string.IsNullOrEmpty(selectedText)) return;
                
                // Extract employee code from "Name (Code)" format
                var codeStart = selectedText.LastIndexOf('(');
                var codeEnd = selectedText.LastIndexOf(')');
                
                if (codeStart > 0 && codeEnd > codeStart && codeEnd > codeStart + 1)
                {
                    var employeeCode = selectedText.Substring(codeStart + 1, codeEnd - codeStart - 1).Trim();
                    if (!string.IsNullOrEmpty(employeeCode))
                    {
                        if (PayslipEmployeeCodeEntry != null)
                        {
                            PayslipEmployeeCodeEntry.Text = employeeCode;
                        }
                        await LoadEmployeeForPayslipAsync(employeeCode);
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to process employee selection.", "OK");
                System.Diagnostics.Debug.WriteLine($"Error in OnEmployeeDropdownChanged: {ex}");
            }
           
        }

        private async void OnLoadEmployeeForPayslipClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PayslipEmployeeCodeEntry.Text))
            {
                await DisplayAlert("Validation Error", "Please enter an employee code.", "OK");
                return;
            }

            await LoadEmployeeForPayslipAsync(PayslipEmployeeCodeEntry.Text.Trim());
        }

        private async Task LoadEmployeeForPayslipAsync(string employeeCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(employeeCode))
                {
                    await DisplayAlert("Error", "Please enter a valid employee code.", "OK");
                    return;
                }

                var employee = await _dataStorageService.GetEmployeeByCodeAsync(employeeCode.Trim());
                
                if (employee != null)
                {
                    _selectedEmployee = employee;
                    
                    // Update UI with employee details
                    SelectedEmployeeLabel.Text = employee.EmployeeName;
                    EmployeeDesignationLabel.Text = $"{employee.Designation} | {employee.Department}";
                    
                    // Show employee details container
                    SelectedEmployeeContainer.IsVisible = true;
                    
                    // Show payslip forms
                    PayslipDetailsFrame.IsVisible = true;
                    EarningsDeductionsFrame.IsVisible = true;
                    SummaryFrame.IsVisible = true;
                    GeneratePayslipButton.IsVisible = true;
                    
                    // Initialize with predefined fields populated from employee data
                    _earningsItems.Clear();
                    _deductionsItems.Clear();
                    
                    // Populate predefined fields with employee data
                    if (BasicEarningEntry != null)
                        BasicEarningEntry.Text = employee.BasicSalary.ToString();
                        
                    if (HraEarningEntry != null)
                        HraEarningEntry.Text = employee.HouseRentAllowance.ToString();
                    
                    // Add any additional earnings that aren't covered by predefined fields
                    if (employee.ConveyanceAllowance > 0)
                    {
                        _earningsItems.Add(new EarningDeductionItem
                        {
                            Name = "Conveyance Allowance",
                            Amount = employee.ConveyanceAllowance,
                            IsReadOnly = false
                        });
                    }
                    
                    // Add any additional deductions that aren't covered by predefined fields
                    if (employee.ProfessionalTax > 0)
                    {
                        _deductionsItems.Add(new EarningDeductionItem
                        {
                            Name = "Professional Tax",
                            Amount = employee.ProfessionalTax,
                            IsReadOnly = false
                        });
                    }
                    
                    RefreshEarningsDeductionsDisplay();
                    UpdateTotals();
                    
                    // Try to load previous income details for this employee
                    await LoadIncomeDetailsForEmployee(employeeCode);
                    
                    // Auto-focus on the month picker for better UX
                    if (MonthPicker != null)
                        MonthPicker.Focus();
                }
                else
                {
                    await DisplayAlert("Not Found", $"No employee found with code: {employeeCode}", "OK");
                    SelectedEmployeeContainer.IsVisible = false;
                    HidePayslipForms();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load employee: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Error in LoadEmployeeForPayslipAsync: {ex}");
            }
        }

        private void HidePayslipForms()
        {
            SelectedEmployeeContainer.IsVisible = false;
            PayslipDetailsFrame.IsVisible = false;
            EarningsDeductionsFrame.IsVisible = false;
            SummaryFrame.IsVisible = false;
            GeneratePayslipButton.IsVisible = false;
            _selectedEmployee = null;
            
            // Clear all inputs
            if (PayslipEmployeeCodeEntry != null)
                PayslipEmployeeCodeEntry.Text = string.Empty;
                
            if (EmployeeDropdown != null)
                EmployeeDropdown.SelectedIndex = -1;
                
            // Reset pay period to current month/year
            if (MonthPicker != null)
                MonthPicker.SelectedIndex = DateTime.Now.Month - 1;
                
            if (YearPicker != null)
                YearPicker.SelectedItem = DateTime.Now.Year.ToString();
                
            if (PayDatePicker != null)
                PayDatePicker.Date = DateTime.Now;
                
            if (PaidDaysEntry != null)
                PaidDaysEntry.Text = "";
                
            if (LossOfPayDaysEntry != null)
                LossOfPayDaysEntry.Text = "";
                
            if (BasicEarningEntry != null)
                BasicEarningEntry.Text = "0";
                
            if (HraEarningEntry != null)
                HraEarningEntry.Text = "0";
                
            if (IncomeTaxDeductionEntry != null)
                IncomeTaxDeductionEntry.Text = "0";
                
            if (PfDeductionEntry != null)
                PfDeductionEntry.Text = "0";
                
            // Clear earnings and deductions
            _earningsItems.Clear();
            _deductionsItems.Clear();
            RefreshEarningsDeductionsDisplay();
            
            // Reset totals
            UpdateTotals();
        }

        private void OnAddEarningsClicked(object sender, EventArgs e)
        {
            AddEarningsDeductionItem(true);
        }

        private void OnAddDeductionsClicked(object sender, EventArgs e)
        {
            AddEarningsDeductionItem(false);
        }

        private async void AddEarningsDeductionItem(bool isEarnings)
        {
            try
            {
                var name = await DisplayPromptAsync(
                    isEarnings ? "Add Earning" : "Add Deduction",
                    $"Enter {(isEarnings ? "earning" : "deduction")} name:",
                    "Add",
                    "Cancel",
                    "e.g. " + (isEarnings ? "Bonus, Overtime" : "Loan, Advance"));

                if (string.IsNullOrWhiteSpace(name))
                    return;

                var amountStr = await DisplayPromptAsync(
                    "Enter Amount",
                    $"Enter amount for {name}:",
                    "Add",
                    "Cancel",
                    "0.00",
                    keyboard: Keyboard.Numeric);

                if (string.IsNullOrWhiteSpace(amountStr) || !decimal.TryParse(amountStr, out decimal amount))
                {
                    await DisplayAlert("Invalid Amount", "Please enter a valid amount.", "OK");
                    return;
                }

                var item = new EarningDeductionItem 
                { 
                    Name = name.Trim(),
                    Amount = Math.Round(Math.Abs(amount), 2),
                    IsReadOnly = false
                };
                
                if (isEarnings)
                    _earningsItems.Add(item);
                else
                    _deductionsItems.Add(item);

                RefreshEarningsDeductionsDisplay();
                UpdateTotals();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to add item: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Error in AddEarningsDeductionItem: {ex}");
            }
        }

        private void RefreshEarningsDeductionsDisplay()
        {
            try
            {
                DynamicEarningsContainer.Children.Clear();
                DynamicDeductionsContainer.Children.Clear();
                
                // Display dynamic earnings
                for (int i = 0; i < _earningsItems.Count; i++)
                {
                    var earningsItem = _earningsItems[i];
                    
                    var grid = new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitionCollection
                        {
                            new ColumnDefinition { Width = GridLength.Star },
                            new ColumnDefinition { Width = GridLength.Auto },
                            new ColumnDefinition { Width = GridLength.Auto }
                        },
                        ColumnSpacing = 12,
                        Margin = new Thickness(0, 0, 0, 8)
                    };

                    var earningsNameEntry = new Entry
                    {
                        Text = earningsItem.Name,
                        FontSize = 14,
                        BackgroundColor = Color.FromArgb("#f8fafc"),
                        TextColor = Color.FromArgb("#374151"),
                        HeightRequest = 36,
                        Margin = new Thickness(8, 0, 0, 0)
                    };
                    earningsNameEntry.TextChanged += (s, e) => earningsItem.Name = e.NewTextValue;
                    
                    var earningsAmountEntry = new Entry
                    {
                        Text = earningsItem.Amount.ToString(),
                        FontSize = 14,
                        BackgroundColor = Color.FromArgb("#f8fafc"),
                        TextColor = Color.FromArgb("#374151"),
                        Keyboard = Keyboard.Numeric,
                        HeightRequest = 36,
                        WidthRequest = 100,
                        HorizontalTextAlignment = TextAlignment.End,
                        Margin = new Thickness(8, 0, 0, 0)
                    };
                    earningsAmountEntry.TextChanged += (s, e) => 
                    {
                        if (string.IsNullOrWhiteSpace(e.NewTextValue))
                        {
                            earningsItem.Amount = 0;
                            UpdateTotals();
                            return;
                        }

                        string filteredText = new string(e.NewTextValue.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
                        
                        int decimalCount = filteredText.Count(c => c == '.' || c == ',');
                        if (decimalCount > 1)
                        {
                            filteredText = filteredText.Substring(0, filteredText.LastIndexOf('.'));
                        }

                        if (decimal.TryParse(filteredText, out decimal amount))
                        {
                            earningsItem.Amount = amount;
                            UpdateTotals();
                        }
                    };
                    
                    var deleteButton = new Button
                    {
                        Text = "×",
                        FontSize = 12,
                        BackgroundColor = Color.FromArgb("#ef4444"),
                        TextColor = Colors.White,
                        CornerRadius = 4,
                        WidthRequest = 28,
                        HeightRequest = 28
                    };
                    
                    int currentIndex = i;
                    deleteButton.Clicked += (s, e) => 
                    {
                        _earningsItems.RemoveAt(currentIndex);
                        RefreshEarningsDeductionsDisplay();
                        UpdateTotals();
                    };
                    
                    Grid.SetColumn(earningsNameEntry, 0);
                    Grid.SetColumn(earningsAmountEntry, 1);
                    Grid.SetColumn(deleteButton, 2);
                    grid.Children.Add(earningsNameEntry);
                    grid.Children.Add(earningsAmountEntry);
                    grid.Children.Add(deleteButton);

                    DynamicEarningsContainer.Children.Add(grid);
                }
                
                // Display dynamic deductions
                for (int i = 0; i < _deductionsItems.Count; i++)
                {
                    var deductionItem = _deductionsItems[i];
                    
                    var grid = new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitionCollection
                        {
                            new ColumnDefinition { Width = GridLength.Star },
                            new ColumnDefinition { Width = GridLength.Auto },
                            new ColumnDefinition { Width = GridLength.Auto }
                        },
                        ColumnSpacing = 12,
                        Margin = new Thickness(0, 0, 0, 8)
                    };

                    var deductionNameEntry = new Entry
                    {
                        Text = deductionItem.Name,
                        FontSize = 14,
                        BackgroundColor = Color.FromArgb("#f8fafc"),
                        TextColor = Color.FromArgb("#374151"),
                        HeightRequest = 36,
                        Margin = new Thickness(8, 0, 0, 0)
                    };
                    deductionNameEntry.TextChanged += (s, e) => deductionItem.Name = e.NewTextValue;
                    
                    var deductionAmountEntry = new Entry
                    {
                        Text = deductionItem.Amount.ToString(),
                        FontSize = 14,
                        BackgroundColor = Color.FromArgb("#f8fafc"),
                        TextColor = Color.FromArgb("#374151"),
                        Keyboard = Keyboard.Numeric,
                        HeightRequest = 36,
                        WidthRequest = 100,
                        HorizontalTextAlignment = TextAlignment.End,
                        Margin = new Thickness(8, 0, 0, 0)
                    };
                    deductionAmountEntry.TextChanged += (s, e) => 
                    {
                        if (string.IsNullOrWhiteSpace(e.NewTextValue))
                        {
                            deductionItem.Amount = 0;
                            UpdateTotals();
                            return;
                        }

                        string filteredText = new string(e.NewTextValue.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
                        
                        int decimalCount = filteredText.Count(c => c == '.' || c == ',');
                        if (decimalCount > 1)
                        {
                            filteredText = filteredText.Substring(0, filteredText.LastIndexOf('.'));
                        }

                        if (decimal.TryParse(filteredText, out decimal amount))
                        {
                            deductionItem.Amount = amount;
                            UpdateTotals();
                        }
                    };
                    
                    var deleteButton = new Button
                    {
                        Text = "×",
                        FontSize = 12,
                        BackgroundColor = Color.FromArgb("#ef4444"),
                        TextColor = Colors.White,
                        CornerRadius = 4,
                        WidthRequest = 28,
                        HeightRequest = 28
                    };
                    
                    int currentIndex = i;
                    deleteButton.Clicked += (s, e) => 
                    {
                        _deductionsItems.RemoveAt(currentIndex);
                        RefreshEarningsDeductionsDisplay();
                        UpdateTotals();
                    };
                    
                    Grid.SetColumn(deductionNameEntry, 0);
                    Grid.SetColumn(deductionAmountEntry, 1);
                    Grid.SetColumn(deleteButton, 2);
                    grid.Children.Add(deductionNameEntry);
                    grid.Children.Add(deductionAmountEntry);
                    grid.Children.Add(deleteButton);

                    DynamicDeductionsContainer.Children.Add(grid);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RefreshEarningsDeductionsDisplay: {ex}");
            }
        }

        private void UpdateTotals()
        {
            try
            {
                // Get paid days and LOP days
                int paidDays = 0;
                int totalWorkingDays = 22; // Default working days in a month
                
                if (int.TryParse(PaidDaysEntry?.Text, out int pd) && pd >= 0)
                    paidDays = pd;
                    
                if (paidDays > totalWorkingDays)
                {
                    paidDays = totalWorkingDays;
                    if (PaidDaysEntry != null)
                        PaidDaysEntry.Text = paidDays.ToString();
                }
                
                int lopDays = 0;
                if (int.TryParse(LossOfPayDaysEntry?.Text, out int lop) && lop >= 0)
                    lopDays = lop;
                
                // Calculate total earnings from predefined fields
                decimal basicEarning = 0;
                decimal hraEarning = 0;
                
                if (decimal.TryParse(BasicEarningEntry?.Text, out decimal basic) && basic >= 0)
                    basicEarning = basic;
                    
                if (decimal.TryParse(HraEarningEntry?.Text, out decimal hra) && hra >= 0)
                    hraEarning = hra;
                
                // Calculate total deductions from predefined fields
                decimal incomeTax = 0;
                decimal pfDeduction = 0;
                
                if (decimal.TryParse(IncomeTaxDeductionEntry?.Text, out decimal tax) && tax >= 0)
                    incomeTax = tax;
                    
                if (decimal.TryParse(PfDeductionEntry?.Text, out decimal pf) && pf >= 0)
                    pfDeduction = pf;
                
                // Calculate total earnings (predefined + dynamic)
                decimal totalEarnings = basicEarning + hraEarning + _earningsItems.Sum(x => x.Amount);
                
                // Calculate total deductions (predefined + dynamic)
                decimal totalDeductions = incomeTax + pfDeduction + _deductionsItems.Sum(x => x.Amount);
                
                // Calculate LOP amount (deduction for unpaid days)
                if (lopDays > 0 && basicEarning > 0 && paidDays > 0)
                {
                    decimal dailyRate = basicEarning / paidDays;
                    decimal lopAmount = Math.Round(dailyRate * lopDays, 2);
                    
                    // Add LOP as a separate deduction
                    var existingLop = _deductionsItems.FirstOrDefault(x => 
                        x.Name.Equals("Loss of Pay", StringComparison.OrdinalIgnoreCase));
                    
                    if (existingLop != null)
                    {
                        existingLop.Amount = lopAmount;
                    }
                    else if (lopAmount > 0)
                    {
                        _deductionsItems.Add(new EarningDeductionItem
                        {
                            Name = "Loss of Pay",
                            Amount = lopAmount,
                            IsReadOnly = true
                        });
                    }
                    
                    totalDeductions = incomeTax + pfDeduction + _deductionsItems.Sum(x => x.Amount);
                }
                
                decimal netPayable = totalEarnings - totalDeductions;
                
                // Update UI
                if (GrossEarningsLabel != null)
                    GrossEarningsLabel.Text = $"₹ {totalEarnings:N0}";
                    
                if (TotalDeductionsLabel != null)
                    TotalDeductionsLabel.Text = $"₹ {totalDeductions:N0}";
                    
                if (NetPayableLabel != null)
                    NetPayableLabel.Text = $"₹ {netPayable:N2}";
                
                // Convert amount to words
                if (AmountInWordsLabel != null)
                    AmountInWordsLabel.Text = ConvertAmountToWords(netPayable);
                
                // Update the display to reflect any prorated amounts
                RefreshEarningsDeductionsDisplay();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateTotals: {ex}");
            }
        }

        private string ConvertAmountToWords(decimal amount)
        {
            try
            {
                if (amount == 0) return "Zero Rupees Only";
                
                string[] ones = { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };
                string[] teens = { "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
                string[] tens = { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };
                string[] thousands = { "", "Thousand", "Lakh", "Crore" };
                
                string ConvertLessThanThousand(int number)
                {
                    if (number == 0)
                        return "";
                    
                    if (number < 10)
                        return ones[number] + " ";
                    
                    if (number < 20)
                        return teens[number - 10] + " ";
                    
                    if (number < 100)
                        return tens[number / 10] + " " + ConvertLessThanThousand(number % 10);
                    
                    return ones[number / 100] + " Hundred " + ConvertLessThanThousand(number % 100);
                }
                
                string ConvertToWords(decimal num)
                {
                    if (num == 0)
                        return "Zero";
                        
                    int rupees = (int)Math.Floor(num);
                    int paise = (int)Math.Round((num - rupees) * 100);
                    
                    if (rupees == 0)
                        return $"Zero Rupees and {paise:00} Paise Only";
                        
                    string result = "";
                    int index = 0;
                    
                    while (rupees > 0)
                    {
                        if (rupees % 1000 != 0)
                        {
                            string part = ConvertLessThanThousand(rupees % 1000).Trim();
                            if (!string.IsNullOrEmpty(part))
                                result = part + " " + thousands[index] + " " + result;
                        }
                        rupees /= 1000;
                        index++;
                    }
                    
                    result = result.Trim();
                    
                    if (paise > 0)
                        return $"{result} Rupees and {paise:00} Paise Only";
                        
                    return $"{result} Rupees Only";
                }
                
                return ConvertToWords(amount);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ConvertAmountToWords: {ex}");
                return $"Rupees {amount:N2} Only";
            }
        }

        private string GetCurrentPayPeriod()
        {
            if (MonthPicker.SelectedItem == null || YearPicker.SelectedItem == null)
                return DateTime.Now.ToString("MMMM yyyy");
                
            return $"{MonthPicker.SelectedItem} {YearPicker.SelectedItem}";
        }

        private async Task<PayslipData> GeneratePayslipData()
        {
            if (_selectedEmployee == null)
                throw new InvalidOperationException("No employee selected");

            var companyInfo = await _dataStorageService.LoadCompanyInformationAsync();
            
            // Get predefined earnings values
            decimal basicEarning = 0;
            decimal hraEarning = 0;
            
            if (decimal.TryParse(BasicEarningEntry?.Text, out decimal basic) && basic >= 0)
                basicEarning = basic;
                
            if (decimal.TryParse(HraEarningEntry?.Text, out decimal hra) && hra >= 0)
                hraEarning = hra;
            
            // Get predefined deductions values
            decimal incomeTax = 0;
            decimal pfDeduction = 0;
            
            if (decimal.TryParse(IncomeTaxDeductionEntry?.Text, out decimal tax) && tax >= 0)
                incomeTax = tax;
                
            if (decimal.TryParse(PfDeductionEntry?.Text, out decimal pf) && pf >= 0)
                pfDeduction = pf;
            
            // Combine predefined and dynamic earnings
            var allEarnings = new Dictionary<string, decimal>();
            if (basicEarning > 0) allEarnings["Basic"] = basicEarning;
            if (hraEarning > 0) allEarnings["House Rent Allowance"] = hraEarning;
            
            // Add dynamic earnings
            foreach (var item in _earningsItems)
            {
                if (item.Amount > 0)
                    allEarnings[item.Name] = item.Amount;
            }
            
            // Combine predefined and dynamic deductions
            var allDeductions = new Dictionary<string, decimal>();
            if (incomeTax > 0) allDeductions["Income Tax"] = incomeTax;
            if (pfDeduction > 0) allDeductions["Provident Fund"] = pfDeduction;
            
            // Add dynamic deductions
            foreach (var item in _deductionsItems)
            {
                if (item.Amount > 0)
                    allDeductions[item.Name] = item.Amount;
            }
            
            var payslipData = new PayslipData
            {
                CompanyName = companyInfo?.CompanyName ?? "",
                CompanyAddress = companyInfo?.Address ?? "",
                CompanyCityPincode = companyInfo?.CityPincode ?? "",
                CompanyCountry = companyInfo?.Country ?? "India",
                CompanyLogoPath = companyInfo?.LogoPath ?? "",
                EmployeeName = _selectedEmployee.EmployeeName,
                EmployeeCode = _selectedEmployee.EmployeeCode,
                Department = _selectedEmployee.Department,
                Designation = _selectedEmployee.Designation,
                BankAccount = _selectedEmployee.BankAccount,
                PanNumber = _selectedEmployee.PanNumber,
                PayPeriod = GetCurrentPayPeriod(),
                PayDate = PayDatePicker.Date,
                PaidDays = int.TryParse(PaidDaysEntry?.Text, out int paidDays) ? paidDays : 0,
                LossOfPayDays = int.TryParse(LossOfPayDaysEntry?.Text, out int lopDays) ? lopDays : 0,
                Earnings = allEarnings,
                Deductions = allDeductions
            };

            return payslipData;
        }

        private async void OnGeneratePayslipClicked(object sender, EventArgs e)
        {
            try
            {
                if (_selectedEmployee == null)
                {
                    await DisplayAlert("Error", "Please select an employee first.", "OK");
                    return;
                }

                // Save current income details before generating PDF
                await SaveCurrentIncomeDetails();

                var payslipData = await GeneratePayslipData();
                string fileName = $"Payslip_{_selectedEmployee.EmployeeName}_{GetCurrentPayPeriod().Replace(" ", "_")}.pdf";
                
                _pdfPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    fileName);
                    
                await _pdfGenerationService.GeneratePayslipAsync(payslipData);

                if (!string.IsNullOrEmpty(_pdfPath) && File.Exists(_pdfPath))
                {
                    await Launcher.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(_pdfPath)
                    });
                }
                else
                {
                    await DisplayAlert("Error", "Failed to generate PDF file. Please try again.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to generate or open PDF: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Error with PDF: {ex}");
            }
        }

        private async Task SaveCurrentIncomeDetails()
        {
            try
            {
                if (_selectedEmployee == null) return;

                var incomeDetails = new IncomeDetails
                {
                    EmployeeCode = _selectedEmployee.EmployeeCode,
                    EmployeeName = _selectedEmployee.EmployeeName,
                    PayPeriod = GetCurrentPayPeriod(),
                    PayDate = PayDatePicker.Date,
                    PaidDays = int.TryParse(PaidDaysEntry?.Text, out int paidDays) ? paidDays : 0,
                    LossOfPayDays = int.TryParse(LossOfPayDaysEntry?.Text, out int lopDays) ? lopDays : 0,
                    
                    // Predefined earnings
                    BasicEarning = decimal.TryParse(BasicEarningEntry?.Text, out decimal basic) ? basic : 0,
                    HraEarning = decimal.TryParse(HraEarningEntry?.Text, out decimal hra) ? hra : 0,
                    
                    // Predefined deductions
                    IncomeTaxDeduction = decimal.TryParse(IncomeTaxDeductionEntry?.Text, out decimal tax) ? tax : 0,
                    PfDeduction = decimal.TryParse(PfDeductionEntry?.Text, out decimal pf) ? pf : 0,
                    
                    // Dynamic items
                    DynamicEarnings = new List<EarningDeductionItem>(_earningsItems),
                    DynamicDeductions = new List<EarningDeductionItem>(_deductionsItems),
                    
                    // Calculated totals
                    GrossEarnings = decimal.TryParse(GrossEarningsLabel?.Text?.Replace("₹ ", "").Replace(",", ""), out decimal gross) ? gross : 0,
                    TotalDeductions = decimal.TryParse(TotalDeductionsLabel?.Text?.Replace("₹ ", "").Replace(",", ""), out decimal total) ? total : 0,
                    NetPayable = decimal.TryParse(NetPayableLabel?.Text?.Replace("₹ ", "").Replace(",", ""), out decimal net) ? net : 0
                };

                await _dataStorageService.SaveIncomeDetailsAsync(incomeDetails);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving income details: {ex}");
            }
        }

        private async Task LoadIncomeDetailsForEmployee(string employeeCode)
        {
            try
            {
                var incomeDetails = await _dataStorageService.GetIncomeDetailsByEmployeeCodeAsync(employeeCode);
                if (incomeDetails != null)
                {
                    // Load pay period details
                    if (PayDatePicker != null)
                        PayDatePicker.Date = incomeDetails.PayDate;
                        
                    if (PaidDaysEntry != null)
                        PaidDaysEntry.Text = incomeDetails.PaidDays.ToString();
                        
                    if (LossOfPayDaysEntry != null)
                        LossOfPayDaysEntry.Text = incomeDetails.LossOfPayDays.ToString();
                    
                    // Load predefined earnings
                    if (BasicEarningEntry != null)
                        BasicEarningEntry.Text = incomeDetails.BasicEarning.ToString();
                        
                    if (HraEarningEntry != null)
                        HraEarningEntry.Text = incomeDetails.HraEarning.ToString();
                    
                    // Load predefined deductions
                    if (IncomeTaxDeductionEntry != null)
                        IncomeTaxDeductionEntry.Text = incomeDetails.IncomeTaxDeduction.ToString();
                        
                    if (PfDeductionEntry != null)
                        PfDeductionEntry.Text = incomeDetails.PfDeduction.ToString();
                    
                    // Load dynamic items
                    _earningsItems.Clear();
                    _deductionsItems.Clear();
                    
                    foreach (var item in incomeDetails.DynamicEarnings)
                        _earningsItems.Add(item);
                        
                    foreach (var item in incomeDetails.DynamicDeductions)
                        _deductionsItems.Add(item);
                    
                    // Refresh display and update totals
                    RefreshEarningsDeductionsDisplay();
                    UpdateTotals();
                    
                    await DisplayAlert("Success", "Previous income details loaded successfully!", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading income details: {ex}");
            }
        }

        private async void OnLoadPreviousDataClicked(object sender, EventArgs e)
        {
            if (_selectedEmployee == null)
            {
                await DisplayAlert("Error", "Please select an employee first.", "OK");
                return;
            }

            await LoadIncomeDetailsForEmployee(_selectedEmployee.EmployeeCode);
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            try
            {
                // Navigate to the settings page
                await Navigation.PushAsync(new SettingsPage());
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to open settings: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Error opening settings: {ex}");
            }
        }


    }
}