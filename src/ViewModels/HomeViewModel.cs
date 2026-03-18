using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Linq;
using System.Collections.Generic;
using ScholarLog.Data;
using ScholarLog.Components.DonutDiagram;

namespace ScholarLog.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    // --- ÉTAT GLOBAL ---
    [ObservableProperty]
    private ObservableRangeCollection<ModuleViewModel> _modules;

    [ObservableProperty]
    private ModuleViewModel? _selectedModule;

    [ObservableProperty]
    private ModuleViewModel? _displayedModule;

    // --- COLLECTIONS POUR L'AFFICHAGE ---
    [ObservableProperty]
    private ObservableRangeCollection<BrancheViewModel> _branchesTM = new();

    [ObservableProperty]
    private ObservableRangeCollection<BrancheViewModel> _branchesM = new();

    [ObservableProperty]
    private ObservableRangeCollection<Entree> _journal = new();

    [ObservableProperty]
    private ObservableRangeCollection<DonutItem> _graphiqueDonnees = new();

    // Événement pour communiquer avec MainWindow (sans couplage fort)
    public event EventHandler<ModuleViewModel>? NavigationVersJournalDemandee;

    public HomeViewModel()
    {
        Modules = AppDataService.Instance.Modules;
    }

    // --- DÉCLENCHEURS AUTOMATIQUES ---
    
    // Remplace l'ancien événement OnModuleSelectionChanged du XAML
    partial void OnSelectedModuleChanged(ModuleViewModel? value)
    {
        if (value == null) return;

        DisplayedModule = value;

        // 1. Tri du journal
        var journalTrie = value.JournalDeTravail?.OrderByDescending(e => e.Date).ToList() ?? new List<Entree>();

        // 2. Regroupement pour le graphique Donut
        var typesTravailCalcules = value.JournalDeTravail?
            .Where(e => e.Type != null)
            .GroupBy(e => e.Type)
            .Select(g => new TypeTravailViewModel
            {
                Id = g.Key.Id,
                Nom = g.Key.Nom,
                ModuleId = g.Key.ModuleId,
                Somme = g.Sum(e => e.Duree)
            }).ToList() ?? new List<TypeTravailViewModel>();

        // 3. Dispatching des branches (Théorie vs Modules)
        var branchesTM = new List<BrancheViewModel>();
        var branchesM = new List<BrancheViewModel>();

        if (value.Branches != null)
        {
            foreach (var branche in value.Branches)
            {
                double moy = branche.CalculerMoyenne();
                var vm = new BrancheViewModel
                {
                    Nom = branche.Nom,
                    Moyenne = Math.Round(moy, 1),
                    Type = branche.Type,
                    Notes = branche.Notes?.ToList() ?? new List<Note>(),
                    BrancheTrend = AppDataService.Instance.DeterminerTendance(new List<Branche> { branche }, moy)
                };

                if (branche.Type == TypeCours.TM) branchesTM.Add(vm);
                else if (branche.Type == TypeCours.M) branchesM.Add(vm);
            }
        }
        
        // 4. Injection des données
        BranchesTM.ReplaceAll(branchesTM);
        BranchesM.ReplaceAll(branchesM);
        Journal.ReplaceAll(journalTrie);
        
        GraphiqueDonnees.ReplaceAll(typesTravailCalcules.Select(t => new DonutItem 
        { 
            Label = t.Nom, 
            Value = t.Somme 
        }));
    }

    // --- COMMANDES ---
    [RelayCommand]
    private void AllerAuxJournaux()
    {
        if (DisplayedModule != null)
        {
            NavigationVersJournalDemandee?.Invoke(this, DisplayedModule);
        }
    }
}