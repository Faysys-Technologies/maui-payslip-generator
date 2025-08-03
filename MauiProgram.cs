using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using UraniumUI;
using faysys_payslip_generator.Services;
using faysys_payslip_generator.Views;

namespace faysys_payslip_generator
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseUraniumUI()
                .UseUraniumUIMaterial()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register services
            builder.Services.AddSingleton<IPdfGenerationService, PdfGenerationService>();
            builder.Services.AddSingleton<IDataStorageService, DataStorageService>();
            builder.Services.AddSingleton<IThemeService, ThemeService>();
            builder.Services.AddTransient<MainPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}