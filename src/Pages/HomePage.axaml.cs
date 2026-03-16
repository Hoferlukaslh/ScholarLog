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


    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedModuleProperty)
        {
            var oldVal = change.GetOldValue<ModuleViewModel?>();
            var newVal = change.GetNewValue<ModuleViewModel?>();

            // Si on a sélectionné un module,  met à jour affichage tampon
            if (newVal != null) DisplayedModule = newVal;

            if (oldVal == null && newVal != null)       AnimateGridsAsync(true);
            else if (oldVal != null && newVal == null)  AnimateGridsAsync(false);
        }
    }
    
    
    public async void AnimateGridsAsync(bool open, int durationMs = 150)
    {
        // Valeurs cibles demandées dans tes commentaires XAML
        double targetCol = open ? 3.0 : 0.0;
        double targetRow = open ? 40.0 : 0.0;

        // Valeurs actuelles de départ
        double startCol = MJETBrancheGraph.ColumnDefinitions[1].Width.Value;
        double startRow = ModuleEtJournal.RowDefinitions[1].Height.Value;

        // Configuration de l'animation
        int fps = 120;
        int steps = durationMs * fps / 1000;
        int delay = 1000 / fps;

        for (int i = 1; i <= steps; i++)
        {
            double progress = (double)i / steps;
        
            // Fonction d'assouplissement "Quadratic Ease Out" pour un mouvement naturel (ralentit à la fin)
            double ease = 1 - (1 - progress) * (1 - progress);
        
            double currentCol = startCol + (targetCol - startCol) * ease;
            double currentRow = startRow + (targetRow - startRow) * ease;

            // Application aux définitions de la grille avec l'unité "Star" (*)
            MJETBrancheGraph.ColumnDefinitions[1].Width = new GridLength(currentCol, GridUnitType.Star);
            ModuleEtJournal.RowDefinitions[1].Height = new GridLength(currentRow, GridUnitType.Star);

            // Pause asynchrone n'impactant pas le thread UI
            await Task.Delay(delay);
        }
    
        // Par sécurité, on force les valeurs exactes de fin pour éviter les erreurs d'arrondis
        MJETBrancheGraph.ColumnDefinitions[1].Width = new GridLength(targetCol, GridUnitType.Star);
        ModuleEtJournal.RowDefinitions[1].Height = new GridLength(targetRow, GridUnitType.Star);
        
        if (!open) DisplayedModule = null;
    }
}