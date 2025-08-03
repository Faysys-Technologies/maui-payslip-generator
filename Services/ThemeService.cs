using Microsoft.Maui.Controls;

namespace faysys_payslip_generator.Services
{
    public interface IThemeService
    {
        bool IsDarkMode { get; }
        Task<bool> GetThemeAsync();
        Task SetThemeAsync(bool isDarkMode);
        void ApplyTheme(bool isDarkMode);
    }

    public class ThemeService : IThemeService
    {
        private const string ThemeKey = "app_theme";
        private bool _isDarkMode;

        public bool IsDarkMode => _isDarkMode;

        public async Task<bool> GetThemeAsync()
        {
            try
            {
                var savedTheme = await SecureStorage.GetAsync(ThemeKey);
                if (bool.TryParse(savedTheme, out bool isDark))
                {
                    _isDarkMode = isDark;
                    return _isDarkMode;
                }
            }
            catch (Exception)
            {
                // If there's an error reading from secure storage, fall back to system theme
            }

            // Default to system theme
            _isDarkMode = Application.Current?.RequestedTheme == AppTheme.Dark;
            return _isDarkMode;
        }

        public async Task SetThemeAsync(bool isDarkMode)
        {
            _isDarkMode = isDarkMode;
            
            try
            {
                await SecureStorage.SetAsync(ThemeKey, isDarkMode.ToString());
            }
            catch (Exception)
            {
                // Handle storage error silently
            }

            ApplyTheme(isDarkMode);
        }

        public void ApplyTheme(bool isDarkMode)
        {
            if (Application.Current != null)
            {
                Application.Current.UserAppTheme = isDarkMode ? AppTheme.Dark : AppTheme.Light;
                
                // Update resource dictionary colors
                var resources = Application.Current.Resources;
                
                if (isDarkMode)
                {
                    // Apply professional dark theme colors - softer and more elegant
                    resources["BackgroundPrimary"] = Color.FromArgb("#1e293b");      // Softer dark blue-gray
                    resources["BackgroundSecondary"] = Color.FromArgb("#334155");    // Medium blue-gray
                    resources["BackgroundTertiary"] = Color.FromArgb("#475569");     // Lighter blue-gray
                    resources["BackgroundCard"] = Color.FromArgb("#1e293b");         // Same as primary for cards
                    resources["Surface"] = Color.FromArgb("#1e293b");                // Consistent surface
                    resources["SurfaceVariant"] = Color.FromArgb("#334155");         // Slightly lighter surface
                    
                    resources["TextPrimary"] = Color.FromArgb("#f1f5f9");            // Soft white
                    resources["TextSecondary"] = Color.FromArgb("#cbd5e1");          // Light gray
                    resources["TextTertiary"] = Color.FromArgb("#94a3b8");           // Medium gray
                    
                    // Professional dark theme gray palette
                    resources["Gray50"] = Color.FromArgb("#0f172a");                 // Darkest
                    resources["Gray100"] = Color.FromArgb("#1e293b");                // Very dark
                    resources["Gray200"] = Color.FromArgb("#334155");                // Dark
                    resources["Gray300"] = Color.FromArgb("#475569");                // Medium dark
                    resources["Gray400"] = Color.FromArgb("#64748b");                // Medium
                    resources["Gray500"] = Color.FromArgb("#94a3b8");                // Medium light
                    resources["Gray600"] = Color.FromArgb("#cbd5e1");                // Light
                    resources["Gray700"] = Color.FromArgb("#e2e8f0");                // Very light
                    resources["Gray800"] = Color.FromArgb("#f1f5f9");                // Almost white
                    resources["Gray900"] = Color.FromArgb("#f8fafc");                // Near white
                    resources["Gray950"] = Color.FromArgb("#ffffff");                // Pure white
                }
                else
                {
                    // Apply light theme colors
                    resources["BackgroundPrimary"] = Color.FromArgb("#ffffff");
                    resources["BackgroundSecondary"] = Color.FromArgb("#f8fafc");
                    resources["BackgroundTertiary"] = Color.FromArgb("#f1f5f9");
                    resources["BackgroundCard"] = Color.FromArgb("#ffffff");
                    resources["Surface"] = Color.FromArgb("#ffffff");
                    resources["SurfaceVariant"] = Color.FromArgb("#f8fafc");
                    
                    resources["TextPrimary"] = Color.FromArgb("#111827");
                    resources["TextSecondary"] = Color.FromArgb("#6b7280");
                    resources["TextTertiary"] = Color.FromArgb("#9ca3af");
                    
                    resources["Gray50"] = Color.FromArgb("#f9fafb");
                    resources["Gray100"] = Color.FromArgb("#f3f4f6");
                    resources["Gray200"] = Color.FromArgb("#e5e7eb");
                    resources["Gray300"] = Color.FromArgb("#d1d5db");
                    resources["Gray400"] = Color.FromArgb("#9ca3af");
                    resources["Gray500"] = Color.FromArgb("#6b7280");
                    resources["Gray600"] = Color.FromArgb("#4b5563");
                    resources["Gray700"] = Color.FromArgb("#374151");
                    resources["Gray800"] = Color.FromArgb("#1f2937");
                    resources["Gray900"] = Color.FromArgb("#111827");
                    resources["Gray950"] = Color.FromArgb("#030712");
                }
            }
        }
    }
}