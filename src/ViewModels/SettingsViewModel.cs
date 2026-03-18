using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using Avalonia;
using Avalonia.Styling;

namespace ScholarLog.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    // etat

    [ObservableProperty]
    private string _pathToBDD = "/home/lukas/Documents/CloudSync/App.db";

    [ObservableProperty]
    private bool _isDarkModeEnabled;

    // Déclencheur pour dire à la vue d'afficher le message d'erreur
    [ObservableProperty]
    private bool _showPathError = false;

    public SettingsViewModel()
    {
        // Initialiser avec le thème actuel au démarrage
        if (Application.Current != null)
        {
            IsDarkModeEnabled = Application.Current.ActualThemeVariant == ThemeVariant.Dark;
        }
    }

    // Cette méthode magique est appelée automatiquement par le Toolkit
    // quand IsDarkModeEnabled change !
    partial void OnIsDarkModeEnabledChanged(bool value)
    {
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = value ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }

    // commandes

    [RelayCommand]
    private void SavePath()
    {
        string cleanedPath = PathToBDD?.Trim() ?? string.Empty;

        if (File.Exists(cleanedPath) || string.IsNullOrEmpty(cleanedPath))
        {
            PathToBDD = cleanedPath;
            ShowPathError = false;
            System.Console.WriteLine($"Chemin sauvegardé : {cleanedPath}");
        }
        else
        {
            System.Console.WriteLine($"Chemin invalide : {cleanedPath}");
            // On lève le drapeau, la Vue s'occupera d'afficher le Flyout
            ShowPathError = true; 
        }
    }
}