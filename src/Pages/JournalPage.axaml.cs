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

public partial class JournalPage : UserControl
{
    // --- Propriétés Bindables pour Avalonia ---

    public static readonly StyledProperty<ModuleViewModel?> SelectedModuleProperty =
        AvaloniaProperty.Register<JournalPage, ModuleViewModel?>(nameof(SelectedModule));

    public ModuleViewModel? SelectedModule
    {
        get => GetValue(SelectedModuleProperty);
        set => SetValue(SelectedModuleProperty, value);
    }

    public static readonly StyledProperty<string> TotalHeuresProperty =
        AvaloniaProperty.Register<JournalPage, string>(nameof(TotalHeures), "0.0h");

    public string TotalHeures
    {
        get => GetValue(TotalHeuresProperty);
        set => SetValue(TotalHeuresProperty, value);
    }

    // --- Collections ---

    public ObservableCollection<ModuleViewModel> Modules { get; set; } = Data.AppDataService.Instance.Modules;
    public ObservableCollection<Entree> Journal { get; set; } = new ObservableCollection<Entree>();
    public ObservableCollection<TypeTravailViewModel> TypesTravail { get; set; } = new ObservableCollection<TypeTravailViewModel>();
    
    public JournalPage()
    {
        InitializeComponent();
        DataContext = this; 
        
        // Sélectionne le premier module par défaut s'il y en a un
        if (Modules != null && Modules.Count > 0)
        {
            SelectedModule = Modules[0];
        }

        this.Loaded += JournalPage_Loaded;
    }
    
    private void JournalPage_Loaded(object? sender, RoutedEventArgs e)
    {
        this.Loaded -= JournalPage_Loaded; 
    }

    // Cette méthode est appelée automatiquement par Avalonia quand une StyledProperty change
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedModuleProperty)
        {
            var newVal = change.GetNewValue<ModuleViewModel?>();
            if (newVal != null)
            {
                actualiserJournalTypeTravail(newVal);
            }
            else
            {
                // Si la sélection est vidée
                Journal.Clear();
                TotalHeures = "0.0h";
            }
        }
    }
    
    private void actualiserJournalTypeTravail(ModuleViewModel moduleVM)
    {
        Journal.Clear();
        TypesTravail.Clear();
        double totalDuree = 0.0;
    
        var typestravail = new List<TypeTravailViewModel>();
    
        // remplit la liste du journal
        var journalTrie = moduleVM.JournalDeTravail
            .OrderByDescending(entree => entree.Date)
            .ToList();

        foreach (var entree in journalTrie)
        {
            Journal.Add(entree);
            totalDuree += entree.Duree; // additionne les heures
        }

        // mMise à jour du texte Total
        TotalHeures = $"{totalDuree:0.0}h";
    }
    
    public async void SupprimerEntree(Entree entreeASupprimer)
    {
        if (entreeASupprimer == null) return;

        try
        {
            // suppression dans la base de données
            using (var repo = new DataRepository())
                await repo.SupprimerEntreeAsync(entreeASupprimer);

            // suppression dans la liste visuelle (Met à jour l'UI instantanément)
            Journal.Remove(entreeASupprimer);

            // suppression dans les données du module sélectionné (pour éviter qu'elle ne réapparaisse)
            if (SelectedModule != null)
                SelectedModule.JournalDeTravail.Remove(entreeASupprimer);

            // recalcul du total des heures
            double totalDuree = Journal.Sum(e => e.Duree);
            TotalHeures = $"{totalDuree:0.0}h";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la suppression de l'entrée : {ex.Message}");
        }
           
    }
}