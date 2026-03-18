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
    
    
    public ObservableRangeCollection<ModuleViewModel> Modules { get; set; } = Data.AppDataService.Instance.Modules;
    public ObservableRangeCollection<BrancheViewModel> BranchesTM { get; set; } = new ObservableRangeCollection<BrancheViewModel>();
    public ObservableRangeCollection<BrancheViewModel> BranchesM { get; set; } = new ObservableRangeCollection<BrancheViewModel>();
    public ObservableRangeCollection<TypeTravailViewModel> TypesTravail { get; set; } = new ObservableRangeCollection<TypeTravailViewModel>();
    public ObservableRangeCollection<Entree> Journal { get; set; } = new ObservableRangeCollection<Entree>();
    
    
    public ObservableRangeCollection<DonutItem> GraphiqueDonnees { get; set; } = new ObservableRangeCollection<DonutItem>();
    
    // Clique sur un model de module (carte)
    private void OnModuleSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (SelectedModule == null) return; 

        // Tri du journal (LINQ)
        var journalTrie = SelectedModule.JournalDeTravail
            .OrderByDescending(entree => entree.Date)
            .ToList();

        // regroupement et somme par type de travail 
        var typesTravailCalcules = SelectedModule.JournalDeTravail
            .GroupBy(entree => entree.Type) // On groupe par type de travail
            .Select(groupe => new TypeTravailViewModel
            {
                Id = groupe.Key.Id,
                Nom = groupe.Key.Nom,
                ModuleId = groupe.Key.ModuleId,
                Somme = groupe.Sum(entree => entree.Duree) // Somme automatique des durées du groupe
            })
            .ToList();
        

        // dispatching des branches
        var branchesTM = new List<BrancheViewModel>();
        var branchesM = new List<BrancheViewModel>();

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

            if (branche.Type == TypeCours.TM)
                branchesTM.Add(vm);
            else if (branche.Type == TypeCours.M)
                branchesM.Add(vm);
        }
        
        // mise à jour de l'interface 
        
        // injection des nouvelles données en un seul bloc (le nettoyage est géré par ReplaceAll)
        BranchesTM.ReplaceAll(branchesTM);
        BranchesM.ReplaceAll(branchesM);
        Journal.ReplaceAll(journalTrie);
        TypesTravail.ReplaceAll(typesTravailCalcules);

        // transformation des types de travail en parts de Donut et injection
        GraphiqueDonnees.ReplaceAll(typesTravailCalcules.Select(type => new DonutItem 
        { 
            Label = type.Nom, 
            Value = type.Somme 
        }));
    }
#region animation

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
                // déclenche l'animation XAML vers l'ouverture
                RightPanelWidthStar = 3.0;
                BottomPanelHeightStar = 40.0;
            }
            else if (oldVal != null && newVal == null)  
            {
                // déclenche l'animation XAML vers la fermeture
                RightPanelWidthStar = 0.0;
                BottomPanelHeightStar = 0.0;
                
                // on attend la fin de l'animation (350ms) pour vider la vue, 
                // tout en vérifiant que l'utilisateur n'a pas cliqué sur un autre module entre temps
                Task.Delay(350).ContinueWith(_ => 
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => 
                    {
                        if (SelectedModule == null) DisplayedModule = null;
                    }));
            }
        }
        // appliquer les valeurs animées à la grille en temps réel ---
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
    
#endregion

}