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




namespace ScholarLog.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Threading;

using Avalonia.Animation;
using Avalonia.Animation.Easings;

using CommunityToolkit.Mvvm;
using System;
using ScholarLog.Pages;
using ScholarLog.Data;


public partial class MainWindow : Window
{
    
    
    private DispatcherTimer? _lightTimer;
    private readonly Random _random = new Random();
    
    // Garder les pages en mémoire
    private HomePage? _homePage;
    private JournalPage? _journalPage;
    private NotesPage? _notesPage;
    private SettingsPage? _settingsPage;
    
   

    public MainWindow()
    {
        InitializeComponent();
        this.Loaded += MainWindow_Loaded;
        
        // 1. Initialiser la HomePage tout de suite
        _homePage = new HomePage();
        
        // 2. S'abonner à l'événement de navigation
        _homePage.NavigationVersJournalDemandee += (sender, moduleASelectionner) => 
        {
            AllerAuJournalAvecModule(moduleASelectionner);
        };
    }
    
    
    
    private void AllerAuJournalAvecModule(ModuleViewModel module)
    {
        _journalPage ??= new JournalPage();
        
        // On définit le module sélectionné dans la page journal
        _journalPage.SelectedModule = module;
        
        // On met à jour l'UI de la barre latérale (curseur)
        MettreAJourSelection(Buttonjournaux);
        
        // On affiche la page
        MainContentControler.Content = _journalPage;
    }
    
    

    private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        MettreAJourSelection(ButtonAccueil);
        
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

        // lance le premier mouvement immédiatement
        MoveLights();
        
        await AppDataService.Instance.ChargerDonneesGlobalesAsync();
        
        MainContentControler.Content = _homePage;
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
    
    private void MettreAJourSelection(Button boutonSelectionne)
    {
        if (boutonSelectionne == null || NavCursor == null || Sidebar == null) return;

        // récupère les coordonnées du bouton par rapport au conteneur complet (Sidebar)
        var pointRelatif = boutonSelectionne.TranslatePoint(new Point(0, 0), Sidebar);
    
        if (pointRelatif.HasValue)
        {
            double yPos = pointRelatif.Value.Y;
        
            // centre le curseur (24px de haut) verticalement par rapport au bouton
            // Si la hauteur n'est pas encore calculée au démarrage, on met une valeur par défaut (ex: 36px/2 - 24px/2 = 6)
            double decalage = boutonSelectionne.Bounds.Height > 0 
                ? (boutonSelectionne.Bounds.Height - NavCursor.Height) / 2 
                : 6; 
            
            Canvas.SetTop(NavCursor, yPos + decalage); // déplacement -> geré par axaml
        }
    }
    
    private void ButtonAccueil_OnClick(object? sender, RoutedEventArgs e)
    {
        _homePage ??= new HomePage(); 
        
        MettreAJourSelection(ButtonAccueil);

        if (MainContentControler?.Content?.Equals(_homePage) == false)
        {
            _homePage.SelectedModule = null;
            MainContentControler.Content = _homePage;
        }
    }
    
    
    
    

    private void ButtonNotes_OnClick(object? sender, RoutedEventArgs e)
    {
        _notesPage ??= new NotesPage();
        MettreAJourSelection(ButtonNotes);
    
        if (MainContentControler?.Content?.Equals(_notesPage) == false)
            MainContentControler.Content = _notesPage;
    }
    
    
    

    private void Buttonjournaux_OnClick(object? sender, RoutedEventArgs e)
    {
        _journalPage ??= new JournalPage();
        MettreAJourSelection(Buttonjournaux);
    
        if (MainContentControler?.Content?.Equals(_journalPage) == false)
            MainContentControler.Content = _journalPage;
    }
    

    private void Setting_OnClick(object? sender, RoutedEventArgs e)
    {
        _settingsPage ??= new SettingsPage();
        MettreAJourSelection(Setting);
    
        if (MainContentControler?.Content?.Equals(_settingsPage) == false)
            MainContentControler.Content = _settingsPage;
    }
}

/// <summary>
/// Animation de transition entre les pages
/// </summary>
public class MyPageSlide : PageSlide
{
    private Easing _easing = new LinearEasing();

    public Easing Easing 
    { 
        set 
        { 
            _easing = value; 
            SlideInEasing = value; 
            SlideOutEasing = value; 
        } 
    }

    public MyPageSlide()
    {
        SlideInEasing = _easing;
        SlideOutEasing = _easing;
    }
}