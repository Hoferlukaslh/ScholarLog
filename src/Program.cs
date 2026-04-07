namespace ScholarLog;

using Avalonia;
using System;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            

            // Gestion stricte du moteur de rendu (Skia) pour la RAM / VRAM
            .With(new SkiaOptions
            {
                // Limite le cache GPU (VRAM) à 256 Mo. Évite l'étouffement des machines modestes.
                MaxGpuResourceSizeBytes = 256 * 1024 * 1024
            });

        // Les logs coûtent de la RAM (allocation de chaînes). À exclure en production.
#if DEBUG
        builder.LogToTrace(Avalonia.Logging.LogEventLevel.Warning);
#endif

        return builder;
    }
}