/*
    Fichier      :  SettingsViewModel.cs
    Projet       :  ScholarLog

    Description  :
        ViewModel gérant les paramètres de l'application.
        Contrôle la sélection et la sauvegarde du chemin de la base de données 
        SQLite locale ainsi que la bascule du thème (Clair/Sombre).

    Auteur       :  Lukas Hofer - TINF2
    Date         :  19.03.2026

    Remarques    :
        - Utilise OnIsDarkModeEnabledChanged pour appliquer dynamiquement le thème à l'application.
        - Communique les erreurs de validation de chemin à la vue via la propriété ShowPathError.
*/


using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using Avalonia;
using Avalonia.Styling;

namespace ScholarLog.ViewModels;


public partial class SettingsViewModel : ViewModelBase
{
    // état
    [ObservableProperty]
    private string _pathToBDD = "/home/lukas/Documents/CloudSync/App.db";

    [ObservableProperty]
    private bool _isDarkModeEnabled;

    // déclencheur pour dire à la vue d'afficher le message d'erreur
    [ObservableProperty]
    private bool _showPathError = false;

    public SettingsViewModel()
    {
        // initialiser avec le thème actuel au démarrage
        if (Application.Current != null)
        {
            IsDarkModeEnabled = Application.Current.ActualThemeVariant == ThemeVariant.Dark;
        }
    }

    // Cette méthode magique est appelée automatiquement par le Toolkit
    // quand IsDarkModeEnabled change
    partial void OnIsDarkModeEnabledChanged(bool value)
    {
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = value ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }
    

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