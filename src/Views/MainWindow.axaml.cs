/*
    Fichier      :  MainWindow.axaml.cs
    Projet       :  ScholarLog

    Description  : 
        Code-behind de la fenêtre principale. Contient :
            - Gestion du déplacement aléatoire des lumières ambiantes
            - Gestion de la barre latérale (ouverture / fermeture)
            - Gestion des boutons de navigation et chargement des pages
            - Adaptation spécifique à Linux pour les effets de flou

    Auteur       :  Lukas Hofer - TINF2
    Date         :  10.03.2026

    Remarques    :
        - Les lumières sont déplacées toutes les 7,5 secondes via DispatcherTimer.
        - Les transitions de largeur et d'opacité de la barre latérale sont gérées manuellement.
        - Le contenu principal est mis à jour via MainContentControler.
        - Classes "linux" et "rotated" utilisées pour ajuster le style.
*/

using CommunityToolkit.Mvvm.ComponentModel;

namespace ScholarLog.Views;

using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CommunityToolkit.Mvvm;
using System;
using ScholarLog.Pages;
using ScholarLog.Data;


public partial class MainWindow : Window
{
    private DispatcherTimer? _lightTimer;
    private readonly Random _random = new Random();
    
   

    public MainWindow()
    {
        InitializeComponent();
        this.Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        // Les gestionnaire de fenêtres linux gère mal le flou -> opacité ++
        if (OperatingSystem.IsLinux()) 
            this.Classes.Add("linux");
        
        // Le timer s'active toutes les 7.5 secondes
        _lightTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(7.5)
        };
        _lightTimer.Tick += (s, ev) => MoveLights();
        _lightTimer.Start();

        // On lance le premier mouvement immédiatement
        MoveLights();
        
        await AppDataService.Instance.ChargerDonneesGlobalesAsync();
        MainContentControler.Content = new HomePage();
    }

    private void MoveLights()
    {
        // On récupère la taille. Si elle est de 0 (fenêtre en train de charger), on donne une valeur par défaut
        double windowWidth = this.Bounds.Width > 0 ? this.Bounds.Width : 1280;
        double windowHeight = this.Bounds.Height > 0 ? this.Bounds.Height : 720;

        void SetRandomPosition(Ellipse light)
        {
            // Calcule une nouvelle position aléatoire
            double newX = _random.NextDouble() * windowWidth - (light.Width / 2);
            double newY = _random.NextDouble() * windowHeight - (light.Height / 2);

            Canvas.SetLeft(light, newX);
            Canvas.SetTop(light, newY);
        }

        if (Light1 != null) SetRandomPosition(Light1);
        if (Light2 != null) SetRandomPosition(Light2);
    }
    
    private void ToggleSidebar_Click(object? sender, RoutedEventArgs e)
    {
        var sidebar = this.FindControl<Grid>("Sidebar");
        var toggleIcon = this.FindControl<Label>("ToggleIcon");

        if (sidebar == null || toggleIcon == null) return;

        if (sidebar.Width <= 60)
        {
            sidebar.Width = 212;
            toggleIcon.Classes.Remove("rotated");
            SetMenuTextOpacity(1);
        }
        else 
        {
            sidebar.Width = 60;
            if (!toggleIcon.Classes.Contains("rotated")) 
                toggleIcon.Classes.Add("rotated");
            
            SetMenuTextOpacity(0);
        }
    }

    private void SetMenuTextOpacity(double opacity)
    {
        string[] controlNames = { 
            "TextLogo", 
            "TextAccueil", 
            "TextNotes", 
            "TextJournaux", 
            "TextCollapse", 
            "TextSettings" 
        };

        foreach (var name in controlNames)
        {
            var control = this.FindControl<Control>(name);
            if (control != null)
            {
                control.Opacity = opacity;
            }
        }
    }

    private void Setting_OnClick(object? sender, RoutedEventArgs e)
    {
        MainContentControler.Content = new SettingsPage();
    }

    private void Buttonjournaux_OnClick(object? sender, RoutedEventArgs e)
    {
        MainContentControler.Content = new JournalPage();
    }

    private void ButtonNotes_OnClick(object? sender, RoutedEventArgs e)
    {
        MainContentControler.Content = new NotesPage();
    }

    private void ButtonAccueil_OnClick(object? sender, RoutedEventArgs e)
    {
        MainContentControler.Content = new HomePage();
    }

    private async void ButtonExemple_OnClick(object? sender, RoutedEventArgs e)
    {
            MainContentControler.Content = new ExemplePage();
    }
}
