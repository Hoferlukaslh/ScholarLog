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
    [ObservableProperty]
    private bool _isListView = true;

    [ObservableProperty]
    private ObservableRangeCollection<NoteViewModel> _allNotes = new();

    [ObservableProperty]
    private ObservableRangeCollection<Branche> _selectedModuleBranchesWithNotes = new();

    [ObservableProperty]
    private ObservableRangeCollection<ModuleViewModel> _modules;

    [ObservableProperty]
    private ModuleViewModel? _selectedModule;

    // état du modal
    [ObservableProperty]
    private bool _isModalOpen = false;

    [ObservableProperty]
    private string _modalTitle = "Nouvelle note";

    [ObservableProperty]
    private Note _editingNote = new();

    [ObservableProperty]
    private ModuleViewModel? _modalSelectedModule;

    [ObservableProperty]
    private ObservableRangeCollection<Branche> _modalBranches = new();

    [ObservableProperty]
    private Branche? _modalSelectedBranche;

    private bool _isEditingExisting;

    public NotesViewModel()
    {
        // récupére les données depuis le service
        Modules = AppDataService.Instance.Modules;

        if (Modules.Count > 0)
        {
            SelectedModule = Modules[0];
        }

        RefreshAllNotes();
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
        
        EditingNote = new Note 
        { 
            Date = DateTime.Today,
            Valeur = 4.0, 
            titre = string.Empty
        };

        ModalSelectedModule = SelectedModule ?? Modules.FirstOrDefault();
        IsModalOpen = true;
    }

    [RelayCommand]
    private void OuvrirModalModification(Note noteAModifier)
    {
        if (noteAModifier == null) return;

        ModalTitle = "Modifier la note";
        _isEditingExisting = true;
    
        EditingNote = new Note
        {
            Id = noteAModifier.Id,
            Date = noteAModifier.Date,
            Valeur = noteAModifier.Valeur,
            titre = noteAModifier.titre,
            BrancheId = noteAModifier.BrancheId
        };
        
        var moduleParent = Modules.FirstOrDefault(m => m.Branches.Any(b => b.Id == noteAModifier.BrancheId));
        if (moduleParent != null)
        {
            ModalSelectedModule = moduleParent; 
            ModalSelectedBranche = ModalBranches.FirstOrDefault(b => b.Id == noteAModifier.BrancheId);
        }

        IsModalOpen = true;
    }

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
                if (_isEditingExisting) await repo.ModifierNoteAsync(EditingNote);
                else await repo.AjouterNoteAsync(EditingNote);
            }

            var moduleDest = Modules.FirstOrDefault(m => m.Id == ModalSelectedModule?.Id);
            if (moduleDest != null)
            {
                var brancheDest = moduleDest.Branches.FirstOrDefault(b => b.Id == ModalSelectedBranche.Id);
                if (brancheDest != null)
                {
                    if (_isEditingExisting)
                    {
                        foreach(var m in Modules)
                        foreach(var b in m.Branches)
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

#endregion
}