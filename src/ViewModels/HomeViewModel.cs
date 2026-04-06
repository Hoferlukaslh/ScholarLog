/*
    Fichier      :  HomeViewModel.cs
    Projet       :  ScholarLog

    Description  :
        ViewModel de la page d'accueil (Tableau de bord).
        Agrège les données globales pour afficher un résumé des modules, 
        calculer les moyennes par branche (Théorie vs Modules) et préparer 
        les données pour le graphique Donut de répartition du travail.

    Auteur       :  Lukas Hofer - TINF2
    Date         :  19.03.2026

    Remarques    :
        - Calcule les tendances (BrancheTrend) pour chaque évaluation.
        - Utilise le WeakReferenceMessenger pour déclencher la navigation vers le Journal 
          sans coupler fortement les ViewModels.
*/


using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using ScholarLog.Data;
using ScholarLog.Components.DonutDiagram;

namespace ScholarLog.ViewModels;


public partial class HomeViewModel : ViewModelBase
{
    // état global
    [ObservableProperty]
    private ObservableRangeCollection<ModuleViewModel> _modules;

    [ObservableProperty]
    private ModuleViewModel? _selectedModule;

    [ObservableProperty]
    private ModuleViewModel? _displayedModule;

    // collection pour l'affichage
    [ObservableProperty]
    private ObservableRangeCollection<BrancheViewModel> _branchesTM = new();

    [ObservableProperty]
    private ObservableRangeCollection<BrancheViewModel> _branchesM = new();

    [ObservableProperty]
    private ObservableRangeCollection<Entree> _journal = new();

    [ObservableProperty]
    private ObservableRangeCollection<DonutItem> _graphiqueDonnees = new();

    // événement pour communiquer avec MainWindow (sans couplage fort)
    public event EventHandler<ModuleViewModel>? NavigationVersJournalDemandee;

    public HomeViewModel()
    {
        Modules = AppDataService.Instance.Modules;
    }

    // déclencheurs automatiques
    
    
    partial void OnSelectedModuleChanged(ModuleViewModel? value)
    {
        if (value == null) return;

        DisplayedModule = value;

        Task.Run(() =>
        {
            var journalTrie = value.JournalDeTravail?
                .OrderByDescending(e => e.Date)
                .ToList() ?? new();

            var typesTravailCalcules = value.JournalDeTravail?
                .Where(e => e.Type != null)
                .GroupBy(e => e.Type)
                .Select(g => new DonutItem
                {
                    Label = g.Key.Nom,
                    Value = g.Sum(e => e.Duree)
                }).ToList() ?? new();

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
                        Notes = branche.Notes?.ToList() ?? new(),
                        BrancheTrend = AppDataService.Instance
                            .DeterminerTendance(new List<Branche> { branche }, moy)
                    };

                    if (branche.Type == TypeCours.TM) branchesTM.Add(vm);
                    else if (branche.Type == TypeCours.M) branchesM.Add(vm);
                }
            }

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                BranchesTM.ReplaceAll(branchesTM);
                BranchesM.ReplaceAll(branchesM);
                Journal.ReplaceAll(journalTrie);
                GraphiqueDonnees.ReplaceAll(typesTravailCalcules);
            });
        });
    }

    [RelayCommand]
    private void AllerAuxJournaux()
    {
        if (DisplayedModule != null)
        {
            // diffuse le message dans toute l'application
            WeakReferenceMessenger.Default.Send(new MainWindowViewModel.ModuleNavigationMessage(DisplayedModule));
        }
    }
}