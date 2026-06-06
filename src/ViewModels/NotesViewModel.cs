/*
    Fichier      :  NotesViewModel.cs
    Projet       :  ScholarLog

    Description  :
        ViewModel responsable de la gestion des notes (évaluations).
        Traite la logique d'ajout, de modification et de suppression des notes
        pour chaque branche, et maintient à jour les listes filtrées pour l'affichage.

    Auteur       :  Lukas Hofer - TINF2
    Date         :  19.03.2026

    Remarques    :
        - Utilise un modèle intermédiaire (NoteViewModel) pour lier visuellement une note brute à son module parent.
        - Gère le changement de module/branche parent lors de la modification d'une note existante.
        - Synchronise les modifications en direct avec la base de données SQLite via le DataRepository.
*/


using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Linq;
using System.Threading.Tasks;
using ScholarLog.Data;
using System.Text.Json;
using System.Text.Json.Nodes;


namespace ScholarLog.ViewModels;

public partial class NotesViewModel : ViewModelBase
{
    // état de la vue
    [ObservableProperty] private bool _isListView = true;

    [ObservableProperty] private System.Collections.ObjectModel.ObservableCollection<NoteViewModel> _allNotes = new();

    [ObservableProperty] private ObservableRangeCollection<Branche> _selectedModuleBranchesWithNotes = new();

    [ObservableProperty] private ObservableRangeCollection<ModuleViewModel> _modules;

    [ObservableProperty] private ModuleViewModel? _selectedModule;

    // état du modal
    [ObservableProperty] private bool _isModalOpen = false;

    [ObservableProperty] private string _modalTitle = "Nouvelle note";

    [ObservableProperty] private Note _editingNote = new();

    [ObservableProperty] private ModuleViewModel? _modalSelectedModule;

    [ObservableProperty] private ObservableRangeCollection<Branche> _modalBranches = new();

    [ObservableProperty] private Branche? _modalSelectedBranche;

    private bool _isEditingExisting;


    [ObservableProperty] private string _pdfStatusText = "Aucun document joint.";

    // Stocke le BLOB temporairement avant la sauvegarde définitive
    private byte[]? _pendingCbzData = null;
    private System.Threading.CancellationTokenSource? _pdfCts;

    [ObservableProperty] private bool _isDocumentAttached = false; // Pour la visibilité de l'icône

    [ObservableProperty] private bool _isPdfViewerOpen = false; // Pour swapper le modal

    [ObservableProperty] private byte[]? _activeCbzBlob = null; // Pour liaison directe au lecteur PDF
    


    public void SetExportContext(bool exportAll)
    {
        _isExportingAll = exportAll;
    }
    
   

    public NotesViewModel()
    {
        Modules = AppDataService.Instance.Modules;
        AppDataService.Instance.DonneesRechargees += OnDonneesRechargees;

        if (AppDataService.Instance.IsLoaded)
        {
            // Données déjà prêtes (navigation vers la page après chargement)
            if (Modules.Count > 0)
                SelectedModule = Modules[0];
            RefreshAllNotes();
            _ = ActualiserIndicateursDocumentsAsync();
        }
        else
        {
            // Données pas encore prêtes, on attend la notification unique de ReplaceAll
            Modules.CollectionChanged += OnModulesLoaded;
        }
    }
    
    private void OnDonneesRechargees()
    {
        SelectedModule = Modules.FirstOrDefault();
        RefreshAllNotes();
        RefreshSelectedModuleBranches();
        _ = ActualiserIndicateursDocumentsAsync();
    }

    private void OnModulesLoaded(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        Modules.CollectionChanged -= OnModulesLoaded;
        if (Modules.Count > 0)
            SelectedModule = Modules[0];
        RefreshAllNotes();
        RefreshSelectedModuleBranches();
        _ = ActualiserIndicateursDocumentsAsync();
    }

    private async Task ActualiserIndicateursDocumentsAsync()
    {
        try
        {
            using var repo = new DataRepository();
            var idsAvecDocument = await repo.GetNoteIdsWithArchiveAsync();

            // Vue "Par module" : met à jour les Note des branches 
            foreach (var module in Modules)
            foreach (var branche in module.Branches)
            foreach (var note in branche.Notes)
            {
                note.HasDocument = idsAvecDocument.Contains(note.Id);
            }

            // Vue "Liste" : met à jour les NoteViewModel de AllNotes 
            foreach (var nvm in AllNotes)
            {
                nvm.HasDocument = idsAvecDocument.Contains(nvm.Id);
            }
        }
        catch
        {
            /* Ignorer en cas d'erreur de lecture */
        }
    }

    partial void OnSelectedModuleChanged(ModuleViewModel? value)
    {
        RefreshSelectedModuleBranches();
    }

    partial void OnModalSelectedModuleChanged(ModuleViewModel? value)
    {
        if (value?.Branches != null)
        {
            ModalBranches.ReplaceAll(value.Branches);
        }
        else
        {
            ModalBranches.Clear();
        }

        if (!_isEditingExisting && ModalBranches.Any())
        {
            ModalSelectedBranche = ModalBranches.First();
        }
    }

    #region Logique Métier

    // Ouvre le lecteur depuis la liste
    [RelayCommand]
    private async Task OuvrirLecteurPdf(Note noteCible)
    {
        if (noteCible == null || noteCible.Id == 0) return;

        ActiveDocumentTitle = noteCible.titre;
        IsPdfViewerOpen = true; // Affiche le modal de lecture

        try
        {
            using (var repo = new DataRepository())
            {
                ActiveCbzBlob = await repo.GetArchiveCbzPourNoteAsync(noteCible.Id);
            }
        }
        catch
        {
            ActiveCbzBlob = null;
        }
    }

    private void RefreshAllNotes()
    {
        AllNotes.Clear();

        var listeTemporaire = AppDataService.Instance.Modules
            .Where(module => module.Branches != null)
            .SelectMany(module => module.Branches
                .Where(branche => branche.Notes != null)
                .SelectMany(branche => branche.Notes
                    .Select(note => new NoteViewModel
                    {
                        Id        = note.Id,
                        Valeur    = note.Valeur,
                        Date      = note.Date,
                        titre     = note.titre,
                        BrancheId = note.BrancheId,
                        BrancheNom = branche.Nom,
                        ModuleNom  = module.Nom,
                        HasDocument = note.HasDocument
                    })))
            .OrderByDescending(n => n.Date);

        foreach (var nvm in listeTemporaire) AllNotes.Add(nvm);
    }

    private void RefreshSelectedModuleBranches()
    {
        if (SelectedModule?.Branches == null)
        {
            SelectedModuleBranchesWithNotes.Clear();
            return;
        }

        var branchesFiltrees = SelectedModule.Branches.Where(b => b.Notes != null && b.Notes.Count > 0);
        SelectedModuleBranchesWithNotes.ReplaceAll(branchesFiltrees);
    }


    [RelayCommand]
    private void OuvrirModalAjout()
    {
        ModalTitle = "Nouvelle note";
        _isEditingExisting = false;
        PdfStatusText = "Aucun document joint.";
        IsDocumentAttached = false;
        _pendingCbzData = null;


        EditingNote = new Note
        {
            Date = DateTime.Today,
            Valeur = 4.0,
            titre = string.Empty
        };

        ModalSelectedModule = SelectedModule ?? Modules.FirstOrDefault();
        IsModalOpen = true;
    }

    [ObservableProperty] private string _activeDocumentTitle = "Document PDF";

    [RelayCommand]
    private void FermerModal() => IsModalOpen = false;

    [RelayCommand]
    private async Task SauvegarderNote()
    {
        try
        {
            if (ModalSelectedBranche == null || string.IsNullOrWhiteSpace(EditingNote.titre))
                return;

            // Sécurisation : clamp de la valeur entre 1 et 6 si l'utilisateur a réussi à entrer null
            if (EditingNote.Valeur < 1.0 || EditingNote.Valeur > 6.0)
            {
                EditingNote.Valeur = Math.Clamp(EditingNote.Valeur, 1.0, 6.0);
            }
            
            EditingNote.BrancheId = ModalSelectedBranche.Id;

            using (var repo = new DataRepository())
            {
                // Sauvegarde de l'entité texte
                if (_isEditingExisting) await repo.ModifierNoteAsync(EditingNote);
                else await repo.AjouterNoteAsync(EditingNote);

                // Sauvegarde de l'archive si un fichier a été généré
                if (_pendingCbzData != null)
                {
                    // L'ID de EditingNote est maintenant garanti grâce à EF Core
                    await repo.SauvegarderArchiveCbzAsync(EditingNote.Id, _pendingCbzData);
                }
            }

            var moduleDest = Modules.FirstOrDefault(m => m.Id == ModalSelectedModule?.Id);
            if (moduleDest != null)
            {
                var brancheDest = moduleDest.Branches.FirstOrDefault(b => b.Id == ModalSelectedBranche.Id);
                if (brancheDest != null)
                {
                    if (_isEditingExisting)
                    {
                        foreach (var m in Modules)
                        foreach (var b in m.Branches)
                        {
                            var oldNote = b.Notes.FirstOrDefault(n => n.Id == EditingNote.Id);
                            if (oldNote != null) b.Notes.Remove(oldNote);
                        }
                    }

                    brancheDest.Notes.Add(EditingNote);
                    brancheDest.Notes = brancheDest.Notes.OrderByDescending(n => n.Date).ToList();
                }
            }

            RefreshAllNotes();
            RefreshSelectedModuleBranches();
            _ = ActualiserIndicateursDocumentsAsync();
            FermerModal();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la sauvegarde : {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ExecuterSuppression(Note noteASupprimer)
    {
        if (noteASupprimer == null) return;

        try
        {
            using (var repo = new DataRepository())
            {
                await repo.SupprimerNoteAsync(noteASupprimer);
            }

            foreach (var m in Modules)
            {
                if (m.Branches == null) continue;
                var b = m.Branches.FirstOrDefault(br => br.Id == noteASupprimer.BrancheId);
                if (b != null)
                {
                    var n = b.Notes.FirstOrDefault(nt => nt.Id == noteASupprimer.Id);
                    if (n != null) b.Notes.Remove(n);
                    break;
                }
            }

            RefreshAllNotes();
            RefreshSelectedModuleBranches();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la suppression : {ex.Message}");
        }
    }

    /// <summary>
    /// Récupère le PDF, le redimensionne et le convertit en CBZ In-Memory
    /// </summary>
    public async Task TraiterPdfAttacheAsync(string pdfPath, string fileName)
    {
        PdfStatusText = $"Compression de '{fileName}' en cours...";
        _pendingCbzData = null;

        _pdfCts?.Cancel();
        _pdfCts = new System.Threading.CancellationTokenSource();

        try
        {
            var fluxImages = PdfManager.ExtractImagesAsync(pdfPath, _pdfCts.Token);
            var fluxResized = ImageProcessor.ResizeImagesAsync(fluxImages, 2_000_000, _pdfCts.Token);

            // NE GARDER QU'UNE SEULE FOIS CE BLOC
            _pendingCbzData = await ArchiveManager.CreateCbzInMemoryAsync(fluxResized,
                SkiaSharp.SKEncodedImageFormat.Webp, 40, _pdfCts.Token);
            double sizeKb = _pendingCbzData.Length / 1024.0;

            PdfStatusText = $"Document prêt à être sauvegardé ({sizeKb:F0} Ko)";
            IsDocumentAttached = true;
        }
        catch (OperationCanceledException)
        {
            PdfStatusText = "Opération annulée.";
        }
        catch (Exception ex)
        {
            PdfStatusText = $"Erreur : {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task AfficherDocument()
    {
        if (EditingNote == null || EditingNote.Id == 0) return;

        ActiveDocumentTitle = EditingNote.titre;
        PdfStatusText = "Chargement du document de la BDD...";

        try
        {
            using (var repo = new DataRepository())
            {
                // On charge le Blob uniquement pour l'affichage (RAM respectée)
                ActiveCbzBlob = await repo.GetArchiveCbzPourNoteAsync(EditingNote.Id);
                if (ActiveCbzBlob != null)
                {
                    IsPdfViewerOpen = true; // Swap visuel vers le lecteur
                }
            }
        }
        catch
        {
            PdfStatusText = "Erreur lors de l'ouverture.";
        }
    }

    /// <summary>
    /// Ferme le lecteur PDF pour revenir à l'éditeur
    /// </summary>
    [RelayCommand]
    private void FermerPdf()
    {
        IsPdfViewerOpen = false;
        ActiveCbzBlob = null; // Libère le blob
        PdfStatusText = "Laissez vide pour conserver le document existant.";

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }


    /// <summary>
    /// Ouvre le modal de modification (asyncTask)
    /// </summary>
    [RelayCommand]
    private async Task OuvrirModalModification(Note noteAModifier)
    {
        if (noteAModifier == null) return;

        ModalTitle = "Modifier la note";
        _isEditingExisting = true;

        // 1. On CLONE la note pour éviter que la vue derrière ne se mette à jour 
        // en temps réel pendant la frappe (avant d'avoir cliqué sur Sauvegarder)
        EditingNote = new Note
        {
            Id = noteAModifier.Id,
            Date = noteAModifier.Date,
            Valeur = noteAModifier.Valeur,
            titre = noteAModifier.titre,
            BrancheId = noteAModifier.BrancheId
        };

        // 2. On retrouve le module parent et la branche pour pré-remplir les ComboBox
        var moduleParent = Modules.FirstOrDefault(m => m.Branches.Any(b => b.Id == noteAModifier.BrancheId));
        if (moduleParent != null)
        {
            ModalSelectedModule = moduleParent; // Déclenche le remplissage des branches
            ModalSelectedBranche = ModalBranches.FirstOrDefault(b => b.Id == noteAModifier.BrancheId);
        }

        // 3. Reset states visuels
        IsDocumentAttached = false;
        ActiveCbzBlob = null;
        IsPdfViewerOpen = false;
        PdfStatusText = "Laissez vide pour conserver le document existant.";
        _pendingCbzData = null;
        IsModalOpen = true; // Ouvre l'overlay

        // 4. Interroge la base de données en arrière-plan pour savoir s'il y a un PDF
        try
        {
            using (var repo = new DataRepository())
            {
                var archiveExistante = await repo.GetArchiveCbzPourNoteAsync(noteAModifier.Id);

                if (archiveExistante != null)
                {
                    double sizeKb = archiveExistante.Length / 1024.0;
                    PdfStatusText = $"Document enregistré ({sizeKb:F0} Ko). Laissez vide pour conserver.";
                    IsDocumentAttached = true; // Affiche l'icône de document
                }
                else
                {
                    PdfStatusText = "Aucun document existant.";
                }
            }
        }
        catch (Exception)
        {
            PdfStatusText = "Erreur lors de la vérification du document.";
        }
    }

    #endregion

    #region ExportModal

     private double CalculerNoteModuleExacte(System.Collections.Generic.List<Branche> branches)
    {
        if (!branches.Any()) return 0;

        // Séparer les branches classiques de la branche Projet de Module (PM)
        var branchesClassiques = branches.Where(b => b.Type != TypeCours.PM).ToList();
        var branchePM = branches.FirstOrDefault(b => b.Type == TypeCours.PM);

        double moyenneBranches = 0;

        // 1. Moyenne des moyennes de branches (qui sont déjà arrondies à 0.5 via CalculerMoyenne)
        if (branchesClassiques.Any())
        {
            moyenneBranches = branchesClassiques.Average(b => b.CalculerMoyenne());
            
            // Arrondi intermédiaire à 0.5 de la "Moyenne Théorique"
            moyenneBranches = Math.Round(moyenneBranches * 2.0, MidpointRounding.AwayFromZero) / 2.0;
        }

        double notePreArrondi = 0;

        // 2. Intégrer le PM selon la règle : (Moyenne Théorique arrondie + Note PM) / 2
        if (branchePM != null)
        {
            double notePM = branchePM.CalculerMoyenne(); // Déjà arrondie à 0.5 par nature
            notePreArrondi = branchesClassiques.Any() ? (moyenneBranches + notePM) / 2.0 : notePM;
        }
        else
        {
            // S'il n'y a pas de PM, la note est juste la moyenne théorique
            notePreArrondi = moyenneBranches;
        }

        // 3. Arrondi final à 0.5 de la note globale du module
        return Math.Round(notePreArrondi * 2.0, MidpointRounding.AwayFromZero) / 2.0;
    }

    private void GenererTexteExportation(string format)
    {
        if (_isExportingAll)
        {
            if (format == "MD") 
                ExportPreviewText = GenererExportGlobal(format);
            else if (format == "JSON") 
                ExportPreviewText = GenererExportGlobalJson();
            else if (format == "CSV") 
                ExportPreviewText = GenererExportGlobalCsv();
        }
        else
        {
            if (format == "MD") 
                ExportPreviewText = GenererExportModule(format);
            else if (format == "JSON") 
                ExportPreviewText = GenererExportModuleJson();
            else if (format == "CSV") 
                ExportPreviewText = GenererExportModuleCsv();
        }
    }
    
    private string GenererExportGlobalCsv()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Branche;Date;Module;Titre;Note");

        if (!AllNotes.Any()) return sb.ToString();

        foreach (var n in AllNotes)
        {
            string branche = n.BrancheNom ?? "";
            string date = n.Date.ToString("dd.MM.yyyy");
            string module = n.ModuleNom ?? "";
            string titre = (n.titre ?? "").Replace(";", ","); // Sécurité CSV
            string note = n.Valeur.ToString("0.0");

            sb.AppendLine($"{branche};{date};{module};{titre};{note}");
        }

        return sb.ToString();
    }

    private string GenererExportModuleCsv()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Branche;Date;Module;Titre;Note");

        if (SelectedModule == null) return sb.ToString();

        // On filtre simplement la liste globale pour ne garder que les notes du module actif
        var notesDuModule = AllNotes.Where(n => n.ModuleNom == SelectedModule.Nom).ToList();
        
        foreach (var n in notesDuModule)
        {
            string branche = n.BrancheNom ?? "";
            string date = n.Date.ToString("dd.MM.yyyy");
            string module = n.ModuleNom ?? "";
            string titre = (n.titre ?? "").Replace(";", ","); // Sécurité CSV
            string note = n.Valeur.ToString("0.0");

            sb.AppendLine($"{branche};{date};{module};{titre};{note}");
        }

        return sb.ToString();
    }
    
    
    private string GenererExportGlobalJson()
    {
        if (!Modules.Any()) return "{}";

        var jsonRoot = new JsonObject
        {
            ["TypeExport"] = JsonValue.Create("Global"),
            ["DateExport"] = JsonValue.Create(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"))
        };

        var modulesArray = new JsonArray();
        
        foreach (var module in Modules)
        {
            var branchesDuModule = module.Branches?.Where(b => b.Notes != null && b.Notes.Any()).ToList() ?? new();
            if (!branchesDuModule.Any()) continue;

            var moduleNode = new JsonObject
            {
                ["Nom"] = JsonValue.Create(module.Nom ?? ""),
                ["NoteDuModule"] = JsonValue.Create(CalculerNoteModuleExacte(branchesDuModule))
            };

            var branchesArray = new JsonArray();
            foreach (var branche in branchesDuModule)
            {
                var brancheNode = new JsonObject
                {
                    ["Nom"] = JsonValue.Create(branche.Nom ?? ""),
                    ["Type"] = JsonValue.Create(branche.Type.ToString()),
                    ["Moyenne"] = JsonValue.Create(branche.CalculerMoyenne())
                };

                var notesArray = new JsonArray();
                foreach (var note in branche.Notes.OrderByDescending(n => n.Date))
                {
                    var noteNode = new JsonObject
                    {
                        ["Date"] = JsonValue.Create(note.Date.ToString("yyyy-MM-dd")),
                        ["Titre"] = JsonValue.Create(note.titre ?? ""),
                        ["Valeur"] = JsonValue.Create(note.Valeur)
                    };
                    notesArray.Add((JsonNode)noteNode);
                }
                
                brancheNode["Notes"] = notesArray;
                branchesArray.Add((JsonNode)brancheNode);
            }
            
            moduleNode["Branches"] = branchesArray;
            modulesArray.Add((JsonNode)moduleNode);
        }
        
        jsonRoot["Modules"] = modulesArray;

        return jsonRoot.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private string GenererExportModuleJson()
    {
        if (SelectedModule == null) return "{}";

        var branches = SelectedModuleBranchesWithNotes.ToList();

        var jsonRoot = new JsonObject
        {
            ["TypeExport"] = JsonValue.Create("Module"),
            ["DateExport"] = JsonValue.Create(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"))
        };

        var moduleNode = new JsonObject
        {
            ["Nom"] = JsonValue.Create(SelectedModule.Nom ?? ""),
            ["NoteDuModule"] = JsonValue.Create(CalculerNoteModuleExacte(branches))
        };

        var branchesArray = new JsonArray();
        foreach (var branche in branches)
        {
            var brancheNode = new JsonObject
            {
                ["Nom"] = JsonValue.Create(branche.Nom ?? ""),
                ["Type"] = JsonValue.Create(branche.Type.ToString()),
                ["Moyenne"] = JsonValue.Create(branche.CalculerMoyenne())
            };

            var notesArray = new JsonArray();
            foreach (var note in branche.Notes.OrderByDescending(n => n.Date))
            {
                var noteNode = new JsonObject
                {
                    ["Date"] = JsonValue.Create(note.Date.ToString("yyyy-MM-dd")),
                    ["Titre"] = JsonValue.Create(note.titre ?? ""),
                    ["Valeur"] = JsonValue.Create(note.Valeur)
                };
                notesArray.Add((JsonNode)noteNode);
            }
            
            brancheNode["Notes"] = notesArray;
            branchesArray.Add((JsonNode)brancheNode);
        }
        
        moduleNode["Branches"] = branchesArray;
        jsonRoot["Module"] = moduleNode;

        return jsonRoot.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private string GenererExportGlobal(string format)
    {
        if (format != "MD") return "Format non supporté.";
        if (!Modules.Any()) return "Aucune donnée à exporter.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Rapport Global des Résultats");
        sb.AppendLine();

        sb.AppendLine("## Résumé des moyennes par module");
        sb.AppendLine();

        foreach (var module in Modules)
        {
            sb.AppendLine($"### Module : {module.Nom}");
            
            var branchesDuModule = module.Branches.Where(b => b.Notes != null && b.Notes.Any()).ToList();
            if (!branchesDuModule.Any())
            {
                sb.AppendLine("*Aucune note pour ce module.*");
                sb.AppendLine();
                continue;
            }

            var moyennesBranches = branchesDuModule.Select(b => new { 
                Nom = b.Nom ?? "Inconnu", 
                Moyenne = b.CalculerMoyenne() 
            }).ToList();
            
            double noteModule = CalculerNoteModuleExacte(branchesDuModule);

            string labelModule = "**NOTE DU MODULE**";
            int maxB = Math.Max(labelModule.Length, moyennesBranches.Max(m => m.Nom.Length));

            sb.AppendLine($"| {"Branche".PadRight(maxB)} | Moyenne |");
            sb.AppendLine($"| {new string('-', maxB)} | ------- |");

            foreach (var mb in moyennesBranches)
            {
                sb.AppendLine($"| {mb.Nom.PadRight(maxB)} | {mb.Moyenne.ToString("0.0").PadRight(7)} |");
            }
            
            sb.AppendLine($"| {labelModule.PadRight(maxB)} | {($"**{noteModule:0.0}**").PadRight(7)} |");
            sb.AppendLine();
        }

        sb.AppendLine("## Historique détaillé de toutes les épreuves");
        sb.AppendLine();
        sb.Append(GenererTableauDetaille(AllNotes.ToList()));

        return sb.ToString();
    }
    
    private string GenererTableauDetaille(System.Collections.Generic.List<NoteViewModel> notes)
    {
        if (!notes.Any()) return "*Aucune note enregistrée.*\n";

        var sb = new System.Text.StringBuilder();
        int maxBranche = Math.Max(7, notes.Max(n => (n.BrancheNom ?? "").Length));
        int maxModule = Math.Max(6, notes.Max(n => (n.ModuleNom ?? "").Length));
        int maxTitre = Math.Max(5, notes.Max(n => (n.titre ?? "").Length));
        
        sb.AppendLine($"| {"Branche".PadRight(maxBranche)} | Date       | {"Module".PadRight(maxModule)} | {"Titre".PadRight(maxTitre)} | Note    |");
        sb.AppendLine($"| {new string('-', maxBranche)} | ---------- | {new string('-', maxModule)} | {new string('-', maxTitre)} | ------- |");

        foreach (var n in notes)
        {
            //  ** autour de la note, puis PadRight
            string noteFormattee = $"**{n.Valeur:0.0}**".PadRight(7);
            
            sb.AppendLine($"| {(n.BrancheNom ?? "").PadRight(maxBranche)} | {n.Date:dd.MM.yyyy} | {(n.ModuleNom ?? "").PadRight(maxModule)} | {(n.titre ?? "").PadRight(maxTitre)} | {noteFormattee} |");
        }
        return sb.ToString();
    }

    private string GenererExportModule(string format)
    {
        if (format != "MD" || SelectedModule == null) return "Export impossible.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# Export Module : {SelectedModule.Nom}");
        sb.AppendLine();

        sb.AppendLine("## Résumé du module");
        
        var branches = SelectedModuleBranchesWithNotes.ToList();
        if (!branches.Any())
        {
            sb.AppendLine("*Aucune note pour ce module.*");
            sb.AppendLine();
        }
        else
        {
            var moyennes = branches.Select(b => new { 
                Nom = b.Nom ?? "Inconnu", 
                Moyenne = b.CalculerMoyenne() 
            }).ToList();
            
            double noteModule = CalculerNoteModuleExacte(branches);

            string labelModule = "**NOTE DU MODULE**";
            int maxB = Math.Max(labelModule.Length, moyennes.Max(m => m.Nom.Length));

            sb.AppendLine($"| {"Branche".PadRight(maxB)} | Moyenne |");
            sb.AppendLine($"| {new string('-', maxB)} | ------- |");

            foreach (var m in moyennes)
                sb.AppendLine($"| {m.Nom.PadRight(maxB)} | {m.Moyenne.ToString("0.0").PadRight(7)} |");
            
            sb.AppendLine($"| {labelModule.PadRight(maxB)} | {($"**{noteModule:0.0}**").PadRight(7)} |");
            sb.AppendLine();
        }

        sb.AppendLine("## Liste des épreuves");
        var toutesLesNotes = AllNotes.Where(n => n.ModuleNom == SelectedModule.Nom).ToList();
        sb.Append(GenererTableauDetaille(toutesLesNotes));

        return sb.ToString();
    }

    private bool _isExportingAll; // Stocke l'intention de l'utilisateur
    
    
    [ObservableProperty] private bool _isExportModalOpen = false;

    [ObservableProperty] private string _exportPreviewText = string.Empty;
    
    [RelayCommand]
    private void FermerModalExportation()
    {
        IsExportModalOpen = false;
    }

    [RelayCommand]
    private void ChangerFormatExportation(string format)
    {
        if (format != null)
        {
            GenererTexteExportation(format);
        }
    }

    #endregion
}