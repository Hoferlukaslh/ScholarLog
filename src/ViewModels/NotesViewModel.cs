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


namespace ScholarLog.ViewModels;

public partial class NotesViewModel : ViewModelBase
{
    // état de la vue
    [ObservableProperty] private bool _isListView = true;

    [ObservableProperty] private ObservableRangeCollection<NoteViewModel> _allNotes = new();

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

    public NotesViewModel()
    {
        // récupére les données depuis le service
        Modules = AppDataService.Instance.Modules;

        if (Modules.Count > 0)
        {
            SelectedModule = Modules[0];
        }

        RefreshAllNotes();
        _ = ActualiserIndicateursDocumentsAsync();
    }

    private async Task ActualiserIndicateursDocumentsAsync()
    {
        try
        {
            using var repo = new DataRepository();
            var idsAvecDocument = await repo.GetNoteIdsWithArchiveAsync();

            foreach (var module in Modules)
            foreach (var branche in module.Branches)
            foreach (var note in branche.Notes)
            {
                note.HasDocument = idsAvecDocument.Contains(note.Id);
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

    // Ouvre le lecteur depuis la liste ---
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
        var listeTemporaire = new System.Collections.Generic.List<NoteViewModel>();

        foreach (var module in Modules)
        {
            if (module.Branches == null) continue;
            foreach (var branche in module.Branches)
            {
                if (branche.Notes == null) continue;
                foreach (var note in branche.Notes)
                {
                    listeTemporaire.Add(new NoteViewModel
                    {
                        NoteData = note,
                        BrancheNom = branche.Nom,
                        ModuleNom = module.Nom
                    });
                }
            }
        }

        AllNotes.ReplaceAll(listeTemporaire.OrderByDescending(n => n.NoteData.Date));
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

            EditingNote.BrancheId = ModalSelectedBranche.Id;

            using (var repo = new DataRepository())
            {
                // Sauvegarde de l'entité texte
                if (_isEditingExisting) await repo.ModifierNoteAsync(EditingNote);
                else await repo.AjouterNoteAsync(EditingNote);

                // NOUVEAU : Sauvegarde de l'archive si un fichier a été généré
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
                    IsPdfViewerOpen = true; // --- NOUVEAU : Swap visuel vers le lecteur ---
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
}