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
            
            .With(new SkiaOptions
            {
                MaxGpuResourceSizeBytes = 256 * 1024 * 1024
            });

        // Les logs ne sont compilés et exécutés qu'en environnement de développement
        // builder.LogToTrace(Avalonia.Logging.LogEventLevel.Warning);


        return builder;
    }

}