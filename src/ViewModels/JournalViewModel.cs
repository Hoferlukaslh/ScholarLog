/*
    Fichier      :  JournalViewModel.cs
    Projet       :  ScholarLog

    Description  :
        ViewModel dédié à la gestion du journal de travail étudiant.
        Gère les opérations CRUD pour les entrées du journal et les catégories 
        (Types de travail). Gère également la logique complexe de génération 
        des rapports d'exportation (Markdown, CSV, JSON).

    Auteur       :  Lukas Hofer - TINF2
    Date         :  19.03.2026

    Remarques    :
        - Gère les états de plusieurs modales (Ajout/Édition, Catégories, Export, Graphique).
        - Les méthodes de génération de contenu calculent les totaux et groupements à la volée.
        - Implémente une sécurité (DeleteWarningMessage) lors de la suppression d'une catégorie liée à des entrées.
*/


using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScholarLog.Data;
using ScholarLog.Components.DonutDiagram;

namespace ScholarLog.ViewModels;


public partial class JournalViewModel : ViewModelBase
{
    // état global
    [ObservableProperty]
    private ObservableRangeCollection<ModuleViewModel> _modules;

    [ObservableProperty]
    private ModuleViewModel? _selectedModule;

    [ObservableProperty]
    private ObservableRangeCollection<Entree> _journal = new();

    [ObservableProperty]
    private ObservableRangeCollection<TypeTravailViewModel> _typesTravail = new();

    [ObservableProperty]
    private string _totalHeures = "0.0h";

    
    // modal : Ajout/Edition Entrée
    [ObservableProperty]
    private bool _isModalOpen = false;

    [ObservableProperty]
    private string _modalTitle = "Nouvelle entrée";

    [ObservableProperty]
    private Entree _editingEntree = new();

    [ObservableProperty]
    private bool _isNewEntry = true;

    [ObservableProperty]
    private ModuleViewModel? _modalSelectedModule;

    [ObservableProperty]
    private ObservableCollection<TypeTravailViewModel> _modalTypesTravail = new();

    private bool _isEditingExisting;

    
    // modal : types de travail
    [ObservableProperty]
    private bool _isModalTypesOpen = false;

    [ObservableProperty]
    private string _nouveauTypeNom = string.Empty;

    [ObservableProperty]
    private bool _isConfirmDeleteTypeOpen = false;

    [ObservableProperty]
    private string _deleteWarningMessage = string.Empty;

    private TypeTravailViewModel? _typeToDelete;

    // modal : graphique
    [ObservableProperty]
    private bool _isModalGraphOpen = false;

    [ObservableProperty]
    private ObservableRangeCollection<DonutItem> _graphiqueDonnees = new();

    
    // modal : exportation
    [ObservableProperty]
    private bool _isExportModalOpen = false;

    [ObservableProperty]
    private string _exportPreviewText = string.Empty;

    private string _currentExportFormat = "MD";
    private bool _exportAllModules = false;


    public JournalViewModel()
    {
        Modules = AppDataService.Instance.Modules;

        if (Modules.Count > 0)
        {
            SelectedModule = Modules[0];
        }
    }

    // déclencheurs
    partial void OnSelectedModuleChanged(ModuleViewModel? value)
    {
        if (value != null)
        {
            ActualiserJournalTypeTravail(value);
        }
        else
        {
            Journal.Clear();
            TotalHeures = "0.0h";
        }
    }

    partial void OnModalSelectedModuleChanged(ModuleViewModel? value)
    {
        if (value != null && EditingEntree != null)
        {
            EditingEntree.Module = value;
            EditingEntree.ModuleId = value.Id;
            ActualiserModalTypesTravail(value);
        }
    }

    // actualisation
    private async void ActualiserJournalTypeTravail(ModuleViewModel moduleVM)
    {
        Journal.Clear();
        TypesTravail.Clear();
        double totalDuree = 0.0;

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

        if (TypesTravail.Count == 0)
        {
            var typeDefaut = new TypeTravail { Nom = "Général", ModuleId = moduleVM.Id };
            using (var repo = new DataRepository())
            {
                await repo.AjouterTypeTravailAsync(typeDefaut);
            }
            TypesTravail.Add(new TypeTravailViewModel { Id = typeDefaut.Id, Nom = typeDefaut.Nom, ModuleId = typeDefaut.ModuleId });
        }

        var journalTrie = moduleVM.JournalDeTravail?.OrderByDescending(entree => entree.Date).ToList() ?? new List<Entree>();

        foreach (var entree in journalTrie)
        {
            Journal.Add(entree);
            totalDuree += entree.Duree;
        }

        TotalHeures = $"{totalDuree:0.0}h";
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

#region Commandes : CRUD Entrées 

    [RelayCommand]
    private void OuvrirModalAjout()
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

    [RelayCommand]
    private void OuvrirModalModification(Entree entreeAModifier)
    {
        if (entreeAModifier == null) return;

        ModalTitle = "Modifier entrée";
        _isEditingExisting = true;
        IsNewEntry = false;

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

    [RelayCommand]
    private void FermerModal() => IsModalOpen = false;

    [RelayCommand]
    private async Task SauvegarderEntree()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(EditingEntree?.Description)) return;

            if (EditingEntree.Module != null) EditingEntree.ModuleId = EditingEntree.Module.Id;
            if (EditingEntree.TypeTravailId <= 0 && ModalTypesTravail?.Count > 0)
                EditingEntree.TypeTravailId = ModalTypesTravail[0].Id;

            if (EditingEntree.ModuleId <= 0 || EditingEntree.TypeTravailId <= 0) return;

            var typeSelectionne = ModalTypesTravail.FirstOrDefault(t => t.Id == EditingEntree.TypeTravailId);
            EditingEntree.Module = null;
            EditingEntree.Type = null;

            using (var repo = new DataRepository())
            {
                if (_isEditingExisting) await repo.ModifierEntreeAsync(EditingEntree);
                else await repo.AjouterEntreeAsync(EditingEntree);
            }

            if (typeSelectionne != null)
            {
                EditingEntree.Type = new TypeTravail { Id = typeSelectionne.Id, Nom = typeSelectionne.Nom, ModuleId = typeSelectionne.ModuleId };
            }

            var moduleDestination = Modules?.FirstOrDefault(m => m.Id == EditingEntree.ModuleId);
            if (moduleDestination != null)
            {
                moduleDestination.JournalDeTravail ??= new List<Entree>();
                if (_isEditingExisting)
                {
                    var ancienneEntree = moduleDestination.JournalDeTravail.FirstOrDefault(e => e.Id == EditingEntree.Id);
                    if (ancienneEntree != null) moduleDestination.JournalDeTravail.Remove(ancienneEntree);
                }
                moduleDestination.JournalDeTravail.Add(EditingEntree);
            }

            if (SelectedModule != null) ActualiserJournalTypeTravail(SelectedModule);
            FermerModal();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur : {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ExecuterSuppression(Entree entreeASupprimer)
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
#endregion

#region Commandes : CRUD Types de travail



    [RelayCommand]
    private void OuvrirModalTypesTravail()
    {
        if (SelectedModule != null) IsModalTypesOpen = true;
    }

    [RelayCommand]
    private void FermerModalTypes()
    {
        IsModalTypesOpen = false;
        NouveauTypeNom = string.Empty;
    }

    [RelayCommand]
    private async Task AjouterNouveauType()
    {
        if (string.IsNullOrWhiteSpace(NouveauTypeNom) || SelectedModule == null) return;

        var nouveauType = new TypeTravail { Nom = NouveauTypeNom.Trim(), ModuleId = SelectedModule.Id };

        using (var repo = new DataRepository())
        {
            await repo.AjouterTypeTravailAsync(nouveauType);
        }

        SelectedModule.TypesDeTravail ??= new List<TypeTravail>();
        SelectedModule.TypesDeTravail.Add(nouveauType);

        ActualiserJournalTypeTravail(SelectedModule);
        ActualiserModalTypesTravail(SelectedModule);
        NouveauTypeNom = string.Empty;
    }

    [RelayCommand]
    private async Task RenommerType(TypeTravailViewModel typeVM)
    {
        if (typeVM == null || string.IsNullOrWhiteSpace(typeVM.Nom) || SelectedModule == null) return;

        var typeAModifier = new TypeTravail { Id = typeVM.Id, Nom = typeVM.Nom.Trim(), ModuleId = typeVM.ModuleId };

        using (var repo = new DataRepository())
        {
            await repo.ModifierTypeTravailAsync(typeAModifier);
        }

        var typeInMemory = SelectedModule.TypesDeTravail?.FirstOrDefault(t => t.Id == typeVM.Id);
        if (typeInMemory != null) typeInMemory.Nom = typeVM.Nom;

        if (SelectedModule.JournalDeTravail != null)
        {
            foreach (var entree in SelectedModule.JournalDeTravail.Where(e => e.Type != null && e.Type.Id == typeVM.Id))
            {
                entree.Type.Nom = typeVM.Nom;
            }
        }

        ActualiserJournalTypeTravail(SelectedModule);
        ActualiserModalTypesTravail(SelectedModule);
    }

    [RelayCommand]
    private async Task DemanderSuppressionType(TypeTravailViewModel typeVM)
    {
        if (typeVM == null || SelectedModule == null) return;

        _typeToDelete = typeVM;
        int count = SelectedModule.JournalDeTravail?.Count(e => e.Type != null && e.Type.Id == typeVM.Id) ?? 0;

        if (count == 0)
        {
            await ExecuterSuppressionTypeAsync();
        }
        else
        {
            DeleteWarningMessage = $"Voulez-vous vraiment supprimer la catégorie '{typeVM.Nom}' ?\nCela supprimera également {count} entrée(s) de journal qui y sont associée(s).";
            IsConfirmDeleteTypeOpen = true;
        }
    }

    [RelayCommand]
    private void AnnulerSuppressionType()
    {
        IsConfirmDeleteTypeOpen = false;
        _typeToDelete = null;
    }

    [RelayCommand]
    private async Task ConfirmerSuppressionType()
    {
        await ExecuterSuppressionTypeAsync();
        IsConfirmDeleteTypeOpen = false;
    }

    private async Task ExecuterSuppressionTypeAsync()
    {
        if (_typeToDelete == null || SelectedModule == null) return;

        using (var repo = new DataRepository())
        {
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

            var typeStub = new TypeTravail { Id = _typeToDelete.Id };
            await repo.SupprimerTypeTravailAsync(typeStub);
            var typeInMemory = SelectedModule.TypesDeTravail?.FirstOrDefault(t => t.Id == _typeToDelete.Id);
            if (typeInMemory != null) SelectedModule.TypesDeTravail.Remove(typeInMemory);
        }

        _typeToDelete = null;
        ActualiserJournalTypeTravail(SelectedModule);
        ActualiserModalTypesTravail(SelectedModule);
    }
    
#endregion


    // graphique
    [RelayCommand]
    private void OuvrirModalGraphique()
    {
        var donneesGroupees = Journal
            .Where(e => e.Type != null)
            .GroupBy(e => e.Type.Nom)
            .Select(g => new DonutItem
            {
                Label = g.Key,
                Value = g.Sum(e => e.Duree)
            });

        GraphiqueDonnees.ReplaceAll(donneesGroupees);
        IsModalGraphOpen = true;
    }

    [RelayCommand]
    private void FermerModalGraphique() => IsModalGraphOpen = false;

    // exportation
    [RelayCommand]
    private void ChangerFormatExportation(string format)
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

     private string GenererContenuMD()
    {
        var sb = new StringBuilder();
        var modules = GetModulesAExporter();

        if (_exportAllModules)
        {
            sb.AppendLine("# Rapport Global - Tous les modules\n");
        }

        double grandTotalHeuresFichier = 0.0; // pour le total final de tout le fichier

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
                var listeRepartition = repartitionDict.OrderByDescending(kvp => kvp.Value);

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

    [RelayCommand]
    private void FermerModalExportation() => IsExportModalOpen = false;


    public void SetExportAllModules(bool exportAll)
    {
        _exportAllModules = exportAll;
    }
    private List<ModuleViewModel> GetModulesAExporter()
    {
        if (_exportAllModules)
            return Modules.Where(m => m.JournalDeTravail != null && m.JournalDeTravail.Any()).ToList();
        
        if (SelectedModule != null && SelectedModule.JournalDeTravail != null && SelectedModule.JournalDeTravail.Any())
            return new List<ModuleViewModel> { SelectedModule };
            
        return new List<ModuleViewModel>();
    }
    

    public string GetCurrentExportFormat() => _currentExportFormat;
    public bool GetExportAllModules() => _exportAllModules;
}