/*
    Fichier      :  HomePage.axaml.cs
    Projet       :  ScholarLog

    Description  :
        Code-behind de la vue HomePage.
        Initialise la collection de modules affichés dans la page
        principale en récupérant les données depuis la base SQLite de manière asynchrone.

    Auteur       :  Lukas Hofer - TINF2
    Date         :  14.03.2026
*/

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel; 
using System.Threading.Tasks; 

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity; 

using ScholarLog.Data;
using ScholarLog.Components.DonutDiagram;

namespace ScholarLog.Pages;

public partial class HomePage : UserControl
{
    public static readonly StyledProperty<ModuleViewModel?> SelectedModuleProperty =
        AvaloniaProperty.Register<HomePage, ModuleViewModel?>(nameof(SelectedModule));
    
    public static readonly StyledProperty<ModuleViewModel?> DisplayedModuleProperty =
        AvaloniaProperty.Register<HomePage, ModuleViewModel?>(nameof(DisplayedModule));
    public ModuleViewModel? DisplayedModule
    {
        get => GetValue(DisplayedModuleProperty);
        set => SetValue(DisplayedModuleProperty, value);
    }
    
    // Événement que la vue principale (MainWindow) pourra écouter
    public event EventHandler<ModuleViewModel>? NavigationVersJournalDemandee;

    private void BoutonAllerAuxJournaux_Click(object? sender, RoutedEventArgs e)
    {
        // un module est bien affiché avant de lancer la navigation
        if (DisplayedModule != null)
        {
            NavigationVersJournalDemandee?.Invoke(this, DisplayedModule);
        }
    }
    
    public HomePage()
    {
        InitializeComponent();
        DataContext = this; 
        
        this.Loaded += HomePage_Loaded;
    }
    
    private async void HomePage_Loaded(object? sender, RoutedEventArgs e)
    {
        this.Loaded -= HomePage_Loaded; 
    }
    
    public ModuleViewModel? SelectedModule
    {
        get => GetValue(SelectedModuleProperty);
        set => SetValue(SelectedModuleProperty, value);
    }
    
    
    public ObservableCollection<ModuleViewModel> Modules { get; set; } = Data.AppDataService.Instance.Modules;
    public ObservableCollection<BrancheViewModel> BranchesTM { get; set; } = new ObservableCollection<BrancheViewModel>();
    public ObservableCollection<BrancheViewModel> BranchesM { get; set; } = new ObservableCollection<BrancheViewModel>();
    public ObservableCollection<TypeTravailViewModel> TypesTravail { get; set; } = new ObservableCollection<TypeTravailViewModel>();
    public ObservableCollection<Entree> Journal { get; set; } = new ObservableCollection<Entree>();
    
    
    public ObservableCollection<DonutItem> GraphiqueDonnees { get; set; } = new ObservableCollection<DonutItem>();
    
    // Clique sur un model de module (carte)
    private void OnModuleSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (SelectedModule == null) return; 

        // nettoyage 
        BranchesTM.Clear();
        BranchesM.Clear();
        Journal.Clear();
        TypesTravail.Clear();
        GraphiqueDonnees.Clear();
        
        var typestravail = new List<TypeTravailViewModel>();
        
        // parcours de chaque entrée du journal
        foreach (var entree in SelectedModule.JournalDeTravail)
        {
            bool existeDeja = false;

            // Recherche manuelle par identifiant unique
            for (int i = 0; i < typestravail.Count; i++)
            {
                if (typestravail[i].Id == entree.Type.Id) 
                    existeDeja = true;
            }

            // insertion uniquement si l'élément est absent
            if (!existeDeja)
            {
                TypeTravailViewModel wm = new TypeTravailViewModel
                {
                    Id = entree.Type.Id,
                    Nom = entree.Type.Nom,
                    ModuleId = entree.Type.ModuleId,
                    Somme = 0.00
                };
        
                typestravail.Add(wm);
            }
        }
        

        var journalTrie = SelectedModule.JournalDeTravail
            .OrderByDescending(entree => entree.Date)
            .ToList();

       
        foreach (var entree in journalTrie)
        {
            Journal.Add(entree);
            
            for (int i = 0; i < typestravail.Count; i++)
            {
                if (entree.TypeTravailId == typestravail[i].Id)
                    typestravail[i].Somme += entree.Duree;
            }
        }

        foreach (var type in typestravail)
            TypesTravail.Add(type);
        
       
        foreach (var type in TypesTravail)
        {
            GraphiqueDonnees.Add(new DonutItem 
            { 
                Label = type.Nom, 
                Value = type.Somme 
            });
        }



        // dispatching
        foreach (var branche in SelectedModule.Branches)
        {
            double moy = branche.CalculerMoyenne();
        
            var vm = new BrancheViewModel
            {
                Nom = branche.Nom,
                Moyenne = Math.Round(moy, 1),
                Type = branche.Type,
                Notes = branche.Notes.ToList(),
                BrancheTrend = Data.AppDataService.Instance.DeterminerTendance(new List<Branche> { branche }, moy)
            };

            // ajout dans la bonne collection selon le type
            switch (branche.Type)
            {
                case TypeCours.TM:
                    BranchesTM.Add(vm);
                    break;
                
                case TypeCours.M:
                    BranchesM.Add(vm);
                    break;
            }
        }
    }


   // --- 1. Ajout des propriétés animables par XAML ---
public static readonly StyledProperty<double> RightPanelWidthStarProperty =
    AvaloniaProperty.Register<HomePage, double>(nameof(RightPanelWidthStar), 0.0);

public static readonly StyledProperty<double> BottomPanelHeightStarProperty =
    AvaloniaProperty.Register<HomePage, double>(nameof(BottomPanelHeightStar), 0.0);

public double RightPanelWidthStar
{
    get => GetValue(RightPanelWidthStarProperty);
    set => SetValue(RightPanelWidthStarProperty, value);
}

public double BottomPanelHeightStar
{
    get => GetValue(BottomPanelHeightStarProperty);
    set => SetValue(BottomPanelHeightStarProperty, value);
}

// --- 2. Mise à jour de OnPropertyChanged ---
protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
{
    base.OnPropertyChanged(change);

    if (change.Property == SelectedModuleProperty)
    {
        var oldVal = change.GetOldValue<ModuleViewModel?>();
        var newVal = change.GetNewValue<ModuleViewModel?>();

        if (newVal != null) DisplayedModule = newVal;

        if (oldVal == null && newVal != null)       
        {
            // Déclenche l'animation XAML vers l'ouverture
            RightPanelWidthStar = 3.0;
            BottomPanelHeightStar = 40.0;
        }
        else if (oldVal != null && newVal == null)  
        {
            // Déclenche l'animation XAML vers la fermeture
            RightPanelWidthStar = 0.0;
            BottomPanelHeightStar = 0.0;
            
            // On attend la fin de l'animation (350ms) pour vider la vue, 
            // tout en vérifiant que l'utilisateur n'a pas cliqué sur un autre module entre temps
            Task.Delay(350).ContinueWith(_ => 
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => 
                {
                    if (SelectedModule == null) DisplayedModule = null;
                }));
        }
    }
    // --- 3. Appliquer les valeurs animées à la grille en temps réel ---
    else if (change.Property == RightPanelWidthStarProperty)
    {
        // Math.Max évite les valeurs négatives si l'animation "rebondit" légèrement
        double val = Math.Max(0, change.GetNewValue<double>());
        MJETBrancheGraph.ColumnDefinitions[1].Width = new GridLength(val, GridUnitType.Star);
    }
    else if (change.Property == BottomPanelHeightStarProperty)
    {
        double val = Math.Max(0, change.GetNewValue<double>());
        ModuleEtJournal.RowDefinitions[1].Height = new GridLength(val, GridUnitType.Star);
    }
}
    
   
}