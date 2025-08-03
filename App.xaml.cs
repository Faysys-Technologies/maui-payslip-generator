using faysys_payslip_generator.Services;

namespace faysys_payslip_generator
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            InitializeTheme();
            MainPage = new AppShell();
        }

        private async void InitializeTheme()
        {
            try
            {
                var themeService = ServiceHelper.GetService<IThemeService>();
                if (themeService != null)
                {
                    var isDarkMode = await themeService.GetThemeAsync();
                    themeService.ApplyTheme(isDarkMode);
                }
            }
            catch (Exception)
            {
                // Fallback to system theme
                UserAppTheme = RequestedTheme;
            }
        }
    }
}
