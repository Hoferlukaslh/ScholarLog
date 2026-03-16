/*
    Fichier      :  JournalPage.axaml.cs
    Projet       :  ScholarLog

    Description  :
        Code-behind de la vue JournalPage. 
        Gère l'affichage, la création, la modification et la suppression 
        des entrées du journal de travail. Inclut également la gestion 
        des catégories (TypeTravail), la visualisation via un graphique 
        Donut et les fonctionnalités d'exportation (MD, CSV, JSON).

    Auteur       :  Lukas Hofer - TINF2
    Date         :  16.03.2026

    Remarques    :
        - Utilise des StyledProperties pour le binding bidirectionnel.
        - Gère un système de "DeletePending" (confirmation visuelle rouge) pour la suppression.
        - Supporte l'exportation multi-modules via les touches modificatrices (Ctrl/Shift/Alt).
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

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia.Platform.Storage; 

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
    
    public async Task SauvegarderEntree()
    {
        try 
        {
            // vérification que la description n'est pas vide
            if (string.IsNullOrWhiteSpace(EditingEntree?.Description))
            {
                Console.WriteLine("Sauvegarde annulée : La description est obligatoire.");
                return; // On bloque la sauvegarde ici
            }

            // Préparation des IDs
            if (EditingEntree.Module != null) 
                EditingEntree.ModuleId = EditingEntree.Module.Id;
                
            if (EditingEntree.TypeTravailId <= 0 && ModalTypesTravail?.Count > 0)
                EditingEntree.TypeTravailId = ModalTypesTravail[0].Id;

            // validation finale des IDs
            if (EditingEntree.ModuleId <= 0 || EditingEntree.TypeTravailId <= 0)
            {
                Console.WriteLine("Sauvegarde annulée : ModuleId ou TypeTravailId est invalide.");
                return; 
            }

            var typeSelectionne = ModalTypesTravail.FirstOrDefault(t => t.Id == EditingEntree.TypeTravailId);
            
            // détacher les objets pour éviter que Entity Framework essaie de les re-créer
            EditingEntree.Module = null;
            EditingEntree.Type = null;

            // Sauvegarde en base de données
            using (var repo = new DataRepository())
            {
                if (_isEditingExisting)
                    await repo.ModifierEntreeAsync(EditingEntree);
                else
                    await repo.AjouterEntreeAsync(EditingEntree);
            }

            // mise à jour mémoire (interface)
            if (typeSelectionne != null)
            {
                EditingEntree.Type = new TypeTravail 
                { 
                    Id = typeSelectionne.Id, 
                    Nom = typeSelectionne.Nom, 
                    ModuleId = typeSelectionne.ModuleId 
                };
            }
            
            var moduleDestination = Modules?.FirstOrDefault(m => m.Id == EditingEntree.ModuleId);

            if (moduleDestination != null)
            {
                moduleDestination.JournalDeTravail ??= new List<Entree>();

                if (_isEditingExisting)
                {
                    var ancienneEntree = moduleDestination.JournalDeTravail.FirstOrDefault(e => e.Id == EditingEntree.Id);
                    if (ancienneEntree != null)
                    {
                        moduleDestination.JournalDeTravail.Remove(ancienneEntree);
                    }
                }
                
                // Dans les deux cas (ajout ou édition), on ajoute l'entité à jour
                moduleDestination.JournalDeTravail.Add(EditingEntree);
            }
            
            if (SelectedModule != null)
            {
                actualiserJournalTypeTravail(SelectedModule); 
            }
            
            FermerModal();
        }
        catch (Exception ex)
        {
            // empêche l'application de crasher si la DB est verrouillée
            Console.WriteLine($"Erreur fatale lors de la sauvegarde : {ex.Message}");
        }
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
    

#region  Journal Modal

    
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
#endregion

#region Type Travail Modal

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
#endregion
    

#region Graphique Donut

  
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
#endregion
    
    
#region Exportation des données

    public static readonly StyledProperty<bool> IsExportModalOpenProperty =
        AvaloniaProperty.Register<JournalPage, bool>(nameof(IsExportModalOpen), false);

    public bool IsExportModalOpen
    {
        get => GetValue(IsExportModalOpenProperty);
        set => SetValue(IsExportModalOpenProperty, value);
    }
    
    public static readonly StyledProperty<string> ExportPreviewTextProperty =
        AvaloniaProperty.Register<JournalPage, string>(nameof(ExportPreviewText), string.Empty);

    public string ExportPreviewText
    {
        get => GetValue(ExportPreviewTextProperty);
        set => SetValue(ExportPreviewTextProperty, value);
    }

    private string _currentExportFormat = "MD";
    private bool _exportAllModules = false;


    // commandes et interface

    private void BoutonExporter_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        _exportAllModules = e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control) ||
                            e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Shift) ||
                            e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Alt);

        OuvrirModalExportation();
    }

    public void OuvrirModalExportation()
    {
        if (!GetModulesAExporter().Any()) return;
        
        // Coche visuellement le bouton MD par défaut à l'ouverture
        RbExportMd.IsChecked = true;
        ChangerFormatExportation("MD");
        
        IsExportModalOpen = true;
    }

    public void FermerModalExportation() => IsExportModalOpen = false;

    // choix exportation
    public void ChangerFormatExportation(string format)
    {
        _currentExportFormat = format;

        ExportPreviewText = format.ToUpper() switch
        {
            "CSV" => GenererContenuCSV(),
            "JSON" => GenererContenuJSON(),
            "MD" => GenererContenuMD(),
            _ => string.Empty
        };
    }

    public async void CopierPressePapier()
    {
        if (string.IsNullOrEmpty(ExportPreviewText)) return;
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard != null) await topLevel.Clipboard.SetTextAsync(ExportPreviewText);
    }

    public async void SauvegarderFichierExportation()
    {
        if (string.IsNullOrEmpty(ExportPreviewText)) return;
        await SauvegarderFichierAsync(ExportPreviewText, _currentExportFormat.ToLower());
    }


    // logique métier

    private List<ModuleViewModel> GetModulesAExporter()
    {
        if (_exportAllModules)
            return Modules.Where(m => m.JournalDeTravail != null && m.JournalDeTravail.Any()).ToList();
        
        if (SelectedModule != null && SelectedModule.JournalDeTravail != null && SelectedModule.JournalDeTravail.Any())
            return new List<ModuleViewModel> { SelectedModule };
            
        return new List<ModuleViewModel>();
    }

   private string GenererContenuMD()
    {
        var sb = new StringBuilder();
        var modules = GetModulesAExporter();

        if (_exportAllModules)
        {
            sb.AppendLine("# Rapport Global - Tous les modules\n");
        }

        double grandTotalHeuresFichier = 0.0; // Pour le total final de tout le fichier

        foreach (var mod in modules)
        {
            if (mod.JournalDeTravail != null)
            {
                if (_exportAllModules) sb.AppendLine($"## Module : {mod.Nom}");
                else sb.AppendLine($"# {mod.Nom}");
                
                sb.AppendLine();

                // calcul des heures 
                double totalHeuresModule = 0.0;
                var repartitionDict = new Dictionary<string, double>();

                foreach (var entree in mod.JournalDeTravail)
                {
                    totalHeuresModule += entree.Duree;
                    
                    string categorie = entree.Type?.Nom ?? "Général";
                    if (repartitionDict.ContainsKey(categorie))
                    {
                        repartitionDict[categorie] += entree.Duree;
                    }
                    else
                    {
                        repartitionDict.Add(categorie, entree.Duree);
                    }
                }

                grandTotalHeuresFichier += totalHeuresModule;

                // affichage du tableau d'abord
                sb.AppendLine($"| Date       | Temps | Type de travail | Description");
                sb.AppendLine($"|------------|-------|-----------------|----------------------------------");

                foreach (var entree in mod.JournalDeTravail)
                {
                    string date = entree.Date.ToString("dd.MM.yyyy");
                    string duree = entree.Duree.ToString("0.00").Replace(",", ".");
                    string type = (entree.Type?.Nom ?? "Général").PadRight(15);
                    string desc = entree.Description?.Replace("\n", " ").Replace("|", "-") ?? "";
                    
                    sb.AppendLine($"| {date} | {duree}  | {type} | {desc}");
                }

                sb.AppendLine("\n### Répartition des heures\n");

                // tri et affichage de la répartition
                var listeRepartition = new List<KeyValuePair<string, double>>();
                foreach (var kvp in repartitionDict)
                {
                    listeRepartition.Add(kvp);
                }

                listeRepartition.Sort((a, b) => b.Value.CompareTo(a.Value));

                foreach (var item in listeRepartition)
                {
                    sb.AppendLine($"**{item.Key}** : {item.Value.ToString("0.00").Replace(",", ".")}h   ");
                }

                sb.AppendLine();
                
                //  total du module à la fin
                sb.AppendLine($"**Total des heures :** {totalHeuresModule.ToString("0.00").Replace(",", ".")}h");
                sb.AppendLine();

                if (_exportAllModules) sb.AppendLine("---\n"); 
            }
        }

        // total global à la a la fin du fichier (si plusieurs modules)
        if (_exportAllModules && modules.Count > 1)
        {
            sb.AppendLine($"# TOTAL GLOBAL : {grandTotalHeuresFichier.ToString("0.00").Replace(",", ".")}h");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string GenererContenuCSV()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Module;Date;Temps;Type;Description");
        
        foreach (var mod in GetModulesAExporter())
        {
            if (mod.JournalDeTravail != null)
            {
                foreach (var e in mod.JournalDeTravail)
                {
                    sb.AppendLine($"{mod.Nom};{e.Date:dd.MM.yyyy};{e.Duree};{e.Type?.Nom ?? "Général"};{e.Description?.Replace(";", ",")}");
                }
            }
        }
        return sb.ToString();
    }

    private string GenererContenuJSON()
    {
        var modules = GetModulesAExporter();
        
        if (modules.Count == 1 && !_exportAllModules)
        {
            return CreerJsonPourModule(modules[0]).ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }
        
        var jsonArrayGlobal = new JsonArray();
        foreach (var mod in modules)
        {
            jsonArrayGlobal.Add((JsonNode)CreerJsonPourModule(mod));
        }
        return jsonArrayGlobal.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private JsonObject CreerJsonPourModule(ModuleViewModel mod)
    {
        var jsonRoot = new JsonObject();
        jsonRoot["Module"] = JsonValue.Create(mod.Nom ?? "MOD");

        var totalHeuresObject = new JsonObject();
        if (mod.JournalDeTravail != null)
        {
            var repartition = mod.JournalDeTravail
                .GroupBy(e => e.Type?.Nom ?? "Général")
                .Select(g => new { Categorie = g.Key, Total = g.Sum(e => e.Duree) });

            foreach (var item in repartition) 
                totalHeuresObject[item.Categorie] = JsonValue.Create(item.Total);
        }
        jsonRoot["TotalHeures"] = totalHeuresObject;

        var jsonArray = new JsonArray();
        if (mod.JournalDeTravail != null)
        {
            foreach (var e in mod.JournalDeTravail)
            {
                var jsonObject = new JsonObject
                {
                    ["Date"] = JsonValue.Create(e.Date.ToString("yyyy-MM-dd")),
                    ["Temps"] = JsonValue.Create(e.Duree),
                    ["Type"] = JsonValue.Create(e.Type?.Nom ?? "Général"),
                    ["Description"] = JsonValue.Create(e.Description ?? "")
                };
                
                jsonArray.Add((JsonNode)jsonObject);
            }
        }
        jsonRoot["Entrees"] = jsonArray;

        return jsonRoot;
    }


    // sauvegarde en fichier
    private async Task SauvegarderFichierAsync(string contenu, string extension)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            string trigramme = "ALL";
            
            if (!_exportAllModules)
            {
                string nomModule = SelectedModule?.ShortName ?? "MOD";
                trigramme = nomModule.Length >= 3 ? nomModule.Substring(0, 3).ToUpper() : nomModule.ToUpper();
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Exporter le journal de travail",
                SuggestedFileName = $"JournalDeTravail_{trigramme}.{extension}",
                DefaultExtension = extension
            });

            if (file != null)
            {
                await using var stream = await file.OpenWriteAsync();
                using var writer = new System.IO.StreamWriter(stream, Encoding.UTF8);
                await writer.WriteAsync(contenu);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur sauvegarde: {ex.Message}");
        }
    }
 #endregion
}