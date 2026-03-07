using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Platform.Storage;
using Avalonia.Controls.Primitives;

namespace ScholarLog.Views;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
        
        if (Application.Current != null)
            _isDarkModeEnabled = Application.Current.ActualThemeVariant == ThemeVariant.Dark;
        
        this.DataContext = this;
    }
    private bool _isDarkModeEnabled;
    public bool IsDarkModeEnabled
    {
        get => _isDarkModeEnabled;
        set 
        {
            if (_isDarkModeEnabled != value)
            {
                _isDarkModeEnabled = value;
                ApplyTheme(value);
            }
        }
    }

    private void ApplyTheme(bool isDark)
    {
        var app = Application.Current;
        if (app != null)
            app.RequestedThemeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;
    }
    
    private string _pathToBDD = "/home/lukas/Documents/CloudSync/App.db";

    public string PathToBDD
    {
        get => _pathToBDD;
        set
        {
            // Normaliser le chemin (enlever les espaces inutiles)
            string cleanedPath = value?.Trim() ?? string.Empty;

            if (File.Exists(cleanedPath) || string.IsNullOrEmpty(cleanedPath))
            {
                _pathToBDD = cleanedPath;
                Console.WriteLine($"Chemin sauvegarde : {cleanedPath}");
            }
                
            else
            {
                Console.WriteLine($"Chemin non-sauvegarde : {cleanedPath}");
                // Affiche le petit message d'erreur juste au-dessus du TextBox
                FlyoutBase.ShowAttachedFlyout(userbddPath);
                _pathToBDD = cleanedPath; // On garde quand même la valeur pour permettre la correction
                
            }
            
        }
    }
    
    public async void BrowseFileCommand(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Sélectionner la base de données",
            AllowMultiple = false,
            FileTypeFilter = new[] 
            { 
                new FilePickerFileType("SQLite") { Patterns = new[] { "*.db", "*.sqlite" } } 
            }
        });

        if (files != null && files.Count > 0)
            PathToBDD = files[0].Path.LocalPath;
        
        Console.WriteLine(PathToBDD);
        userbddPath.Text = PathToBDD;
    }

    
    private void Button_SavePathBDD(object? sender, RoutedEventArgs e)
    {
        string path = userbddPath.Text ?? string.Empty;
        
        PathToBDD = path;
    }
}