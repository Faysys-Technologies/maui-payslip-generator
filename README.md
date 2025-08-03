# Faysys Payslip Generator

A comprehensive desktop payslip generator built with .NET MAUI, featuring company information management, employee data management, and professional PDF payslip generation.

## 🚀 Features

### Company Management
- **Company Information Setup**: Store and manage company details including name, address, city/pincode, and country
- **Logo Management**: Upload and integrate company logos into payslips
- **Company Data Persistence**: Automatically saves company information for future use

### Employee Management
- **Employee Database**: Complete employee information management system
- **Employee Search**: Quick search functionality by employee code
- **Custom Fields**: Add dynamic custom fields for additional employee data
- **Employee Dropdown**: Easy employee selection from existing database
- **Data Validation**: Comprehensive validation for employee information

### Payslip Generation
- **Professional PDF Output**: Generate high-quality PDF payslips with company branding
- **Dynamic Earnings & Deductions**: Add custom earning and deduction items
- **Predefined Components**: Built-in support for common salary components:
  - Basic Salary
  - House Rent Allowance (HRA)
  - Income Tax
  - Provident Fund (PF)
  - Professional Tax
  - Conveyance Allowance
- **Pay Period Management**: Month/year selection for payslip generation
- **Automatic Calculations**: Real-time calculation of gross earnings, total deductions, and net pay
- **Loss of Pay (LOP) Calculation**: Automatic calculation of salary deductions for unpaid days
- **Data Persistence**: Save and load previous income details for employees

### User Interface
- **Modern UI Design**: Clean and professional interface with light/dark theme support
- **Tabbed Navigation**: Organized workflow across Company Info, Employee Info, and Payslip Generation
- **Responsive Layout**: Optimized for desktop usage
- **Real-time Updates**: Instant calculation updates as you input data
- **Visual Feedback**: Professional styling with shadows, rounded corners, and smooth animations

## 🛠️ Technology Stack

- **.NET 8**: Latest .NET framework for cross-platform development
- **.NET MAUI**: Multi-platform App UI framework for desktop applications
- **PdfSharpCore**: PDF generation library for creating professional payslips
- **CommunityToolkit.Maui**: Enhanced MAUI controls and utilities
- **UraniumUI**: Modern UI components and styling
- **Data Storage**: Local data persistence for company and employee information

## 📋 Prerequisites

- **Visual Studio Community 2022** (or higher)
- **.NET 8 SDK 8.0.412** (or higher)
- **MAUI Development Workload** installed in Visual Studio

## 🚀 Getting Started

### Installation

1. **Clone the Repository**
   ```bash
   git clone https://github.com/Faysys-Technologies/maui-payslip-generator.git
   cd maui-payslip-generator
   ```

2. **Open in Visual Studio**
   - Open `faysys-payslip-generator.sln` in Visual Studio Community 2022
   - Ensure MAUI workload is installed

3. **Restore NuGet Packages**
   ```bash
   dotnet restore
   ```

4. **Build and Run**
   - Set target framework to `net8.0-windows10.0.19041.0`
   - Build and run the application (F5)

## 📖 Usage Guide

### Initial Setup

1. **Company Information**
   - Navigate to the "Company Info" tab
   - Enter your company details (name, address, city/pincode)
   - Upload company logo (optional)
   - Save the information

2. **Employee Management**
   - Go to "Employee Info" tab
   - Add new employees with their details:
     - Employee Code (unique identifier)
     - Employee Name
     - Department and Designation
     - Bank Account Details
     - PAN Number
   - Add custom fields as needed
   - Save employee information

### Generating Payslips

1. **Select Employee**
   - Navigate to "Payslip Generation" tab
   - Use employee dropdown or enter employee code
   - System will load employee details automatically

2. **Configure Pay Period**
   - Select month and year
   - Set pay date
   - Enter paid days and loss of pay days

3. **Add Earnings**
   - Enter basic salary and HRA
   - Add custom earnings using "+" button
   - System calculates gross earnings automatically

4. **Add Deductions**
   - Enter income tax and PF deductions
   - Add custom deductions as needed
   - System calculates total deductions

5. **Generate PDF**
   - Review calculated net pay
   - Click "Generate Payslip PDF"
   - PDF opens automatically upon generation

### Data Management

- **Save Previous Data**: Use "Load Previous Data" to retrieve last saved payslip details for an employee
- **Automatic Saving**: Income details are automatically saved when generating payslips
- **Data Persistence**: All company and employee data is stored locally

## 🏗️ Project Structure

```
├── Models/
│   ├── CompanyInformation.cs      # Company data model
│   ├── EmployeeInformation.cs     # Employee data model
│   ├── PayslipData.cs            # Payslip data structure
│   ├── IncomeDetails.cs          # Income/salary details model
│   └── EarningDeductionItem.cs   # Dynamic earnings/deductions
├── Services/
│   ├── IPdfGenerationService.cs  # PDF generation interface
│   ├── PdfGenerationService.cs   # PDF generation implementation
│   ├── DataStorageService.cs     # Data persistence service
│   └── ThemeService.cs           # Theme management
├── Views/
│   ├── CompanyInfoTab.xaml(.cs)  # Company information UI
│   ├── EmployeeInfoTab.xaml(.cs) # Employee management UI
│   ├── SettingsPage.xaml(.cs)    # Application settings
│   └── MainPage.xaml(.cs)        # Main application interface
├── Converters/
│   └── IntToBoolConverter.cs     # UI data converters
├── Resources/
│   ├── Images/                   # Application images and icons
│   ├── Fonts/                    # Custom fonts
│   └── Styles/                   # UI styling and themes
└── Platforms/                    # Platform-specific configurations
```

## 🎨 Key Features in Detail

### PDF Generation
- Professional payslip layout with company branding
- Automatic formatting and styling
- Support for company logos
- Dynamic content based on employee and payslip data
- High-quality PDF output suitable for official use

### Data Management
- Local SQLite database for data persistence
- Automatic data validation and error handling
- Support for multiple employees and companies
- Historical payslip data storage

### User Experience
- Intuitive tabbed interface
- Real-time calculations and updates
- Comprehensive error handling and user feedback
- Professional UI design with modern styling
- Support for light and dark themes

## 🔧 Configuration

The application uses local data storage and doesn't require external configuration. All settings are managed through the UI.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🏢 About Faysys Technologies

Developed by Faysys Technologies - providing innovative software solutions for business automation and management.

## 📞 Support

For support and questions, please open an issue in the GitHub repository or contact our development team.
