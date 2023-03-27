using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Maui;
using Microsoft.Extensions.Logging;

namespace ViewAssociationsMaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            }).UseArcGISRuntime();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static MauiAppBuilder UseArcGISRuntime(this MauiAppBuilder builder)
    {
        builder.UseArcGISRuntime(config =>
        {
            config.UseLicense("<set license key>");
        });
        return builder;
    }
}
