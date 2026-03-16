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
    public static readonly StyledProperty<ModuleViewModel?> SelectedModuleProperty =
        AvaloniaProperty.Register<JournalPage, ModuleViewModel?>(nameof(SelectedModule));
    
    public static readonly StyledProperty<bool> IsModalOpenProperty =
        AvaloniaProperty.Register<JournalPage, bool>(nameof(IsModalOpen), false);
    
    public bool IsModalOpen
    {
        get => GetValue(IsModalOpenProperty);
        set => SetValue(IsModalOpenProperty, value);
    }
 
    
    public static readonly StyledProperty<string> ModalTitleProperty =
        AvaloniaProperty.Register<JournalPage, string>(nameof(ModalTitle), "Nouvelle entrée");
    
    public string ModalTitle
    {
        get => GetValue(ModalTitleProperty);
        set => SetValue(ModalTitleProperty, value);
    }
    

    public static readonly StyledProperty<Entree> EditingEntreeProperty =
        AvaloniaProperty.Register<JournalPage, Entree>(nameof(EditingEntree));

    public Entree EditingEntree
    {
        get => GetValue(EditingEntreeProperty);
        set => SetValue(EditingEntreeProperty, value);
    }
    
    public static readonly StyledProperty<bool> IsNewEntryProperty =
        AvaloniaProperty.Register<JournalPage, bool>(nameof(IsNewEntry), true);

    public bool IsNewEntry
    {
        get => GetValue(IsNewEntryProperty);
        set => SetValue(IsNewEntryProperty, value);
    }

    private bool _isEditingExisting; // update ou insert
    

    public ObservableCollection<TypeTravailViewModel> ModalTypesTravail { get; set; } = new ObservableCollection<TypeTravailViewModel>();

    public static readonly StyledProperty<ModuleViewModel?> ModalSelectedModuleProperty =
        AvaloniaProperty.Register<JournalPage, ModuleViewModel?>(nameof(ModalSelectedModule));

    public ModuleViewModel? ModalSelectedModule
    {
        get => GetValue(ModalSelectedModuleProperty);
        set => SetValue(ModalSelectedModuleProperty, value);
    }
    public void OuvrirModalAjout()
    {
        ModalTitle = "Nouvelle entrée";
        _isEditingExisting = false;
        IsNewEntry = true;
        
        EditingEntree = new Entree 
        { 
            Date = DateTime.Today,
            Duree = 1.0,
            Module = SelectedModule,
            ModuleId = SelectedModule?.Id ?? 0
        };
        
        ModalSelectedModule = SelectedModule;
    
        IsModalOpen = true;
    }

    public void OuvrirModalModification(Entree entreeAModifier)
    {
        if (entreeAModifier == null) return;

        ModalTitle = "Modifier entrée";
        _isEditingExisting = true;
        IsNewEntry = false;
    
        // clone l'entrée
        EditingEntree = new Entree
        {
            Id = entreeAModifier.Id,
            Date = entreeAModifier.Date,
            Duree = entreeAModifier.Duree,
            Description = entreeAModifier.Description,
            Module = entreeAModifier.Module,
            ModuleId = entreeAModifier.ModuleId,
            Type = entreeAModifier.Type,
            TypeTravailId = entreeAModifier.TypeTravailId
        };
        
        ModalSelectedModule = Modules.FirstOrDefault(m => m.Id == entreeAModifier.ModuleId);
        
        EditingEntree.TypeTravailId = entreeAModifier.TypeTravailId; 

        IsModalOpen = true;
    }

    public void FermerModal()
    {
        IsModalOpen = false;
    }
    
    public async void SauvegarderEntree()
{
    if (EditingEntree.Module != null) 
        EditingEntree.ModuleId = EditingEntree.Module.Id;
        
    if (EditingEntree.TypeTravailId == 0 && ModalTypesTravail.Count > 0)
        EditingEntree.TypeTravailId = ModalTypesTravail[0].Id;

    if (EditingEntree.ModuleId <= 0 || EditingEntree.TypeTravailId <= 0)
    {
        Console.WriteLine("Sauvegarde annulée : ModuleId ou TypeTravailId est invalide (0).");
        return; 
    }

    var typeSelectionne = ModalTypesTravail.FirstOrDefault(t => t.Id == EditingEntree.TypeTravailId);
    
    EditingEntree.Module = null;
    EditingEntree.Type = null;

    using (var repo = new DataRepository())
    {
        if (_isEditingExisting)
            await repo.ModifierEntreeAsync(EditingEntree);
        else
            await repo.AjouterEntreeAsync(EditingEntree);
    }

    // mise a jour mémoire (interface)

    // objet pour l'affichage
    if (typeSelectionne != null)
    {
        EditingEntree.Type = new TypeTravail 
        { 
            Id = typeSelectionne.Id, 
            Nom = typeSelectionne.Nom, 
            ModuleId = typeSelectionne.ModuleId 
        };
    }
    
    var moduleDestination = Modules.FirstOrDefault(m => m.Id == EditingEntree.ModuleId);

    if (moduleDestination != null)
    {

        if (moduleDestination.JournalDeTravail == null)
        {
            moduleDestination.JournalDeTravail = new List<Entree>();
        }

        if (!_isEditingExisting)
        {
            // ajoute la nouvelle entrée à son vrai module
            moduleDestination.JournalDeTravail.Add(EditingEntree);
        }
        else
        {
            // remplace l'ancienne entrée
            var ancienneEntree = moduleDestination.JournalDeTravail.FirstOrDefault(e => e.Id == EditingEntree.Id);
            if (ancienneEntree != null)
            {
                moduleDestination.JournalDeTravail.Remove(ancienneEntree);
            }
            moduleDestination.JournalDeTravail.Add(EditingEntree);
        }
    }
    
    if (SelectedModule != null)
    {
        actualiserJournalTypeTravail(SelectedModule); 
    }
    
    FermerModal();
}
    

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
                actualiserJournalTypeTravail(newVal);
            else
            {
                Journal.Clear();
                TotalHeures = "0.0h";
            }
        }
        // AJOUT ICI : Si on change le module dans le Modal
        else if (change.Property == ModalSelectedModuleProperty)
        {
            var newVal = change.GetNewValue<ModuleViewModel?>();
            if (newVal != null && EditingEntree != null)
            {
                EditingEntree.Module = newVal;
                EditingEntree.ModuleId = newVal.Id;
                ActualiserModalTypesTravail(newVal);
            }
        }
    }
    
   
    
    private async void actualiserJournalTypeTravail(ModuleViewModel moduleVM)
    {
        Journal.Clear();
        TypesTravail.Clear();
        double totalDuree = 0.0;
    
        // fouille dans le journal existant
        if (moduleVM.JournalDeTravail != null)
        {
            foreach (var entree in moduleVM.JournalDeTravail)
            {
                if (entree.Type != null && !TypesTravail.Any(t => t.Id == entree.Type.Id))
                {
                    TypesTravail.Add(new TypeTravailViewModel { Id = entree.Type.Id, Nom = entree.Type.Nom, ModuleId = entree.Type.ModuleId });
                }
            }
        }

        // ajoute les types de la base de données
        if (moduleVM.TypesDeTravail != null)
        {
            foreach (var type in moduleVM.TypesDeTravail)
            {
                if (!TypesTravail.Any(t => t.Id == type.Id))
                {
                    TypesTravail.Add(new TypeTravailViewModel { Id = type.Id, Nom = type.Nom, ModuleId = type.ModuleId });
                }
            }
        }

        // aucun typetravail dans BDD -> création type générique
        if (TypesTravail.Count == 0)
        {
            var typeDefaut = new TypeTravail { Nom = "Général", ModuleId = moduleVM.Id };
        
            using (var repo = new DataRepository())
            {
                await repo.AjouterTypeTravailAsync(typeDefaut);
            }
        
            TypesTravail.Add(new TypeTravailViewModel { Id = typeDefaut.Id, Nom = typeDefaut.Nom, ModuleId = typeDefaut.ModuleId });
        }
       
        var journalTrie = moduleVM.JournalDeTravail
            .OrderByDescending(entree => entree.Date)
            .ToList();

        foreach (var entree in journalTrie)
        {
            Journal.Add(entree);
            totalDuree += entree.Duree; 
        }

        TotalHeures = $"{totalDuree:0.0}h";
    }
    
    /// <summary>
    /// Journal Modal
    /// </summary>
    
    private async void BoutonSupprimer_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        // On récupère le bouton cliqué et l'Entree (la donnée) qui lui est attachée
        if (sender is Button btn && btn.DataContext is Entree entree)
        {
            // on vérifie l'état du clavier au moment du clic
            bool isModifierPressed = e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control) ||
                                     e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Shift) ||
                                     e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Alt);

            // si une touche est enfoncée OU si c'est le 2ème clic (déjà rouge)
            if (isModifierPressed || entree.IsDeletePending)
            {
                await ExecuterSuppression(entree);
            }
            else
            {
                // Clic normal -> On met en attente (devient rouge)
                entree.IsDeletePending = true;
            
                // attend 3 secondes
                await Task.Delay(3000);
            
                // si elle n'a pas été supprimée entre-temps, elle redevient normale
                if (entree != null)
                    entree.IsDeletePending = false;
            }
        }
    }


    private async Task ExecuterSuppression(Entree entreeASupprimer)
    {
        try
        {
            using (var repo = new DataRepository())
            {
                await repo.SupprimerEntreeAsync(entreeASupprimer);
            }

            Journal.Remove(entreeASupprimer);
            if (SelectedModule != null) SelectedModule.JournalDeTravail.Remove(entreeASupprimer);
        
            double totalDuree = Journal.Sum(e => e.Duree);
            TotalHeures = $"{totalDuree:0.0}h";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur : {ex.Message}");
        }
    }
    
    private async void ActualiserModalTypesTravail(ModuleViewModel moduleVM)
    {
        ModalTypesTravail.Clear();
        
        if (moduleVM.TypesDeTravail != null)
        {
            foreach (var type in moduleVM.TypesDeTravail)
            {
                if (!ModalTypesTravail.Any(t => t.Id == type.Id))
                    ModalTypesTravail.Add(new TypeTravailViewModel { Id = type.Id, Nom = type.Nom, ModuleId = type.ModuleId });
            }
        }

        if (moduleVM.JournalDeTravail != null)
        {
            foreach (var entree in moduleVM.JournalDeTravail)
            {
                if (entree.Type != null && !ModalTypesTravail.Any(t => t.Id == entree.Type.Id))
                    ModalTypesTravail.Add(new TypeTravailViewModel { Id = entree.Type.Id, Nom = entree.Type.Nom, ModuleId = entree.Type.ModuleId });
            }
        }
        
        if (ModalTypesTravail.Count == 0)
        {
            var typeDefaut = new TypeTravail { Nom = "Général", ModuleId = moduleVM.Id };
            using (var repo = new DataRepository())
            {
                await repo.AjouterTypeTravailAsync(typeDefaut); 
            }
            ModalTypesTravail.Add(new TypeTravailViewModel { Id = typeDefaut.Id, Nom = typeDefaut.Nom, ModuleId = typeDefaut.ModuleId });
        }
        
        if (EditingEntree != null && !ModalTypesTravail.Any(t => t.Id == EditingEntree.TypeTravailId))
        {
            EditingEntree.TypeTravailId = ModalTypesTravail.FirstOrDefault()?.Id ?? 0;
        }
    }

    private void CalendarDatePicker_OnSelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is CalendarDatePicker picker)
        {
            // si l'utilisateur a effacé la date (null), on remet aujourd'hui
            if (picker.SelectedDate == null)
            {
                picker.SelectedDate = DateTime.Now;
                return;
            }

            // on s'assure que notre objet EditingEntree est bien mis à jour
            if (EditingEntree != null)
            {
                // Conversion forcée de DateTimeOffset? vers DateTime
                EditingEntree.Date = picker.SelectedDate.Value.Date;
            }
        }
    }
    
    /// 
    /// Type Travail Modal
    /// 

    public static readonly StyledProperty<bool> IsModalTypesOpenProperty =
        AvaloniaProperty.Register<JournalPage, bool>(nameof(IsModalTypesOpen), false);

    public bool IsModalTypesOpen
    {
        get => GetValue(IsModalTypesOpenProperty);
        set => SetValue(IsModalTypesOpenProperty, value);
    }

    public static readonly StyledProperty<string> NouveauTypeNomProperty =
        AvaloniaProperty.Register<JournalPage, string>(nameof(NouveauTypeNom), string.Empty);

    public string NouveauTypeNom
    {
        get => GetValue(NouveauTypeNomProperty);
        set => SetValue(NouveauTypeNomProperty, value);
    }

    public static readonly StyledProperty<bool> IsConfirmDeleteTypeOpenProperty =
        AvaloniaProperty.Register<JournalPage, bool>(nameof(IsConfirmDeleteTypeOpen), false);

    public bool IsConfirmDeleteTypeOpen
    {
        get => GetValue(IsConfirmDeleteTypeOpenProperty);
        set => SetValue(IsConfirmDeleteTypeOpenProperty, value);
    }

    public static readonly StyledProperty<string> DeleteWarningMessageProperty =
        AvaloniaProperty.Register<JournalPage, string>(nameof(DeleteWarningMessage), string.Empty);

    public string DeleteWarningMessage
    {
        get => GetValue(DeleteWarningMessageProperty);
        set => SetValue(DeleteWarningMessageProperty, value);
    }

    private TypeTravailViewModel? _typeToDelete;

    public void OuvrirModalTypesTravail()
    {
        if (SelectedModule == null) return;
        IsModalTypesOpen = true;
    }

    public void FermerModalTypes()
    {
        IsModalTypesOpen = false;
        NouveauTypeNom = string.Empty;
    }

    public async void AjouterNouveauType()
    {
        if (string.IsNullOrWhiteSpace(NouveauTypeNom) || SelectedModule == null) return;

        var nouveauType = new TypeTravail { Nom = NouveauTypeNom.Trim(), ModuleId = SelectedModule.Id };
        
        using (var repo = new DataRepository())
        {
            await repo.AjouterTypeTravailAsync(nouveauType);
        }

        if (SelectedModule.TypesDeTravail == null)
            SelectedModule.TypesDeTravail = new List<TypeTravail>();
            
        SelectedModule.TypesDeTravail.Add(nouveauType);
        
        actualiserJournalTypeTravail(SelectedModule);
        ActualiserModalTypesTravail(SelectedModule);
        
        NouveauTypeNom = string.Empty;
    }

    public async void RenommerType(TypeTravailViewModel typeVM)
    {
        if (typeVM == null || string.IsNullOrWhiteSpace(typeVM.Nom) || SelectedModule == null) return;

        var typeAModifier = new TypeTravail { Id = typeVM.Id, Nom = typeVM.Nom.Trim(), ModuleId = typeVM.ModuleId };
        
        using (var repo = new DataRepository())
        {
            await repo.ModifierTypeTravailAsync(typeAModifier);
        }

        // met à jour la mémoire
        var typeInMemory = SelectedModule.TypesDeTravail?.FirstOrDefault(t => t.Id == typeVM.Id);
        if (typeInMemory != null) typeInMemory.Nom = typeVM.Nom;

        if (SelectedModule.JournalDeTravail != null)
        {
            foreach (var entree in SelectedModule.JournalDeTravail.Where(e => e.Type != null && e.Type.Id == typeVM.Id))
            {
                entree.Type.Nom = typeVM.Nom;
            }
        }
        
        actualiserJournalTypeTravail(SelectedModule);
        ActualiserModalTypesTravail(SelectedModule);
    }

    public async void DemanderSuppressionType(TypeTravailViewModel typeVM)
    {
        if (typeVM == null || SelectedModule == null) return;

        _typeToDelete = typeVM;
        
        // compte le nombre d'entrées liées à cette catégorie
        int count = SelectedModule.JournalDeTravail?.Count(e => e.Type != null && e.Type.Id == typeVM.Id) ?? 0;

        if (count == 0)
        {
            // s'il n'y a aucune entrée, on supprime directement et silencieusement
            await ExecuterSuppressionTypeAsync();
        }
        else
        {
            // s'il y a des entrées, on demande confirmation
            DeleteWarningMessage = $"Voulez-vous vraiment supprimer la catégorie '{typeVM.Nom}' ?\nCela supprimera également {count} entrée(s) de journal qui y sont associée(s).";
            IsConfirmDeleteTypeOpen = true;
        }
    }

    public void AnnulerSuppressionType()
    {
        IsConfirmDeleteTypeOpen = false;
        _typeToDelete = null;
    }

    public async void ConfirmerSuppressionType()
    {
        await ExecuterSuppressionTypeAsync();
        IsConfirmDeleteTypeOpen = false;
    }
    
    private async Task ExecuterSuppressionTypeAsync()
    {
        if (_typeToDelete == null || SelectedModule == null) return;

        using (var repo = new DataRepository())
        {
            // supprime d'abord les entrées de journal associées à cette catégorie
            if (SelectedModule.JournalDeTravail != null)
            {
                var entreesASupprimer = SelectedModule.JournalDeTravail.Where(e => e.Type != null && e.Type.Id == _typeToDelete.Id).ToList();
                foreach (var entree in entreesASupprimer)
                {
                    var entreeStub = new Entree { Id = entree.Id };
                    
                    await repo.SupprimerEntreeAsync(entreeStub);
                    SelectedModule.JournalDeTravail.Remove(entree);
                }
            }

            // supprime la catégorie elle-même de la BDD via un stub
            var typeStub = new TypeTravail { Id = _typeToDelete.Id };
            await repo.SupprimerTypeTravailAsync(typeStub);

            // nettoyage de la mémoire locale
            var typeInMemory = SelectedModule.TypesDeTravail?.FirstOrDefault(t => t.Id == _typeToDelete.Id);
            if (typeInMemory != null) SelectedModule.TypesDeTravail.Remove(typeInMemory);
        }

        _typeToDelete = null;
        
        // rafraîchissement de l'interface
        actualiserJournalTypeTravail(SelectedModule);
        ActualiserModalTypesTravail(SelectedModule);
    }
    
    /// 
    /// Graphique Donut
    ///
  
    // ajouter la collection pour le graphique
    public ObservableCollection<DonutItem> GraphiqueDonnees { get; set; } = new ObservableCollection<DonutItem>();

    // propriété pour afficher/masquer le modal
    public static readonly StyledProperty<bool> IsModalGraphOpenProperty =
        AvaloniaProperty.Register<JournalPage, bool>(nameof(IsModalGraphOpen), false);

    public bool IsModalGraphOpen
    {
        get => GetValue(IsModalGraphOpenProperty);
        set => SetValue(IsModalGraphOpenProperty, value);
    }

    // méthode pour ouvrir et calculer le graphique
    public void OuvrirModalGraphique()
    {
        GraphiqueDonnees.Clear();

        // regrouper les entrées du journal actuel par nom de type de travail et faire la somme des heures
        var donneesGroupees = Journal
            .Where(e => e.Type != null)
            .GroupBy(e => e.Type.Nom)
            .Select(g => new DonutItem
            {
                Label = g.Key,
                Value = g.Sum(e => e.Duree)
            });

        foreach (var item in donneesGroupees)
        {
            GraphiqueDonnees.Add(item);
        }

        IsModalGraphOpen = true;
    }

    // fermer le modal
    public void FermerModalGraphique()
    {
        IsModalGraphOpen = false;
    }
}