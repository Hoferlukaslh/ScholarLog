using System;
using Avalonia.Styling;
using ScholarLog.Data;

namespace ScholarLog;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using ScholarLog.ViewModels;
using ScholarLog.Views;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        /*
#if DEBUG
        this.AttachDeveloperTools();
#endif*/
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var savedTheme = AppSettingsService.Instance.Current.IsDarkMode;
        RequestedThemeVariant = savedTheme ? ThemeVariant.Dark : ThemeVariant.Light;
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };

            desktop.Exit += (sender, e) =>
            {
                try
                {
                    using var repo = new DataRepository();
                    repo.OptimiserBaseDeDonneesAsync().GetAwaiter().GetResult();
                    Console.WriteLine("VACUUM exécuté à la fermeture.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"VACUUM ignoré : {ex.Message}");
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
    
   

}