/*
    Fichier      :  AppSettingsService.cs
    Projet       :  ScholarLog

    Description  :
        Gère la persistance des préférences utilisateur dans un fichier JSON
        situé dans le répertoire de configuration propre à chaque OS :
            - Windows : %APPDATA%\ScholarLog\settings.json
            - macOS   : ~/Library/Application Support/ScholarLog/settings.json
            - Linux   : ~/.config/ScholarLog/settings.json
*/

using System;
using System.IO;
using System.Text.Json;

namespace ScholarLog.Data;

public class AppSettingsService
{
    // Singleton
    private static AppSettingsService? _instance;
    
    private static readonly object _lock = new();

    public static AppSettingsService Instance
    {
        get
        {
            if (_instance == null)
                lock (_lock) { _instance ??= new AppSettingsService(); }
            return _instance;
        }
    }
    

    /// <summary> Settings chargés en mémoire. </summary>
    public AppSettings Current { get; private set; } = new();

    /// <summary>
    /// Dossier de config spécifique à l'OS.
    /// Retourne le même chemin peu importe la plateforme.
    /// </summary>
    public static string ConfigDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ScholarLog"
        );

    private static string SettingsFilePath =>
        Path.Combine(ConfigDirectory, "settings.json");

    /// <summary>
    /// Chemin effectif de la BDD.
    /// Si le chemin sauvegardé est invalide, crée la BDD à côté de l'exécutable
    /// et met à jour le fichier de settings automatiquement.
    /// </summary>
    public string EffectiveDatabasePath
    {
        get
        {
            // Chemin sauvegardé valide → on l'utilise directement
            if (!string.IsNullOrWhiteSpace(Current.DatabasePath) && File.Exists(Current.DatabasePath))
                return Current.DatabasePath;

            // Fallback : à côté de l'exécutable
            string cheminParDefaut = Path.Combine(ConfigDirectory, "BDD.db");

            // Si le chemin sauvegardé était différent (BDD déplacée ou settings corrompus),
            // on met à jour et on persiste pour ne pas recréer à chaque lancement
            if (Current.DatabasePath != cheminParDefaut)
            {
                Console.WriteLine($"[Settings] BDD introuvable à '{Current.DatabasePath}'. " +
                                  $"Réinitialisation vers : {cheminParDefaut}");
                Current.DatabasePath = cheminParDefaut;
                Save();
            }

            return cheminParDefaut;
        }
    }

    private AppSettingsService()
    {
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                Current = JsonSerializer.Deserialize(json, AppSettingsJsonContext.Default.AppSettings) 
                          ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Settings] Impossible de lire les settings : {ex.Message}");
            Current = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDirectory); // crée le dossier si absent
            var json = JsonSerializer.Serialize(Current, AppSettingsJsonContext.Default.AppSettings);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Settings] Impossible de sauvegarder les settings : {ex.Message}");
        }
    }

    /// <summary>
    /// Valide un chemin avant de l'accepter (fichier existant et extension correcte).
    /// </summary>
    public static bool IsValidDatabasePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (!File.Exists(path)) return false;

        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".db" or ".sqlite" or ".sqlite3";
    }
    
    
}


public class AppSettings
{
    /// <summary>
    /// Chemin absolu vers la base de données SQLite choisie par l'utilisateur.
    /// Null = utiliser le chemin par défaut dans AppData.
    /// </summary>
    public string? DatabasePath { get; set; }
    
    /// <summary> Désactive les effets visuels énergivores (lumières animées, flou). </summary>
    public bool DisableEffects { get; set; } = false;
    
    public bool IsDarkMode { get; set; } = true; // dark par défaut
}